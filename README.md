# GestionBibliotheque

Application de gestion de bibliothèque multi-sites basée sur .NET, structurée en couches Domain / Application / Infrastructure / Web, avec une suite de tests unitaires exhaustive et une intégration continue via Jenkins.

---

## 1. Objectif du projet

L’objectif est de modéliser et implémenter une bibliothèque répartie sur plusieurs sites avec :

- Gestion des **livres** (Book) et de leurs **exemplaires** physiques (BookCopy).
- Gestion des **utilisateurs** (UserAccount).
- Gestion des **sites** (Site) de la bibliothèque.
- **Emprunt** et **retour** de livres avec :
  - limitation du nombre d’emprunts actifs,
  - calcul de **pénalités de retard**.
- **Transfert** d’exemplaires de livres entre sites.
- Exposition de ces fonctionnalités via une **application web** (ASP.NET Core) + **tests unitaires** avec couverture de code à 100 % sur Domain et Application.

---

## 2. Architecture globale du projet

La solution est organisée comme suit :

```text
GestionBibliotheque.sln
│
├── Library.Domain
├── Library.Application
├── Library.Infrastructure
├── Library.WebApi
├── Library.Tests
├── RunCoverage/        (outil CLI pour tests + couverture multi-OS)
├── CoverageMerged/     (généré : rapport de couverture HTML)
└── coverlet.runsettings
```

### 2.1 Library.Domain (couche métier)

Contient la **logique métier pure**, sans dépendance sur l’infrastructure :

- `Entities/`
  - `Book` : ISBN, titre, auteur.
  - `BookCopy` : exemplaire physique, site, statut (`Available`, `Borrowed`, `InTransfer`).
  - `Loan` : prêt (dates, utilisateur, exemplaire, état de retard).
  - `Site` : sites physiques de la bibliothèque.
  - `UserAccount` : utilisateur, emprunts actifs, montant dû.
- `Enums/`
  - `BookCopyStatus` : `Available`, `Borrowed`, `InTransfer`.
- `Services/`
  - `BorrowingService` : applique les règles d’emprunt (plafond d’emprunts, disponibilité des exemplaires, cohérence des dates).
  - `ReturnService` : logique de retour (marque le prêt comme retourné, applique la pénalité, décrémente les emprunts).
  - `PenaltyService` : calcule le montant de la pénalité à partir des jours de retard.
- `Results/`
  - `BorrowResult` : résultat métier de l’emprunt (succès/échec + code erreur + `Loan`).

### 2.2 Library.Application (couche use cases)

Contient l’**orchestration des cas d’usage** (commandes, handlers, interfaces de persistance) :

- `Commands/`
  - `RegisterBookCommand`
  - `RegisterUserCommand`
  - `CreateSiteCommand`
  - `AddBookCopyCommand`
  - `BorrowBookCommand`
  - `ReturnBookCommand`
  - `RequestTransferCommand`
- `Handlers/`
  - `RegisterBookHandler`
  - `RegisterUserHandler`
  - `CreateSiteHandler`
  - `AddBookCopyHandler`
  - `BorrowBookHandler`
  - `ReturnBookHandler`
  - `RequestTransferHandler`
- `Results/`
  - `RegisterBookResult`
  - `RegisterUserResult`
  - `CreateSiteResult`
  - `AddBookCopyResult`
  - `BorrowBookResult`
  - `ReturnBookResult`
  - `RequestTransferResult`
- `Repositories/` (interfaces)
  - `IBookRepository`
  - `IBookCopyRepository`
  - `ILoanRepository`
  - `ISiteRepository`
  - `IUserRepository`
- Racine :
  - `IClock` : abstraction du temps (testable).

Les handlers coordonnent :

- lecture et écriture via les repositories,
- appels aux services de Domain (`BorrowingService`, `ReturnService`, `PenaltyService`),
- production de `Result` applicatifs (codes erreurs explicites).

### 2.3 Library.Infrastructure (couche d’infrastructure)

Implémentations concrètes des interfaces de `Library.Application` :

