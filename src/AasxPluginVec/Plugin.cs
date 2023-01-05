/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AasxPluginVec;
using AdminShellNS;
using AnyUi;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();
        private PluginEventStack _eventStack = new PluginEventStack();
        private AasxPluginVec.VecOptions options = new AasxPluginVec.VecOptions();

        public string GetPluginName()
        {
            return "AasxPluginVec";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AasxPluginVec.VecOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginVec.VecOptions>(
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
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
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

            // default
            return null;
        }
    }
}
