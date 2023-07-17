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
using AdminShellNS;
using JetBrains.Annotations;
using System.Linq;
using System.Windows.Controls;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();
        private PluginEventStack _eventStack = new PluginEventStack();
        private VecOptions options = new VecOptions();
        private VecTreeView treeView = new VecTreeView();

        public string GetPluginName()
        {
            return "AasxPluginVec";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = VecOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<VecOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this.options = newOpt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public object CheckForLogMessage()
        {
            return Log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            Log.Info("ListActions() called");
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
            res.Add(new AasxPluginActionDescriptionBase(
                "import-vec", "Import VEC file and create BOM submodel."));
            res.Add(new AasxPluginActionDescriptionBase(
                "derive-subassembly", "Derive new subassembly from selected entities."));
            res.Add(new AasxPluginActionDescriptionBase(
                "reuse-subassembly", "Reuse existing subassembly for selected entities."));
            res.Add(new AasxPluginActionDescriptionBase(
                "associate-subassemblies-with-module", "Associate an orderable module with the subassemblies required for production."));
            res.Add(new AasxPluginActionDescriptionBase(
                "create-order", "Create a new wire harness order for the selected modules."));
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
                var sm = args[0] as AdminShell.Submodel;
                if (sm == null)
                    return null;

                // check for a record in options, that matches Submodel
                var isVecSubmodel = sm.semanticId?.Matches("Submodel", true, "IRI", VecSMUtils.SEM_ID_VEC_SUBMODEL) ?? false;

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
                    this.options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this.options, Newtonsoft.Json.Formatting.Indented);
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

            if (action == "import-vec"
                && args != null && args.Length >= 3
                && args[0] is IFlyoutProvider 
                && args[1] is AdminShellPackageEnv
                && args[2] is AdminShell.AdministrationShellEnv
                && args[3] is AdminShellV20.AdministrationShell)
            {
                var fn = (args.Length >= 5) ? args[4] as string : null;

                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                // which Submodel
                var packageEnv = args[1] as AdminShellPackageEnv;
                var env = args[2] as AdminShell.AdministrationShellEnv;
                var aas = args[3] as AdminShellV20.AdministrationShell;
                if (packageEnv == null || env == null || aas == null)
                    return null;

                // ask for filename
                var dlg = new Microsoft.Win32.OpenFileDialog();
                try
                {
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(
                        System.AppDomain.CurrentDomain.BaseDirectory);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
                dlg.Title = "Select VEC file to import ..";
                dlg.DefaultExt = "*.vec";
                dlg.Filter = "VEC container files (*.vec)|*.vec|Alle Dateien (*.*)|*.*";

                fop?.StartFlyover(new EmptyFlyout());
                var fnres = dlg.ShowDialog(fop?.GetWin32Window());
                fop?.CloseFlyover();
                if (fnres != true)
                    return null;
                fn = dlg.FileName;

                // use functionality
                Log.Info($"Importing VEC container from file: {fn} ..");
                VecImporter.ImportVecFromFile(packageEnv, env, aas, fn, options, Log);
            }

            if (action == "derive-subassembly" && args != null && args.Length >= 4)
            {
                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                var env = args[1] as AdminShellV20.AdministrationShellEnv;
                var aas = args[2] as AdminShellV20.AdministrationShell;
                var selectedEntities = (args[3] as IEnumerable<AdminShellV20.Entity>)?.ToList();

                if (fop == null || env == null || aas == null || selectedEntities == null)
                {
                    return null;
                }

                // ask for filename
                var dlg = new DeriveSubassemblyDialog(fop?.GetWin32Window(), selectedEntities);

                fop?.StartFlyover(new EmptyFlyout());
                var fnres = dlg.ShowDialog();
                fop?.CloseFlyover();
                if (fnres != true)
                    return null;

                var subassemblyAASName = dlg.SubassemblyAASName;
                var subassemblyEntityName = dlg.SubassemblyEntityName;
                var partNames = dlg.PartNames;

                SubassemblyDeriver.DeriveSubassembly(env, aas, selectedEntities, subassemblyAASName, subassemblyEntityName, partNames, options, Log);

                Log.Info($"Deriving subassembly...");
            }

            if (action == "reuse-subassembly" && args != null && args.Length >= 4)
            {
                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                var env = args[1] as AdminShellV20.AdministrationShellEnv;
                var aas = args[2] as AdminShellV20.AdministrationShell;
                var selectedEntities = (args[3] as IEnumerable<AdminShellV20.Entity>)?.ToList();

                if (fop == null || env == null || aas == null || selectedEntities == null)
                {
                    return null;
                }

                // ask for filename
                var dlg = new ReuseSubassemblyNameDialog(fop?.GetWin32Window(), selectedEntities, env);

                fop?.StartFlyover(new EmptyFlyout());
                var fnres = dlg.ShowDialog();
                fop?.CloseFlyover();
                if (fnres != true)
                    return null;

                //var subassemblyAASName = dlg.SubassemblyAASName;
                var subassemblyEntityName = dlg.SubassemblyEntityName;
                var nameOfAasToReuse = dlg.AasToReuse;
                var partNames = dlg.PartNames;

                SubassemblyReuser.ReuseSubassembly(env, aas, selectedEntities, nameOfAasToReuse, subassemblyEntityName, partNames, options, Log);

                Log.Info($"Reusing subassembly...");
            }

            if (action == "associate-subassemblies-with-module" && args != null && args.Length >= 4)
            {
                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                var env = args[1] as AdminShellV20.AdministrationShellEnv;
                var aas = args[2] as AdminShellV20.AdministrationShell;
                var selectedEntities = (args[3] as IEnumerable<AdminShellV20.Entity>)?.ToList();

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

                SubassemblyToModuleAssociator.AssociateSubassemblies(env, aas, selectedEntities, selectedModuleEntity, options, Log);

                Log.Info($"Associating module with subassembly...");
            }

            if (action == "create-order" && args != null && args.Length >= 4)
            {
                // flyout provider (will be required in the future)
                var fop = args[0] as IFlyoutProvider;

                var env = args[1] as AdminShellV20.AdministrationShellEnv;
                var aas = args[2] as AdminShellV20.AdministrationShell;
                var selectedModules = (args[3] as IEnumerable<AdminShellV20.Entity>)?.ToList();

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

                OrderCreator.CreateOrder(env, aas, selectedModules, orderNumber, options, Log);

                Log.Info($"Creating order...");
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
                var submodel = args[1] as AdminShellV20.Submodel;
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
