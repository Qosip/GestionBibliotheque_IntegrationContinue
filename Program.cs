using System.Diagnostics;
using System.Runtime.InteropServices;

static int Run(string cmd, string args)
{
    Console.WriteLine($"> {cmd} {args}");

    var p = Process.Start(new ProcessStartInfo
    {
        FileName = cmd,
        Arguments = args,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
        UseShellExecute = true
    });

    p!.WaitForExit();
    return p.ExitCode;
}

static void CleanOldCoverage()
{
    Console.WriteLine("=== Nettoyage des anciens dossiers ===");

    string[] dirs =
    {
        "Coverage",
        "CoverageMerged",
        "TestResults"
    };

    foreach (var dir in dirs)
    {
        if (Directory.Exists(dir))
        {
            Console.WriteLine($"Suppression : {dir}");
            Directory.Delete(dir, true);
        }
    }
}

static void OpenReport(string file)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        Run("cmd.exe", $"/c start {file}");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        Run("open", file);
    else
        Run("xdg-open", file);
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
var argsReport =
    "-reports:\"**/coverage.cobertura.xml\" " +
    "-targetdir:\"CoverageMerged\" " +
    "-reporttypes:Html";

if (Run("reportgenerator", argsReport) != 0)
{
    Console.WriteLine("Erreur génération couverture.");
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
    Console.WriteLine("ERREUR : index.html introuvable.");
    return 1;
}

Console.WriteLine("\n=== FIN ===");
return 0;
