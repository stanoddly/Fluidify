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
        var parser = new FluidParser();

        foreach (var item in Templates)
        {
            var templatePath = item.GetMetadata("FullPath");
            var templateContent = File.ReadAllText(templatePath);

            if (!parser.TryParse(templateContent, out var template, out var error))
            {
                Log.LogError("Failed to parse template '{0}': {1}", templatePath, error);
                return false;
            }

            var target = item.GetMetadata("TargetPath");
            string outputPath;
            if (!string.IsNullOrEmpty(target))
            {
                outputPath = Path.IsPathRooted(target)
                    ? target
                    : Path.Combine(ProjectDirectory, target);
            }
            else
            {
                outputPath = templatePath.Substring(0, templatePath.Length - ".fluid".Length);
            }

            var context = new TemplateContext();
            foreach (DictionaryEntry entry in item.CloneCustomMetadata())
            {
                var key = entry.Key.ToString();
                if (!string.Equals(key, "TargetPath", StringComparison.OrdinalIgnoreCase))
                {
                    context.SetValue(key, entry.Value?.ToString() ?? "");
                }
            }

            var result = template.Render(context);

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(outputPath, result);
            Log.LogMessage(MessageImportance.Normal, "Fluidify: {0} -> {1}", item.ItemSpec, outputPath);
        }

        return true;
    }
}
