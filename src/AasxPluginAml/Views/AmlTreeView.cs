using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Controls;
using AasCore.Aas3_0;
using Extensions;
using System.Xml;
using Aml.Engine.CAEX;
using AasxPluginAml.Utils;
using AasxPluginAml.Views;

namespace AasxPluginAml
{
    class AmlTreeView
    {
        private AdminShellPackageEnv env;
        private Submodel submodel;
        private DockPanel panel;
        private File amlFileElement;
        private CAEXDocument amlDocument;

        public object FillWithWpfControls(
            AdminShellPackageEnv env, Submodel submodel, DockPanel panel)
        {
            if (env == null || submodel == null || panel == null)
            {
                return null;
            }

            this.env = env;
            this.submodel = submodel;
            this.panel = panel;

            amlFileElement = submodel.GetAmlFile();

            if (amlFileElement == null)
            {
                return null;
            }
            
            LoadAmlFile();

            if (amlDocument == null)
            {
                return null;
            }

            var amlViewerPanel = new AmlViewerPanel(amlDocument, submodel);
            this.panel.Children.Add(amlViewerPanel);

            return amlViewerPanel;
        }

        private void LoadAmlFile()
        {
            var amlFile = env.GetListOfSupplementaryFiles().Find(f => f.Uri.ToString() == amlFileElement.Value);
            using var amlDocumentStream = env.GetLocalStreamFromPackage(amlFile.Uri.ToString());

            amlDocument = CAEXDocument.LoadFromStream(amlDocumentStream);
        }
    }
}
