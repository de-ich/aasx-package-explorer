using AasCore.Aas3_0;
using AasxIntegrationBase;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using static AasxPluginVec.BomSMUtils;
using static AasxPluginVec.SubassemblyUtils;

namespace AasxPluginVec.AnyUi
{
    public static class AssociateSubassembliesWithModuleDialog
    {
        public class AssociateSubassembliesWithModuleDialogResult
        {
            public IEntity SelectedModule { get; set; }
        }

        public static async Task<AssociateSubassembliesWithModuleDialogResult> DetermineAssociateSubassembliesWithModuleConfiguration(
            VecOptions options,
            LogInstance log,
            AnyUiContextPlusDialogs displayContext,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            IAssetAdministrationShell aas,
            AasCore.Aas3_0.Environment env)
        {
            var moduleBomSubmodels = FindBomSubmodels(env, aas).Where(sm => sm.IsConfigurationBom());
            var moduleEntitiesToSelect = moduleBomSubmodels.SelectMany(sm => sm.FindEntryNode().GetChildEntities());

            var dialogData = new AssociateSubassembliesWithModuleDialogResult();
            
            var uc = new AnyUiDialogueDataModalPanel("Select Module");
            uc.ActivateRenderPanel(
                dialogData,
                (uci) => RenderMainDialogPanel(moduleEntitiesToSelect, dialogData)
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogData;

            

        }

        private static AnyUiPanel RenderMainDialogPanel(IEnumerable<IEntity> moduleEntitiesToSelect, AssociateSubassembliesWithModuleDialogResult dialogData)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            var moduleNames = moduleEntitiesToSelect.Select(e => e.IdShort).ToArray();

            // specify module
            helper.AddSmallLabelTo(grid, 0, 0, content: "Module to Associate with Selected Subassemblies:");
            AnyUiUIElement.RegisterControl(
                helper.AddSmallComboBoxTo(grid, 0, 1, items: moduleNames),
                (text) =>
                {
                    dialogData.SelectedModule = moduleEntitiesToSelect.First(s => s.IdShort == text);
                    return new AnyUiLambdaActionNone();
                }
            );

            return panel;
        }
    }
}
