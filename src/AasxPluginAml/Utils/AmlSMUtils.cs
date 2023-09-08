using AasCore.Aas3_0;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginAml.Utils;

public static class AmlSMUtils
{
    public const string SEM_ID_AML_SM = "https://automationml.org/aas/1/0/AmlFile";

    public static File? GetAmlFile(this ISubmodel amlSubmodel)
    {
        return amlSubmodel.OverSubmodelElementsOrEmpty().FirstOrDefault(f => IsAmlFile(f as File)) as File;
    }

    public static bool IsAmlFile(IFile? file)
    {
        return file?.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_AML_SM) ?? false;
    }
}
