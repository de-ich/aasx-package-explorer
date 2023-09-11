/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Reflection;

using JetBrains.Annotations;
using System.Linq;
using System.Windows.Controls;
using AasCore.Aas3_0;
using AasxIntegrationBase;
using Extensions;
using AdminShellNS;
using System.Threading.Tasks;
using AnyUi;
using AasxPluginAml;
using static AasxPluginAml.Utils.AmlSMUtils;
using System.Windows.Forms;
using AasxPluginAml.Views;
using Aml.Engine.CAEX;
using AasxPluginAml.Utils;
using Aml.Engine.CAEX.Extensions;
using System.Windows;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AmlOptions _options = new AmlOptions();
        private AmlTreeView treeView = new AmlTreeView();

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginAml";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));
            
            // .. with built-in options
            _options = AmlOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AmlOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this._options = newOpt;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            _log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "call-check-visual-extension",
                    "When called with Referable, returns possibly visual extension for it."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-licenses", "Gets a description of used licenses."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase(
                "get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-menu-items", "Provides a list of menu items of the plugin to the caller."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "call-menu-item", "Caller activates a named menu item.", useAsync: true));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "fill-panel-visual-extension",
                    "When called, fill given WPF panel with control for graph display."));
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "call-check-visual-extension")
            {
                // arguments
                if (args.Length < 1)
                    return null;

                // looking only for Submodels
                var sm = args[0] as Submodel;
                if (sm == null)
                    return null;

                // check for a record in options, that matches Submodel
                var isAmlSubmodel = sm.GetAmlFile() != null;

                if (!isAmlSubmodel)
                    return null;
                // ReSharper enable UnusedVariable

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("AML", "AML Tree Viewer");

                // ok
                return cve;
            }

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AmlOptions>(
                    (args[0] as string));
                if (newOpt != null)
                    this._options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this._options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                /*lic.shortLicense = "The OpenXML SDK is under MIT license." + Environment.NewLine +
                    "The ClosedXML library is under MIT license." + Environment.NewLine +
                    "The ExcelNumberFormat number parser is licensed under the MIT license." + Environment.NewLine +
                    "The FastMember reflection access is licensed under Apache License 2.0 (Apache - 2.0).";*/
                lic.shortLicense = "";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "get-events" && _eventStack != null)
            {
                // try access
                return _eventStack.PopEvent();
            }

            if (action == "get-menu-items")
            {
                // result list 
                var res = new List<AasxPluginResultSingleMenuItem>();

                // import vec
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "PublishAMLAttribute",
                        Header = "AutomationML: Publish AML attribute as AAS property",
                        HelpText = "Publish an AML attribute as an AAS property that is linked to the attribute",
                        ArgDefs = new AasxMenuListOfArgDefs()
                    }
                });

                // return
                return new AasxPluginResultProvideMenuItems()
                {
                    MenuItems = res
                };
            }

            if (action == "get-check-visual-extension")
            {
                var cve = new AasxPluginResultBaseObject();
                cve.strType = "True";
                cve.obj = true;
                return cve;
            }

            if (action == "fill-panel-visual-extension")
            {
                // arguments
                if (args == null || args.Length < 3)
                    return null;

                var env = args[0] as AdminShellPackageEnv;
                var submodel = args[1] as Submodel;
                var panel = args[2] as DockPanel;

                object resobj = this.treeView.FillWithWpfControls(env, submodel, panel);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = resobj;
                return res;


            }

            // default
            return null;
        }

        /// <summary>
        /// Async variant of <c>ActivateAction</c>.
        /// Note: for some reason of type conversion, it has to return <c>Task<object></c>.
        /// </summary>
        public new async Task<object> ActivateActionAsync(string action, params object[] args)
        {
            if (action == "call-menu-item")
            {
                try
                {
                    HandleMenuItemCalled(args);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "when executing plugin menu item " + args[0] as string);
                }
            }

            // default
            return null;
        }

        private void HandleMenuItemCalled(object[] args)
        {
            IEnumerable<AasxPluginResultEventBase> resultEvents = null;

            var cmd = args[0] as string;

            var selectedObject = GetSelectedObject(args);
            var associatedSubmodel = GetAssociatedSubmodel(args);

            if (cmd == "publishamlattribute")
            {

                if (selectedObject == null || associatedSubmodel == null || selectedObject is not AttributeType)
                {
                    return;
                }

                var propertySmc = PublishAmlAttribute(selectedObject as AttributeType, associatedSubmodel);

                resultEvents = new List<AasxPluginResultEventBase>() {
                    new AasxPluginResultEventRedrawAllElements(),
                    new AasxPluginResultEventNavigateToReference()
                    {
                        targetReference = propertySmc.GetReference()
                    }
                };
            }

            resultEvents?.ToList().ForEach(r => _eventStack.PushEvent(r));
        }

        private CAEXObject? GetSelectedObject(params object[] args)
        {
            var amlViewerPanel = FindAmlViewerPanel(args[3] as DockPanel);

            return amlViewerPanel?.SelectedObject;
        }

        private Submodel? GetAssociatedSubmodel(params object[] args)
        {
            var amlViewerPanel = FindAmlViewerPanel(args[3] as DockPanel);

            return amlViewerPanel.AssociatedSubmodel;
        }

        private AmlViewerPanel FindAmlViewerPanel(DockPanel parent)
        {
            if (parent == null)
            {
                return null;
            }

            foreach(var child in parent.Children)
            {
                if (child is AmlViewerPanel)
                {
                    return child as AmlViewerPanel;
                }
            }

            return null;
        }
    }
}
