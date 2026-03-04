using System.Diagnostics;
using NUnit.Framework;

namespace Fluidify.Tests.Functional;

[TestFixture]
public class GenerationTests
{
    private static readonly string SolutionRoot = Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    private static readonly string FluidifyProject = Path.Combine(
        SolutionRoot, "src", "Fluidify", "Fluidify.csproj");

    private static readonly string SampleAppDir = Path.Combine(
        SolutionRoot, "tests", "Fluidify.Tests.Functional", "Fixtures", "SampleApp");

    private static readonly string ExpectedDir = Path.Combine(SampleAppDir, "Expected");

    private static readonly string[] GeneratedPaths =
        ["Models/Greeting.cs", "Config/appsettings.json", "Output/report.html"];

    [OneTimeSetUp]
    public async Task PackAndBuildSampleApp()
    {
        // Clean previously generated output files to avoid stale artifacts
        foreach (string path in GeneratedPaths)
        {
            string fullPath = Path.Combine(SampleAppDir, path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        // Clear any cached Fluidify package from the global NuGet cache
        (int ExitCode, string Output) localsResult = await RunDotnet("nuget locals global-packages --list");
        string globalPackagesDir = localsResult.Output
            .Replace("global-packages:", "")
            .Replace("info :", "")
            .Trim();
        string fluidifyCache = Path.Combine(globalPackagesDir, "fluidify");
        if (Directory.Exists(fluidifyCache))
            Directory.Delete(fluidifyCache, true);

        // Build Fluidify (GeneratePackageOnBuild produces the nupkg)
        (int ExitCode, string Output) buildResult = await RunDotnet(
            $"build \"{FluidifyProject}\" -c Release");
        Assert.That(buildResult.ExitCode, Is.EqualTo(0),
            $"Fluidify build failed:\n{buildResult.Output}");

        // Build SampleApp using the Fluidify NuGet
        string sampleAppProject = Path.Combine(SampleAppDir, "SampleApp.csproj");
        (int ExitCode, string Output) sampleBuildResult = await RunDotnet($"build \"{sampleAppProject}\" --force");
        Assert.That(sampleBuildResult.ExitCode, Is.EqualTo(0),
            $"SampleApp build failed:\n{sampleBuildResult.Output}");
    }

    [TestCase("Models/Greeting.cs")]
    [TestCase("Config/appsettings.json")]
    [TestCase("Output/report.html")]
    public async Task FluidTemplate_GeneratesExpectedOutput(string relativePath)
    {
        string generatedFile = Path.Combine(SampleAppDir, relativePath);
        string expectedFile = Path.Combine(ExpectedDir, relativePath);

        Assert.That(File.Exists(generatedFile), Is.True,
            $"Generated file not found: {generatedFile}");

        string generated = await File.ReadAllTextAsync(generatedFile);
        string expected = await File.ReadAllTextAsync(expectedFile);

        Assert.That(generated, Is.EqualTo(expected),
            $"Generated output for {relativePath} does not match expected");
    }

    private static async Task<(int ExitCode, string Output)> RunDotnet(string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(psi)!;
        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout + stderr);
    }
}
