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
    public static class GenerateAmlDialog
    {
        public class GenerateAmlDialogResult
        {
            public string SucLibName { get; set; } = string.Empty;
            public string SucName { get; set; } = string.Empty;
            public string SubmodelName { get; set;} = "AutomationML";
        }

        public static async Task<GenerateAmlDialogResult> DetermineCreateAmlConfiguration(
            AnyUiContextPlusDialogs displayContext, string manufacturerName, string articleNumber)
        {

            var dialogResult = new GenerateAmlDialogResult()
            {
                SucLibName = manufacturerName ?? "",
                SucName = articleNumber ?? ""
            };
            
            var uc = new AnyUiDialogueDataModalPanel("Configure AML File");
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

        private static AnyUiPanel RenderMainDialogPanel(GenerateAmlDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(3, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify SUClib name
            helper.AddSmallLabelTo(grid, 0, 0, content: "Name of the SystemUnitClassLib:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.SucLibName),
                (text) => { dialogResult.SucLibName = text; }
            );

            // specify SUC name
            helper.AddSmallLabelTo(grid, 1, 0, content: "Name of the SystemUnitClass:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 1, 1, text: dialogResult.SucName),
                (text) => { dialogResult.SucName = text; }
            );

            // specify submodel name
            helper.AddSmallLabelTo(grid, 2, 0, content: "Name of the Submodel:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 2, 1, text: dialogResult.SubmodelName),
                (text) => { dialogResult.SubmodelName = text; }
            );

            return panel;
        }
    }
}
