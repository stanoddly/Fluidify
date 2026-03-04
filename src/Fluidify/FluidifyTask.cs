using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fluidify;

public class FluidifyTask : Task
{
    [Required]
    public ITaskItem[] Templates { get; set; } = [];

    public override bool Execute()
    {
        // TODO: For each template item:
        // 1. Read the .fluid file (item.ItemSpec)
        // 2. Derive output path: strip .fluid extension, or use explicit Target metadata
        // 3. Collect all custom metadata as Liquid template variables
        // 4. Process template with Fluid library
        // 5. Write output file next to the template
        return true;
    }
}