- `LibraryDbContext` : DbContext EF Core (SQLite).
- `EfBookRepository`, `EfBookCopyRepository`, `EfUserRepository`, `EfSiteRepository`, `EfLoanRepository`.
- `SystemClock` : implémentation concrète de `IClock` basée sur `DateTime.UtcNow`.

### 2.4 Library.WebApi (couche web)

Application ASP.NET Core (MVC / Razor) :

- `Controllers/`
  - `BooksController`, `UsersController`, `SitesController`, `LoansController`, `TransfersController`, `HomeController`.
- `Models/` (ViewModels)
- `Views/` (Razor) : pages pour lister, créer, emprunter, retourner, transférer.
- `Program.cs` : composition racine (DI, EF Core, configuration SQLite, routing).

---

## 3. Fonctionnalités métier principales

### 3.1 Gestion des livres et exemplaires

- Enregistrement d’un livre via `RegisterBookHandler` à partir d’un `RegisterBookCommand`.
- Ajout d’un exemplaire pour un livre sur un site via `AddBookCopyHandler`.

### 3.2 Gestion des utilisateurs

- Enregistrement d’un utilisateur via `RegisterUserHandler`.
- Les utilisateurs sont porteurs :
  - d’un **compteur de prêts actifs** (`ActiveLoansCount`),
  - d’un **montant dû** (`AmountDue`) correspondant aux pénalités.

### 3.3 Emprunt de livre

Orchestré par `BorrowBookHandler` :

1. Vérifie que l’utilisateur existe.
2. Cherche un exemplaire disponible sur le site demandé (`IBookCopyRepository.FindAvailableCopy`).
3. Fixe une durée d’emprunt (ex. 14 jours).
4. Appelle `BorrowingService.TryBorrow` (Domain).
5. Met à jour :
   - l’état de l’exemplaire (`BookCopy.MarkAsBorrowed()`),
   - le compteur d’emprunts de l’utilisateur,
   - persiste le `Loan`.

### 3.4 Retour de livre et pénalités

Orchestré par `ReturnBookHandler` :

1. Charge le `Loan` depuis `ILoanRepository`.
2. Charge l’utilisateur associé et la copie associée.
3. Récupère la date de retour via `IClock.UtcNow`.
4. Appelle `ReturnService.ReturnBook(user, loan, returnDate, DefaultDailyRate)` :
   - marque le prêt comme retourné,
   - calcule la pénalité via `PenaltyService` (nombre de jours de retard × tarif journalier),
   - met à jour `AmountDue` de l’utilisateur,
   - décrémente son compteur de prêts actifs.
5. Remet l’exemplaire disponible (`BookCopy.MarkAsReturned()`).
6. Persiste les changements.

### 3.5 Transfert d’exemplaire entre sites

Orchestré par `RequestTransferHandler` :

1. Vérifie que `SourceSiteId` et `TargetSiteId` sont différents.
2. Cherche un exemplaire disponible sur le site source.
3. Marque la copie comme **InTransfer**.
4. Retourne un `RequestTransferResult` indiquant l’ID de la copie mise en transfert.

---

## 4. Tests : organisation, style et objectifs

### 4.1 Organisation des tests

Les tests se trouvent dans :

```text
Library.Tests/
```

Structure logique :

- Tests Domain :
  - `BookTests.cs`
  - `BookCopyTests.cs`
  - `LoanTests.cs`
  - `UserAccountTests.cs`
  - `BorrowingServiceTests.cs`
  - `ReturnServiceTests.cs`
  - `PenaltyServiceTests.cs`
  - `BorrowResultTests.cs`
- Tests Application :
  - `ApplicationBookTests.cs`
  - `ApplicationUserTests.cs`
  - `ApplicationSiteTests.cs`
  - `ApplicationLoanTests.cs`
  - `ApplicationTransferTests.cs`
  - `ApplicationCopyTests.cs`
  - etc. (un fichier par “type” fonctionnel).

### 4.2 Style des tests

- Framework : **xUnit**.
- Convention : **un test par scénario métier**, nom explicite, par ex. :
  - `BorrowBookHandler_Should_Fail_When_User_Not_Found`
  - `ReturnBookHandler_Should_ApplyPenalty_When_BookIsReturnedLate`
