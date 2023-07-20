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

namespace AasxPluginVec.AnyUi
{
    public static class DeriveSubassemblyDialog
    {
        class DeriveSubassemblyDialogData
        {
            public IEnumerable<Entity> SelectedEntities { get; set; }
            public string SubassemblyAASName { get; set; } = string.Empty;
            public string SubassemblyEntityName { get; set; } = string.Empty;
            public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();
        }

        public static async Task DeriveSubassemblyDialogBased(
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
                log.Error($"Derive Subassembly: One or multiple Entities have to be selected!");
                return;
            }

            if (selectedObjects.Any(e => !(e is Entity))) {
                log.Error($"Derive Subassembly: Only Entities may be selected!");
                return;
            }

            var selectedEntities = selectedObjects.Select(e => e as Entity);
            var submodelReferences = new HashSet<AasCore.Aas3_0.Reference>(selectedEntities.Select(e => e.GetParentSubmodel().GetReference() as AasCore.Aas3_0.Reference));
            var aas = env.AssetAdministrationShells.First(aas => submodelReferences.All(r => aas.HasSubmodelReference(r)));

            var dialogData = new DeriveSubassemblyDialogData();
            dialogData.SelectedEntities = selectedObjects.Select(e => e as Entity);
            dialogData.SubassemblyAASName = string.Join("_", dialogData.SelectedEntities.Select(e => e.IdShort));
            dialogData.SubassemblyEntityName = "Subassembly_" + string.Join("_", dialogData.SelectedEntities.Select(e => e.IdShort));

            var uc = new AnyUiDialogueDataModalPanel("Configure Subassembly");
            uc.ActivateRenderPanel(dialogData,
                (uci) =>
                {
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(grid);

                    // specify subassembly entity name
                    helper.AddSmallLabelTo(grid, 0, 0, content: "Name of Subassembly Entity in existing AAS:");
                    AnyUiUIElement.SetStringFromControl(
                        helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogData.SubassemblyEntityName),
                        (text) => { dialogData.SubassemblyEntityName = text; }
                    );

                    // specify subassembly aas name
                    helper.AddSmallLabelTo(grid, 1, 0, content: "Name of new Subassembly AAS:");
                    AnyUiUIElement.SetStringFromControl(
                        helper.AddSmallTextBoxTo(grid, 1, 1, text: dialogData.SubassemblyAASName),
                        (text) => { dialogData.SubassemblyAASName = text; }
                    );

                    // specify name of subassembly parts
                    foreach(var entity in dialogData.SelectedEntities)
                    {
                        grid.RowDefinitions.Add(new AnyUiRowDefinition());
                        var currentRow = grid.RowDefinitions.Count() - 1;
                        helper.AddSmallLabelTo(grid, currentRow, 0, content: entity.IdShort);
                        AnyUiUIElement.SetStringFromControl(
                            helper.AddSmallTextBoxTo(grid, currentRow, 1, text: entity.IdShort),
                            (text) => { dialogData.PartNames[entity.IdShort] = text; }
                        );
                    }

                    return panel;
                }
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return;
            }

            log.Info($"Deriving subassembly...");
            SubassemblyDeriver.DeriveSubassembly(env, aas, dialogData.SelectedEntities, dialogData.SubassemblyAASName, dialogData.SubassemblyEntityName, dialogData.PartNames, options, log);

        }

    }
}
