using System;
using System.Collections;
using System.IO;
using Fluid;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fluidify;

public class FluidifyTask : Task
{
    [Required]
    public ITaskItem[] Templates { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public string ProjectDirectory { get; set; } = "";

    public override bool Execute()
    {
        FluidParser parser = new FluidParser();

        foreach (ITaskItem item in Templates)
        {
            string templatePath = item.GetMetadata("FullPath");
            string templateContent = File.ReadAllText(templatePath);

            if (!parser.TryParse(templateContent, out IFluidTemplate template, out string error))
            {
                Log.LogError("Failed to parse template '{0}': {1}", templatePath, error);
                return false;
            }

            string destination = item.GetMetadata("Destination");
            string outputPath;
            if (!string.IsNullOrEmpty(destination))
            {
                outputPath = Path.IsPathRooted(destination)
                    ? destination
                    : Path.Combine(ProjectDirectory, destination);
            }
            else
            {
                outputPath = templatePath.Substring(0, templatePath.Length - ".fluid".Length);
            }

            TemplateContext context = new TemplateContext();
            foreach (DictionaryEntry entry in item.CloneCustomMetadata())
            {
                string key = entry.Key.ToString();
                if (!string.Equals(key, "Destination", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(key, "Compile", StringComparison.OrdinalIgnoreCase))
                {
                    context.SetValue(key, entry.Value?.ToString() ?? "");
                }
            }

            string result = template.Render(context);

            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(outputPath, result);
            Log.LogMessage(MessageImportance.Normal, "Fluidify: {0} -> {1}", item.ItemSpec, outputPath);
        }

        return true;
    }
}
