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

namespace AasxPluginVec.AnyUi
{
    public static class AssociateSubassembliesWithModuleDialog
    {
        class AssociateSubassembliesWithModuleDialogData
        {
            public IEnumerable<Entity> SelectedEntities { get; set; }
            public Entity SelectedModule { get; set; }
        }

        public static async Task AssociateSubassembliesWithModuleDialogBased(
            VecOptions options,
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext)
        {
            // which Submodel
            var packageEnv = ticket.Package;
            var env = ticket.Env;
            var selectedObjects = ticket.SelectedDereferencedMainDataObjects;
            if (packageEnv == null || env == null || selectedObjects == null || selectedObjects.Count() == 0)
            {
                log.Error($"Reuse Subassembly: One or multiple Entities have to be selected!");
                return;
            }

            if (selectedObjects.Any(e => e is not Entity)) {
                log.Error($"Reuse Subassembly: Only Entities may be selected!");
                return;
            }

            var selectedEntities = selectedObjects.Select(e => e as Entity);
            var submodelReferences = new HashSet<AasCore.Aas3_0.Reference>(selectedEntities.Select(e => e.GetParentSubmodel().GetReference() as AasCore.Aas3_0.Reference));
            var aas = env.AssetAdministrationShells.First(aas => submodelReferences.All(r => aas.HasSubmodelReference(r)));

            var moduleBomSubmodels = FindBomSubmodels(aas, env).Where(sm => FindEntryNode(sm)?.EnumerateChildren().Where(c => c is Entity).All(c => (c as Entity).EntityType == EntityType.CoManagedEntity) ?? false);
            var moduleEntitiesToSelect = moduleBomSubmodels.Select(sm => FindEntryNode(sm)).SelectMany(e => e.EnumerateChildren()).Where(c => c is Entity).Select(c => c as Entity);

            var dialogData = new AssociateSubassembliesWithModuleDialogData();
            dialogData.SelectedEntities = selectedObjects.Select(e => e as Entity);
            
            var uc = new AnyUiDialogueDataModalPanel("Select Module");
            uc.ActivateRenderPanel(dialogData,
                (uci) =>
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
                        (text) => { 
                            dialogData.SelectedModule = moduleEntitiesToSelect.First(s => s.IdShort == text);
                            return new AnyUiLambdaActionNone();
                        }
                    );

                    return panel;
                }
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return;
            }

            log.Info($"Associating subassemblies with module...");
            SubassemblyToModuleAssociator.AssociateSubassemblies(env, aas, dialogData.SelectedEntities, dialogData.SelectedModule, options, log);

        }

    }
}
