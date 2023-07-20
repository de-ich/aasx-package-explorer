/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using AasxPluginVec;
using JetBrains.Annotations;
using System.Linq;
using System.Windows.Controls;
using AasCore.Aas3_0;
using AasxIntegrationBase;
using Extensions;
using AdminShellNS;
using System.Threading.Tasks;
using AnyUi;
using AasxPluginVec.AnyUi;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private VecOptions _options = new VecOptions();
        private VecTreeView treeView = new VecTreeView();

        public void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginVec";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = VecOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<VecOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this._options = newOpt;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public AasxPluginActionDescriptionBase[] ListActions()
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
            /*
            res.Add(new AasxPluginActionDescriptionBase(
                "associate-subassemblies-with-module", "Associate an orderable module with the subassemblies required for production."));
            res.Add(new AasxPluginActionDescriptionBase(
                "create-order", "Create a new wire harness order for the selected modules."));*/
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "fill-panel-visual-extension",
                    "When called, fill given WPF panel with control for graph display."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
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
                var isVecSubmodel = sm.SemanticId?.Matches(KeyTypes.Submodel, VecSMUtils.SEM_ID_VEC_SUBMODEL) ?? false;

                if (!isVecSubmodel)
                    return null;
                // ReSharper enable UnusedVariable

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("VEC", "VEC Tree Viewer");

                // ok
                return cve;
            }

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginVec.VecOptions>(
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
                    AttachPoint = "Import",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ImportVEC",
                        Header = "Import a VEC file into an AAS",
                        HelpText = "Import a VEC file into an AAS and create the related submodels.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                    }
                });

                // derive subassembly
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "DeriveSubassembly",
                        Header = "Derive new subassembly from selected entities",
                        HelpText = "Derive new subassembly based on selected entities (components and/or subassemblies) and create the required admin shell.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                    }
                });

                // reuse subassembly
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ReuseSubassembly",
                        Header = "Reuse existing subassembly for selected entities",
                        HelpText = "Reuse an existing subassembly for the selected entities (components).",
                        ArgDefs = new AasxMenuListOfArgDefs()
                    }
                });

                // return
                return new AasxPluginResultProvideMenuItems()
                {
                    MenuItems = res
                };
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
                if (args != null && args.Length >= 3
                    && args[0] is string cmd
                    && args[1] is AasxMenuActionTicket ticket
                    && args[2] is AnyUiContextPlusDialogs displayContext)
                {
                    try
                    {
                        if (cmd == "importvec")
                        {
                            await ImportVecDialog.ImportVECDialogBased(_options, _log, ticket, displayContext);
                            return new AasxPluginResultBase();
                        }

                        if (cmd == "derivesubassembly")
                        {
                            await AasxPluginVec.AnyUi.DeriveSubassemblyDialog.DeriveSubassemblyDialogBased(_options, _log, ticket, displayContext);
                            return new AasxPluginResultBase();
                        }

                        if (cmd == "reusesubassembly")
                        {
                            await AasxPluginVec.AnyUi.ReuseSubassemblyDialog.ReuseSubassemblyDialogBased(_options, _log, ticket, displayContext);
                            return new AasxPluginResultBase();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when executing plugin menu item " + cmd);
                    }
                }
            }


            // default
            return null;

            if (action == "associate-subassemblies-with-module" && args != null && args.Length >= 4)
            {
                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                var env = args[1] as AasCore.Aas3_0.Environment;
                var aas = args[2] as AssetAdministrationShell;
                var selectedEntities = (args[3] as IEnumerable<Entity>)?.ToList();

                if (fop == null || env == null || aas == null || selectedEntities == null)
                {
                    return null;
                }

                //TODO ab hier implementieren
                var dlg = new AssociateSubassembliesWithModuleDialog(fop?.GetWin32Window(), selectedEntities, aas, env);

                fop?.StartFlyover(new EmptyFlyout());
                var fnres = dlg.ShowDialog();
                fop?.CloseFlyover();
                if (fnres != true)
                    return null;

                var selectedModuleEntity = dlg.SelectedModule;

                SubassemblyToModuleAssociator.AssociateSubassemblies(env, aas, selectedEntities, selectedModuleEntity, _options, _log);

                _log.Info($"Associating module with subassembly...");
            }

            if (action == "create-order" && args != null && args.Length >= 4)
            {
                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                var env = args[1] as AasCore.Aas3_0.Environment;
                var aas = args[2] as AssetAdministrationShell;
                var selectedModules = (args[3] as IEnumerable<Entity>)?.ToList();

                if (fop == null || env == null || aas == null || selectedModules == null)
                {
                    return null;
                }

                //TODO ab hier implementieren
                var dlg = new CreateOrderDialog(fop?.GetWin32Window());

                fop?.StartFlyover(new EmptyFlyout());
                var fnres = dlg.ShowDialog();
                fop?.CloseFlyover();
                if (fnres != true)
                    return null;

                var orderNumber = dlg.OrderNumber;

                OrderCreator.CreateOrder(env, aas, selectedModules, orderNumber, _options, _log);

                _log.Info($"Creating order...");
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
    }
}
