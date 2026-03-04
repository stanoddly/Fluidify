using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fluidify;

public class FluidifyTask : Task
{
    [Required]
    public ITaskItem[] Templates { get; set; } = [];

    public string OutputDir { get; set; } = "";

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = [];

    public override bool Execute()
    {
        // TODO: Process templates with Fluid library
        return true;
    }
}
