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
    public static class ReuseSubassemblyDialog
    {
        class ReuseSubassemblyDialogData
        {
            public IEnumerable<IAssetAdministrationShell> Shells;
            public IEnumerable<Entity> SelectedEntities { get; set; }
            public string SubassemblyEntityName { get; set; } = string.Empty;

            public IAssetAdministrationShell AasToReuse { get; set; }
            public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();
        }

        public static async Task ReuseSubassemblyDialogBased(
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

            if (selectedObjects.Any(e => !(e is Entity))) {
                log.Error($"Reuse Subassembly: Only Entities may be selected!");
                return;
            }

            var selectedEntities = selectedObjects.Select(e => e as Entity);
            var submodelReferences = new HashSet<AasCore.Aas3_0.Reference>(selectedEntities.Select(e => e.GetParentSubmodel().GetReference() as AasCore.Aas3_0.Reference));
            var aas = env.AssetAdministrationShells.First(aas => submodelReferences.All(r => aas.HasSubmodelReference(r)));

            var dialogData = new ReuseSubassemblyDialogData();
            dialogData.SelectedEntities = selectedObjects.Select(e => e as Entity);
            dialogData.Shells = env.AssetAdministrationShells;
            dialogData.SubassemblyEntityName = "Subassembly_" + string.Join("_", dialogData.SelectedEntities.Select(e => e.IdShort));
            
            var shellNames = dialogData.Shells.Select(s => s.IdShort).ToArray();

            var uc = new AnyUiDialogueDataModalPanel("Configure Subassembly");
            uc.ActivateRenderPanel(dialogData,
                (uci) =>
                {
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var grid = helper.AddSmallGrid(3, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(grid);

                    var mapPartsGrid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));

                    // specify subassembly entity name
                    helper.AddSmallLabelTo(grid, 0, 0, content: "Name of Subassembly Entity in existing AAS:");
                    AnyUiUIElement.SetStringFromControl(
                        helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogData.SubassemblyEntityName),
                        (text) => { dialogData.SubassemblyEntityName = text; }
                    );

                    // specify subassembly to reuse
                    helper.AddSmallLabelTo(grid, 1, 0, content: "Subassembly AAS to Reuse:");
                    AnyUiUIElement.RegisterControl(
                        helper.AddSmallComboBoxTo(grid, 1, 1, items: shellNames, selectedIndex: shellNames.ToList().IndexOf(dialogData.AasToReuse?.IdShort)),
                        (text) => { 
                            dialogData.AasToReuse = dialogData.Shells.First(s => s.IdShort == text);
                            return new AnyUiLambdaActionModalPanelReRender(uc);
                        }
                    );

                    // specify name of subassembly parts

                    var lab = new AnyUiSelectableTextBlock();
                    lab.Text = "Map Subassembly Parts:";
                    panel.Add(lab);
                    var mapPartsPanel = new AnyUiStackPanel();
                    panel.Add(mapPartsPanel);
                    
                    mapPartsPanel.Add(mapPartsGrid);

                    var bomSubmodel = FindFirstBomSubmodel(dialogData.AasToReuse, env);
                    var atomicComponentEntities = GetLeafNodes(bomSubmodel);

                    foreach (var entity in atomicComponentEntities)
                    {
                        mapPartsGrid.RowDefinitions.Add(new AnyUiRowDefinition());
                        var currentRow = mapPartsGrid.RowDefinitions.Count() - 1;
                        helper.AddSmallLabelTo(mapPartsGrid, currentRow, 0, content: entity.IdShort);
                        AnyUiUIElement.RegisterControl(
                            helper.AddSmallComboBoxTo(mapPartsGrid, currentRow, 1, items: dialogData.SelectedEntities.Select(e => e.IdShort).ToArray()),
                            (text) =>
                            {
                                dialogData.PartNames[text as string] = entity.IdShort;
                                return new AnyUiLambdaActionNone();
                            }
                        );
                    }


                    return panel;
                }
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return;
            }

            log.Info($"Reusing subassembly...");
            SubassemblyReuser.ReuseSubassembly(env, aas, dialogData.SelectedEntities, dialogData.AasToReuse, dialogData.SubassemblyEntityName, dialogData.PartNames, options, log);

        }

    }
}
