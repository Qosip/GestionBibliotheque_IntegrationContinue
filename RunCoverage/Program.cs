using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

static int Run(string file, string args, bool useShell = false)
{
    Console.WriteLine($"> {file} {args}");

    var psi = new ProcessStartInfo
    {
        FileName = file,
        Arguments = args,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
        UseShellExecute = useShell,
        WorkingDirectory = Directory.GetCurrentDirectory()
    };

    using var process = Process.Start(psi);
    process!.WaitForExit();
    return process.ExitCode;
}

static void CleanOldCoverage()
{
    Console.WriteLine("=== Nettoyage des anciens dossiers de couverture ===");

    void DeleteIfExists(string path)
    {
        if (!Directory.Exists(path))
            return;

        Console.WriteLine($"Suppression : {path}");
        Directory.Delete(path, recursive: true);
    }

    // Dossiers standard à la racine
    DeleteIfExists("Coverage");
    DeleteIfExists("CoverageMerged");
    DeleteIfExists("TestResults");

    // Tous les TestResults imbriqués (ex : Library.Tests/TestResults, etc.)
    foreach (var dir in Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "TestResults", SearchOption.AllDirectories))
    {
        if (Directory.Exists(dir))
        {
            Console.WriteLine($"Suppression : {dir}");
            Directory.Delete(dir, recursive: true);
        }
    }
}

static string ResolveReportGeneratorPath()
{
    // Windows : on s'appuie sur le global tool dans le PATH
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return "reportgenerator";

    // Linux / WSL / macOS : utilisation de ~/.dotnet/tools/reportgenerator si présent
    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var candidate = Path.Combine(home, ".dotnet", "tools", "reportgenerator");

    if (File.Exists(candidate))
        return candidate;

    // Fallback : on suppose le PATH configuré
    return "reportgenerator";
}

static string ConvertMntPathToWindows(string fullPath)
{
    // Convertit /mnt/c/Users/... → C:\Users\...
    var normalized = fullPath.Replace('\\', '/');
    const string prefix = "/mnt/";
    if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || normalized.Length <= prefix.Length + 2)
        return fullPath;

    var driveLetter = normalized[prefix.Length];
    var slashAfterDrive = normalized[prefix.Length + 1];
    if (slashAfterDrive != '/')
        return fullPath;

    var rest = normalized.Substring(prefix.Length + 2); // après "/mnt/c/"
    var windowsPath = $"{char.ToUpperInvariant(driveLetter)}:\\{rest.Replace('/', '\\')}";
    return windowsPath;
}

static void OpenReport(string file)
{
    var fullPath = Path.GetFullPath(file);

    // Windows natif
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // IMPORTANT : titre vide "" puis chemin réel
        Run("cmd.exe", $"/c start \"\" \"{fullPath}\"", useShell: true);
        return;
    }

    // Linux / WSL
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        var normalized = fullPath.Replace('\\', '/');

        // Cas WSL : le repo est monté sur /mnt/... → ouvrir via Windows
        if (normalized.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase))
        {
            var windowsPath = ConvertMntPathToWindows(fullPath);
            Run("cmd.exe", $"/c start \"\" \"{windowsPath}\"", useShell: true);
            return;
        }

        // Linux natif avec environnement graphique
        Run("xdg-open", $"\"{fullPath}\"");
        return;
    }

    // macOS
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        Run("open", $"\"{fullPath}\"");
        return;
    }

    // Fallback ultime
    Console.WriteLine($"Ouverture automatique non gérée pour cet OS. Ouvre manuellement : {fullPath}");
}

//
// --- MAIN ---
//

Console.WriteLine("=== RUN COVERAGE ===");

CleanOldCoverage();

Console.WriteLine("\n=== Étape 1 : dotnet test ===");
if (Run("dotnet", "test") != 0)
{
    Console.WriteLine("Tests échoués.");
    return 1;
}

Console.WriteLine("\n=== Étape 2 : dotnet test --collect:\"XPlat Code Coverage\" ===");
if (Run("dotnet", "test --collect:\"XPlat Code Coverage\"") != 0)
{
    Console.WriteLine("Échec de la collecte de couverture.");
    return 1;
}

Console.WriteLine("\n=== Étape 3 : Installation / mise à jour de reportgenerator ===");
Run("dotnet", "tool install -g dotnet-reportgenerator-globaltool");
Run("dotnet", "tool update -g dotnet-reportgenerator-globaltool");

Console.WriteLine("\n=== Étape 4 : Génération du rapport fusionné ===");
var reportGen = ResolveReportGeneratorPath();
var argsReport =
    "-reports:\"**/coverage.cobertura.xml\" " +
    "-targetdir:\"CoverageMerged\" " +
    "-reporttypes:Html";

if (Run(reportGen, argsReport) != 0)
{
    Console.WriteLine("Erreur : génération du rapport de couverture.");
    return 1;
}

var index = Path.Combine("CoverageMerged", "index.html");

Console.WriteLine("\n=== Étape 5 : ouverture ===");

if (File.Exists(index))
{
    OpenReport(index);
    Console.WriteLine("Rapport ouvert.");
}
else
{
    Console.WriteLine("ERREUR : CoverageMerged/index.html introuvable.");
    return 1;
}

Console.WriteLine("\n=== FIN ===");
return 0;