- Pattern : **AAA (Arrange / Act / Assert)** systématique.
- Tests écrits sans Moq :
  - utilisation de **faux repositories** in-memory (`FakeUserRepository`, `FakeBookCopyRepository`, etc.),
  - contrôle des horloges via `FakeClock`.

### 4.3 Ce que les tests vérifient réellement

- **Cas nominaux** :
  - les flux métier “heureux chemin” (emprunt, retour, transfert, enregistrement).
- **Cas limites / erreurs** :
  - `Guid.Empty`,
  - valeurs nulles / chaînes vides (ex. noms, titres),
  - dates incohérentes (`dueDate < borrowedAt`, retour avant l’emprunt),
  - dépassement du **nombre maximal de prêts actifs**,
  - absence d’exemplaire disponible,
  - prêt ou utilisateur introuvable.
- **Invariants de Domain** :
  - impossibilité de marquer une copie comme retournée si elle n’est pas empruntée,
  - impossibilité de marquer comme empruntée si déjà empruntée ou en transfert,
  - calcul du nombre de jours de retard,
  - impossibilité de créer des entités avec données invalides (exceptions).

### 4.4 Couverture de code

- **Domain** : 100 % de lignes couvertes.
- **Application** : 100 % de lignes couvertes (incluant les handlers et résultats).
- La couverture est calculée via :
  - `dotnet test --collect:"XPlat Code Coverage"` (format `.cobertura.xml`),
  - `ReportGenerator` pour produire un rapport HTML fusionné.

---

## 5. Dépôt Git et dépôt bare sur le serveur Git

### 5.1 Dépôt bare sur le serveur Git

Sur le **serveur Git**, le dépôt bare est typiquement stocké dans un dossier dédié :

```text
/home/admin/LibraryProject
```
---

## 6. Build, tests et exécution locale

### 6.1 Prérequis

- SDK .NET 8 (pour Domain, Application, Infrastructure, Tests).
- SDK .NET 9 (pour WebApi).
- SQLite (embarqué via EF Core, aucun serveur externe nécessaire).

### 6.2 Build

```bash
dotnet build
```

### 6.3 Exécuter les tests unitaires

```bash
dotnet test
```

### 6.4 Lancer l’application web

```bash
dotnet run --project Library.WebApi
```

Par défaut, l’application écoute sur `http://localhost:7065/` ou `https://localhost:5143` (selon configuration ASP.NET).  
La base SQLite est créée / utilisée dans `Library.WebApi/library.db`.

---

## 7. Génération locale des rapports de couverture (RunCoverage)

Un projet utilitaire dédié, **multi-plateforme**, orchestre la génération de rapport de couverture :

```text
RunCoverage/
  RunCoverage.csproj
  Program.cs
```

### 7.1 Ce que fait `RunCoverage`

En une commande, il :

1. Nettoie les anciens rapports (`CoverageMerged`, `TestResults`…).
2. Exécute les tests :
   - `dotnet test`
   - `dotnet test --collect:"XPlat Code Coverage"`
3. Lance `reportgenerator` pour fusionner les rapports Cobertura :
   - sortie dans `CoverageMerged/`
   - formats : **HTML** (+ OpenCover si configuré).
4. Ouvre automatiquement le rapport (`index.html`) :
   - sous Windows : `start CoverageMerged/index.html`
   - sous Linux / WSL : `xdg-open` / `wslview` selon l’environnement.

### 7.2 Commande à exécuter

Depuis la racine de la solution :

```bash
dotnet run --project RunCoverage
```

Résultat :

- Rapport de couverture consultable dans :
  - `CoverageMerged/index.html`

---

## 8. Intégration continue Jenkins

### 8.1 Lancement de Jenkins sur le serveur Jenkins

Sur la machine Jenkins (Linux, systemd) :

```bash
/opt/jdk17/bin/java -DJENKINS_HOME=/home/admin/.jenkins -jar /home/admin/jenkins.war &
```

L’interface Web est ensuite accessible à l’adresse :

```text
http://<serveur-jenkins>:8080
```

### 8.2 Job Jenkins « GestionBibliotheque »

