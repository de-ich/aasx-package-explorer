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

namespace AasxPluginAml.AnyUi
{
    public static class InitInterfaceConnectorsSmDialog
    {
        public class InitInterfaceConnectorsSmDialogResult
        {
            public int NumberOfPneumaticConnectors { get; set; } = 0;
            public int NumberOfElectricConnectors { get; set; } = 0;
            public int NumberOfMechanicConnectors { get; set;} = 0;
        }

        public static async Task<InitInterfaceConnectorsSmDialogResult> DetermineInitInterfaceConnectorsSmConfiguration(
            AnyUiContextPlusDialogs displayContext)
        {

            var dialogResult = new InitInterfaceConnectorsSmDialogResult();
            
            var uc = new AnyUiDialogueDataModalPanel("Configure Submodel");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(dialogResult)
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
;

        }

        private static AnyUiPanel RenderMainDialogPanel(InitInterfaceConnectorsSmDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(3, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            helper.AddSmallLabelTo(grid, 0, 0, content: "Number of Pneumatic Connectors:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.NumberOfPneumaticConnectors.ToString()),
                (text) => { dialogResult.NumberOfPneumaticConnectors = Int32.Parse(text); }
            );

            helper.AddSmallLabelTo(grid, 1, 0, content: "Number of Electric Connectors:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 1, 1, text: dialogResult.NumberOfElectricConnectors.ToString()),
                (text) => { dialogResult.NumberOfElectricConnectors = Int32.Parse(text); }
            );

            helper.AddSmallLabelTo(grid, 2, 0, content: "Number of Mechanic Connectors:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 2, 1, text: dialogResult.NumberOfMechanicConnectors.ToString()),
                (text) => { dialogResult.NumberOfMechanicConnectors = Int32.Parse(text); }
            );

            return panel;
        }
    }
}
