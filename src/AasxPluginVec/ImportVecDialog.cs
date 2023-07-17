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
        public static async Task ImportVECDialogBased(
            VecOptions options,
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext)
        {
            // which Submodel
            var packageEnv = ticket.Package;
            var env = ticket.Env;
            var aas = ticket.AAS;
            if (packageEnv == null || env == null || aas == null)
            {
                log.Error($"Import VEC: An AAS has to be selected!");
                return;
            }

            var result = await displayContext.MenuSelectOpenFilenameAsync(
                null,
                null,
                "Select VEC file to import ..",
                "*.vec", "VEC container files (*.vec)|*.vec|Alle Dateien (*.*)|*.*",
                "VEC Import");

            var fileName = result.OriginalFileName;

            log.Info($"Importing VEC container from file: {fileName} ..");
            VecImporter.ImportVecFromFile(packageEnv, env, aas, fileName, options, log);

        }

    }
}