Le job Jenkins « GestionBibliotheque » est configuré comme suit.

1. **Checkout du dépôt Git**

   - Gestion de code source : **Git**
   - Repository URL :  
     `admin@10.10.58.12:/home/admin/LibraryProject`
   - Credentials : utilisateur `admin` (clé/MDP configuré dans Jenkins)
   - Branche à builder :  
     `*/main`

2. **Déclenchement du job**

   - Trigger : **« Déclencher les builds à distance »** activé  
   - Jeton d’authentification : `tokenIC`  
   - URL d’appel distant (exemples) :  
     `http://<JENKINS_URL>/job/biblio_IC/build?token=tokenIC`  
     ou  
     `http://<JENKINS_URL>/job/biblio_IC/buildWithParameters?token=tokenIC`

3. **Environnement**

   - `Delete workspace before build starts` : **activé**  
   - `Add timestamps to the Console Output` : **activé**

4. **Étapes de build (Execute shell)**

   Script de build exécuté par Jenkins :

   ```bash
   echo "-- 1. Restauration des paquets --"
   dotnet restore

   echo "-- 2. Compilation --"
   dotnet build --configuration Release --no-restore

   echo "-- 3. Tests unitaires et couverture --"
   dotnet test --no-build --configuration Release \
     --collect:"XPlat Code Coverage" \
     --logger "trx;LogFileName=test_results.trx" \
     --results-directory ./TestResults

   echo "-- 4. Génération du rapport HTML --"
   dotnet new tool-manifest --force || true
   dotnet tool install dotnet-reportgenerator-globaltool || true

   dotnet tool run reportgenerator \
     "-reports:./TestResults/**/coverage.cobertura.xml" \
     "-targetdir:./CoverageReport" \
     "-reporttypes:Html"
   ```

5. **Actions post-build**

   - **Archivage des artefacts**  
     - Fichiers à archiver :  
       `TestResults/**/*.trx, CoverageReport/**`
   - **Publish HTML reports**  
     - HTML directory to archive : `CoverageReport`  
     - Index page(s) : `index.html`  
     - Report title : `Code Coverage Report`

Après chaque build, le rapport de couverture est consultable directement depuis Jenkins dans la section **« Code Coverage Report »**, qui pointe vers `CoverageReport/index.html` généré par `reportgenerator`.

---

## 9. Qualité et stratégie de tests

### 9.1 Objectifs de tests

- Vérifier **toutes les branches métier critiques**, y compris :
  - erreurs utilisateur (données invalides),
  - absence d’entités (user / book / copy / loan non trouvés),
  - dates incohérentes,
  - dépassement de limite d’emprunts.
- Sécuriser les **règles métier** :
  - pas de retour « dans le passé »,
  - pas d’emprunt si la copie n’est pas disponible,
  - pas de transfert si source et cible sont identiques.
- Garantir que le code reste **refactorable** sous couvert de tests.

### 9.2 Principes appliqués

- Tests **autonomes**, sans base externe :
  - **fake repositories in-memory** pour `IUserRepository`, `IBookRepository`, `IBookCopyRepository`, `ILoanRepository`, `ISiteRepository`.
- Injection d’un `IClock` fake pour maîtriser la notion de temps (scénarios de retard explicites).
- Tests de **cas improbables** mais réalistes pour assurer la robustesse :
  - retours avant la date d’emprunt (doit lever ou échouer),
  - dates d’échéance avant la date de prêt,
  - pénalités à 0 si pas de retard,
  - tentatives d’emprunt au-delà de la limite d’emprunts actifs.

---

## 10. Points forts

- Architecture claire **Domain / Application / Infrastructure / Web**.
- Tests unitaires complets couvrant :
  - la logique métier pure (Domain),
  - les cas d’usage applicatifs (Handlers).
- **100 % de couverture de lignes** sur Domain et Application.
- Outil de couverture **multi-plateforme** (`RunCoverage`) pour automatiser :
  - tests,
  - génération des rapports,
  - ouverture du HTML.
- Intégration avec Jenkins :
  - build,
  - tests,
  - rapports,
  - couverture.
