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
using System.Windows.Forms;
using static AasxPluginVec.BasicAasUtils;
using static AasxPluginVec.SubassemblyUtils;

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
                        Header = "VWS4LS: Import a VEC file into an AAS",
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
                        Header = "VWS4LS: Derive new subassembly from selected components",
                        HelpText = "Derive new subassembly based on selected entities (components in a product bom and/or subassemblies in a manufacturing bom) and create the required admin shell.",
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
                        Header = "VWS4LS: Reuse existing subassembly for selected components",
                        HelpText = "Reuse an existing subassembly for the selected entities (components from a product bom).",
                        ArgDefs = new AasxMenuListOfArgDefs()
                    }
                });

                // associated subassembly with configuration
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "AssociateSubassembliesWithConfiguration",
                        Header = "VWS4LS: Associate selected subassemblies with a configuration",
                        HelpText = "Associate the selected entities (subassemblies from a manufacturing bom) with a configuration in a configuration bom.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                    }
                });

                // create order
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "CreateOrder",
                        Header = "VWS4LS: Create wire harness order for selected configurations",
                        HelpText = "Create a new wire harness order for the selected entities (configurations from a configuration bom).",
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
                try { 
                    CheckArguments(args);

                    var cmd = args[0] as string;
                    var ticket = args[1] as AasxMenuActionTicket;
                    var displayContext = args[2] as AnyUiContextPlusDialogs;

                    // make sure all parents are set so that we do not need to deal with this later
                    ticket.Env.Submodels.ForEach(s => s.SetAllParents());

                    IEnumerable<AasxPluginResultEventBase> resultEvents = null;

                    if (cmd == "importvec")
                    {
                        resultEvents = await ExecuteImportVEC(ticket, displayContext);
                    }

                    if (cmd == "derivesubassembly")
                    {
                        resultEvents = await ExecuteDeriveSubassembly(ticket, displayContext);
                    }

                    if (cmd == "reusesubassembly")
                    {
                        resultEvents = await ExecuteReuseSubassembly(ticket, displayContext);
                    }

                    if (cmd == "associatesubassemblieswithconfiguration")
                    {
                        resultEvents = await ExecuteAssociateSubassembliesWithConfiguration(ticket, displayContext);
                    }

                    if (cmd == "createorder")
                    {
                        resultEvents = await ExecuteCreateOrder(ticket, displayContext);
                    }

                    resultEvents?.ToList().ForEach(r => _eventStack.PushEvent(r));

                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "when executing plugin menu item " + args[0] as string);
                }
            }

            // default
            return null;
        }

        private static void CheckArguments(object[] args)
        {
            if (args == null || args.Length < 3
                    || args[0] is not string cmd
                    || args[1] is not AasxMenuActionTicket ticket
                    || args[2] is not AnyUiContextPlusDialogs)
            {
                throw new ArgumentException("Internal error: Expected the three arguments (cmd, ticket, displayContext)!");
            }
            
            if (ticket.Package == null || ticket.Env == null)
            {
                throw new ArgumentException($"Internal error: Unable to determine open AASX package!");
            }
        }

        private async Task<IEnumerable<AasxPluginResultEventBase>> ExecuteImportVEC(AasxMenuActionTicket ticket, AnyUiContextPlusDialogs displayContext)
        {
            if (ticket.AAS == null)
            {
                throw new ArgumentException($"Import VEC: An AAS has to be selected!");
            }

            var package = ticket.Package;
            var env = ticket.Env;
            var aas = ticket.AAS;
            var fileName = await ImportVecDialog.DetermineVecFileToImport(_options, _log, displayContext);

            if (fileName == null)
            {
                return null;
            }

            _log.Info($"Importing VEC container from file: {fileName} ..");
            var vecSubmodel = VecImporter.ImportVecFromFile(package, env, aas, fileName, _options, _log);
            
            return new List<AasxPluginResultEventBase>() { 
                new AasxPluginResultEventRedrawAllElements(),
                new AasxPluginResultEventNavigateToReference()
                {
                    targetReference = vecSubmodel.GetReference()
                }
            };

        }

        private async Task<IEnumerable<AasxPluginResultEventBase>> ExecuteDeriveSubassembly(AasxMenuActionTicket ticket, AnyUiContextPlusDialogs displayContext)
        {
            var env = ticket.Env;
            var selectedEntities = GetSelectedEntitiesFromTicket(ticket);
            var aas = GetAasContainingElements(selectedEntities, env);

            if (aas == null)
            {
                throw new ArgumentException($"Derive Subassembly: Unable to determine the (single) AAS containing the selected entities!");
            }

            var worker = new SubassemblyDeriver(env, aas, _options);
            worker.ValidateSelection(selectedEntities);

            var result = await DeriveSubassemblyDialog.DetermineDeriveSubassemblyConfiguration(displayContext, selectedEntities);

            if (result == null)
            {
                return null;
            }

            _log.Info($"Deriving subassembly...");
            var subassemblyEntity = worker.DeriveSubassembly(selectedEntities, result.SubassemblyAASName, result.SubassemblyEntityName, result.PartNames);

            return new List<AasxPluginResultEventBase>()
            {
                new AasxPluginResultEventRedrawAllElements(),
                new AasxPluginResultEventNavigateToReference()
                {
                    targetReference = subassemblyEntity.GetReference()
                }

            };
        }

        private async Task<IEnumerable<AasxPluginResultEventBase>> ExecuteReuseSubassembly(AasxMenuActionTicket ticket, AnyUiContextPlusDialogs displayContext)
        {
            var env = ticket.Env;
            var selectedEntities = GetSelectedEntitiesFromTicket(ticket);
            var aas = GetAasContainingElements(selectedEntities, env);

            if (aas == null)
            {
                throw new ArgumentException($"Reuse Subassembly: Unable to determine the (single) AAS containing the selected entities!");
            }

            var worker = new SubassemblyReuser(env, aas, _options);
            worker.ValidateSelection(selectedEntities);

            var result = await ReuseSubassemblyDialog.DetermineReuseSubassemblyConfiguration(displayContext, selectedEntities, env);

            if (result == null)
            {
                return null;
            }
                
            _log.Info($"Reusing subassembly...");
            var subassemblyEntity = worker.ReuseSubassembly(selectedEntities, result.AasToReuse, result.SubassemblyEntityName, result.PartNames);

            return new List<AasxPluginResultEventBase>()
            {
                new AasxPluginResultEventRedrawAllElements(),
                new AasxPluginResultEventNavigateToReference()
                {
                    targetReference = subassemblyEntity.GetReference()
                }
            };
        }

        private async Task<IEnumerable<AasxPluginResultEventBase>> ExecuteAssociateSubassembliesWithConfiguration(AasxMenuActionTicket ticket, AnyUiContextPlusDialogs displayContext)
        {
            var env = ticket.Env;
            var selectedEntities = GetSelectedEntitiesFromTicket(ticket);
            var aas = GetAasContainingElements(selectedEntities, env);

            if (aas == null)
            {
                throw new ArgumentException($"Reuse Subassembly: Unable to determine the (single) AAS containing the selected entities!");
            }

            var worker = new SubassemblyToConfigurationAssociator(env, aas, _options);
            worker.ValidateSelection(selectedEntities);

            var result = await AssociateSubassembliesWithModuleDialog.DetermineAssociateSubassembliesWithModuleConfiguration(displayContext, selectedEntities, aas, env);

            if (result == null)
            {
                return null;
            }

            _log.Info($"Associating subassemblies with configuration...");
            worker.AssociateSubassemblies(selectedEntities, result.SelectedConfiguration);

            return new List<AasxPluginResultEventBase>()
            {
                new AasxPluginResultEventRedrawAllElements(),
                new AasxPluginResultEventNavigateToReference()
                {
                    targetReference = result.SelectedConfiguration.GetReference()
                }
            };
        }

        private async Task<IEnumerable<AasxPluginResultEventBase>> ExecuteCreateOrder(AasxMenuActionTicket ticket, AnyUiContextPlusDialogs displayContext)
        {
            var env = ticket.Env;
            var selectedEntities = GetSelectedEntitiesFromTicket(ticket);
            var aas = GetAasContainingElements(selectedEntities, env);

            if (aas == null)
            {
                throw new ArgumentException($"Reuse Subassembly: Unable to determine the (single) AAS containing the selected entities!");
            }

            var worker = new OrderCreator(env, aas, _options);
            worker.ValidateSelection(selectedEntities);

            var result = await CreateOrderDialog.DetermineCreateOrderConfiguration(displayContext);

            if (result == null) {
                return null;
            }
             
            _log.Info($"Creating Order...");
            var orderAas = worker.CreateOrder(selectedEntities, result.OrderNumber);
            
            return new List<AasxPluginResultEventBase>()
            {
                new AasxPluginResultEventRedrawAllElements(),
                new AasxPluginResultEventNavigateToReference()
                {
                    targetReference = orderAas.GetReference()
                }
            };
        }

        private static IEnumerable<Entity> GetSelectedEntitiesFromTicket(AasxMenuActionTicket ticket)
        {
            var selectedObjects = ticket.SelectedDereferencedMainDataObjects;
            if (selectedObjects == null || selectedObjects.Count() == 0)
            {
                throw new ArgumentException($"One or multiple Entities have to be selected!");
            }

            if (selectedObjects.Any(e => e is not Entity))
            {
                throw new ArgumentException($"Only Entities may be selected!");
            }

            return selectedObjects.Select(e => e as Entity);
        }
    }
}
