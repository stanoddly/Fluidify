using System.Diagnostics;
using NUnit.Framework;

namespace Fluidify.Tests.Functional;

[TestFixture]
public class CompileTests
{
    private static readonly string SolutionRoot = Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    private static readonly string FluidifyProject = Path.Combine(
        SolutionRoot, "src", "Fluidify", "Fluidify.csproj");

    private static readonly string CompileAppDir = Path.Combine(
        SolutionRoot, "tests", "Fluidify.Tests.Functional", "Fixtures", "CompileApp");

    [OneTimeSetUp]
    public async Task PackAndBuildFluidify()
    {
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
    }

    private static readonly string CompileFalseAppDir = Path.Combine(
        SolutionRoot, "tests", "Fluidify.Tests.Functional", "Fixtures", "CompileFalseApp");

    [Test]
    public async Task GeneratedCsFile_IsCompiledSuccessfully()
    {
        string generatedFile = Path.Combine(CompileAppDir, "Models", "Greeter.cs");

        // Delete generated file to simulate a clean build
        if (File.Exists(generatedFile))
            File.Delete(generatedFile);

        string compileAppProject = Path.Combine(CompileAppDir, "CompileApp.csproj");
        (int ExitCode, string Output) result = await RunDotnet($"build \"{compileAppProject}\" --force");

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"CompileApp build failed — generated .cs file was not included in compilation:\n{result.Output}");
    }

    [Test]
    public async Task GeneratedCsFile_CompileFalse_IsExcludedFromCompilation()
    {
        string generatedFile = Path.Combine(CompileFalseAppDir, "Models", "Greeter.cs");

        // Delete generated file to simulate a clean build
        if (File.Exists(generatedFile))
            File.Delete(generatedFile);

        string project = Path.Combine(CompileFalseAppDir, "CompileFalseApp.csproj");
        (int ExitCode, string Output) result = await RunDotnet($"build \"{project}\" --force");

        Assert.That(result.ExitCode, Is.Not.EqualTo(0),
            $"Build should have failed — Compile=\"false\" should exclude the generated .cs from compilation:\n{result.Output}");
        Assert.That(result.Output, Does.Contain("CS0246").Or.Contains("CS0103"),
            "Expected a missing type/namespace compiler error");
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
