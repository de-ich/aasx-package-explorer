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
    public static class DeriveAasDialog
    {
        public class DeriveAasDialogResult
        {
            public string NameOfDerivedAas { get; set; } = string.Empty;
        }

        public static async Task<DeriveAasDialogResult> DetermineDeriveAasConfiguration(
            AnyUiContextPlusDialogs displayContext,
            IAssetAdministrationShell aasToDeriveFrom)
        {
            
            var dialogResult = new DeriveAasDialogResult();
            
            var uc = new AnyUiDialogueDataModalPanel("Configure Derive AAS");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(aasToDeriveFrom.IdShort, dialogResult)
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
;

        }

        private static AnyUiPanel RenderMainDialogPanel(string nameOfAasToDeriveFrom, DeriveAasDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(1, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify name of the derived AAS
            helper.AddSmallLabelTo(grid, 0, 0, content: "Name of Derived AAS:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: nameOfAasToDeriveFrom),
                (text) => { dialogResult.NameOfDerivedAas = text; }
            );

            return panel;
        }
    }
}
