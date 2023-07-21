using AasxIntegrationBase;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginVec.AnyUi
{
    public static class ImportVecDialog
    {
        public static async Task<string> DetermineVecFileToImport(
            VecOptions options,
            LogInstance log,
            AnyUiContextPlusDialogs displayContext)
        {

            var result = await displayContext.MenuSelectOpenFilenameAsync(
                null,
                null,
                "Select VEC file to import ..",
                "*.vec", "VEC container files (*.vec)|*.vec|Alle Dateien (*.*)|*.*",
                "VEC Import");

            return result.OriginalFileName;
        }

    }
}
