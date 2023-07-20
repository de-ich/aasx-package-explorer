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
    public static class CreateOrderDialog
    {
        class CreateOrderDialogData
        {
            public IEnumerable<Entity> SelectedEntities { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
        }

        public static async Task CreateOrderDialogBased(
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
                log.Error($"Create Order: One or multiple Entities have to be selected!");
                return;
            }

            if (selectedObjects.Any(e => !(e is Entity))) {
                log.Error($"Create Order: Only Entities may be selected!");
                return;
            }

            var selectedEntities = selectedObjects.Select(e => e as Entity);
            var submodelReferences = new HashSet<AasCore.Aas3_0.Reference>(selectedEntities.Select(e => e.GetParentSubmodel().GetReference() as AasCore.Aas3_0.Reference));
            var aas = env.AssetAdministrationShells.First(aas => submodelReferences.All(r => aas.HasSubmodelReference(r)));

            var dialogData = new CreateOrderDialogData();
            dialogData.SelectedEntities = selectedObjects.Select(e => e as Entity);
            
            var uc = new AnyUiDialogueDataModalPanel("Configure Order");
            uc.ActivateRenderPanel(dialogData,
                (uci) =>
                {
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var grid = helper.AddSmallGrid(1, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(grid);

                    // specify subassembly entity name
                    helper.AddSmallLabelTo(grid, 0, 0, content: "Order Number:");
                    AnyUiUIElement.SetStringFromControl(
                        helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogData.OrderNumber),
                        (text) => { dialogData.OrderNumber = text; }
                    );

                    return panel;
                }
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return;
            }

            log.Info($"Creating Order...");
            OrderCreator.CreateOrder(env, aas, dialogData.SelectedEntities, dialogData.OrderNumber, options, log);

        }

    }
}
