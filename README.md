# Fluidify

A modern alternative to T4 — an MSBuild task that processes [Fluid](https://github.com/sebastienros/fluid) (Liquid) template files at build time to generate code, configuration, or any other text output.

## Installation

```shell
dotnet add package Fluidify
```

Fluidify is a development dependency — it runs only at build time and adds nothing to your project's runtime output.

## Usage

Add `<Fluidify>` items to your project file. Each item points to a `.fluid` template, and any custom metadata you set becomes a template variable.

```xml
<ItemGroup>
  <Fluidify Include="WelcomeMessage.cs.fluid" Name="Alice" />
  <Fluidify Include="Config/appsettings.json.fluid" AppName="MyApp" Version="1.0.0" />
</ItemGroup>
```

Templates use standard [Liquid syntax](https://shopify.github.io/liquid/):

**WelcomeMessage.cs.fluid**
```liquid
public static class WelcomeMessage
{
    public const string Text = "Welcome, {{ Name }}!";
}
```

Building the project renders each template before compilation. The output path is inferred by stripping the `.fluid` extension, so `WelcomeMessage.cs.fluid` produces `WelcomeMessage.cs`.

Fluidify works with any text format — C#, JSON, HTML, YAML, or anything else. The `.fluid` extension is just a convention; the template itself is plain text with Liquid tags.

### Compilation

Generated `.cs` files are automatically included in compilation. Fluidify handles deduplication, so you don't need to add `<Compile Remove>` for generated files.

To opt out of compilation, set `Compile="false"`:

```xml
<ItemGroup>
  <Fluidify Include="WelcomeMessage.cs.fluid" Name="Alice" Compile="false" />
</ItemGroup>
```

For non-`.cs` outputs that should be compiled, set `Compile="true"` explicitly:

```xml
<ItemGroup>
  <Fluidify Include="Templates/Helper.fluid" Compile="true" />
</ItemGroup>
```

### Custom output path

Use the `Destination` metadata to write the output to a different location:

```xml
<ItemGroup>
  <Fluidify Include="Templates/report.html.fluid"
            Destination="Output/report.html"
            Title="Status Report"
            Author="MyApp" />
</ItemGroup>
```

Relative paths are resolved from the project directory. Directories are created automatically.

## How it works

Fluidify registers an MSBuild target that runs before `CoreCompile`. For each `<Fluidify>` item it:

1. Parses the `.fluid` file using the Fluid template engine
2. Passes all item metadata (except `Destination` and `Compile`) as template variables
3. Writes the rendered output to the inferred or specified destination

MSBuild tracks input and output timestamps, so templates are only re-rendered when the source file changes.

## License

Apache-2.0
