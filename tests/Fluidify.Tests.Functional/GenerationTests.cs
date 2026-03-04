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

    private static readonly string GeneratedDir = Path.Combine(
        SampleAppDir, "obj", "Debug", "net9.0", "Fluidify");

    [OneTimeSetUp]
    public async Task PackAndBuildSampleApp()
    {
        // Clear any cached Fluidify package from the global NuGet cache
        var localsResult = await RunDotnet("nuget locals global-packages --list");
        var globalPackagesDir = localsResult.Output
            .Replace("global-packages:", "")
            .Replace("info :", "")
            .Trim();
        var fluidifyCache = Path.Combine(globalPackagesDir, "fluidify");
        if (Directory.Exists(fluidifyCache))
            Directory.Delete(fluidifyCache, true);

        // Build Fluidify (GeneratePackageOnBuild produces the nupkg)
        var buildResult = await RunDotnet(
            $"build \"{FluidifyProject}\" -c Release");
        Assert.That(buildResult.ExitCode, Is.EqualTo(0),
            $"Fluidify build failed:\n{buildResult.Output}");

        // Build SampleApp using the Fluidify NuGet
        var sampleAppProject = Path.Combine(SampleAppDir, "SampleApp.csproj");
        var sampleBuildResult = await RunDotnet($"build \"{sampleAppProject}\" --force");
        Assert.That(sampleBuildResult.ExitCode, Is.EqualTo(0),
            $"SampleApp build failed:\n{sampleBuildResult.Output}");
    }

    [TestCase("Models/Greeting.cs")]
    [TestCase("Config/appsettings.json")]
    public async Task FluidTemplate_GeneratesExpectedOutput(string relativePath)
    {
        var generatedFile = Path.Combine(GeneratedDir, relativePath);
        var expectedFile = Path.Combine(ExpectedDir, relativePath);

        Assert.That(File.Exists(generatedFile), Is.True,
            $"Generated file not found: {generatedFile}");

        var generated = await File.ReadAllTextAsync(generatedFile);
        var expected = await File.ReadAllTextAsync(expectedFile);

        Assert.That(generated, Is.EqualTo(expected),
            $"Generated output for {relativePath} does not match expected");
    }

    private static async Task<(int ExitCode, string Output)> RunDotnet(string arguments)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout + stderr);
    }
}
