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
using System.Xml.Linq;
using AasxIntegrationBase;
using AdminShellNS;
using static AdminShellNS.AdminShellV20;
using static AasxPluginVec.BomSMUtils;
using static AasxPluginVec.VecSMUtils;
using static AasxPluginVec.BasicAasUtils;
using static AasxPluginVec.SubassemblyUtils;

namespace AasxPluginVec
{
    /// <summary>
    /// This class allows to create an order based on a set of selected orderable modules.
    /// </summary>
    public class OrderCreator
    {
        //
        // Public interface
        //

        public static void CreateOrder(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> selectedModules,
            string orderNumber,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var creator = new OrderCreator(env, aas, selectedModules, orderNumber, options, log);
                creator.CreateOrder();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"creating order");
            }
        }

        //
        // Internal
        //

        protected OrderCreator(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> selectedModules,
            string orderNumber,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.selectedModules = selectedModules ?? throw new ArgumentNullException(nameof(selectedModules));
            this.orderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));
            this.orderAas = null;
            this.orderedModulesSubmodel = null;
            this.orderBuildingBlocksSubmodel = null;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected AdministrationShellEnv env;
        protected AdministrationShell aas;
        protected IEnumerable<Entity> selectedModules;
        protected string orderNumber;
        protected AdministrationShell orderAas;
        protected Submodel orderedModulesSubmodel;
        protected Submodel orderBuildingBlocksSubmodel;
        protected VecOptions options;
        protected LogInstance log;

        protected void CreateOrder()
        {
            var allBomSubmodels = FindBomSubmodels(aas, env);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (!selectedModules.All(HasAssociatedSubassemblies))
            {
                log?.Error("It seems that a module was selected that has no associated subassemblies required for production!");
                return;
            }

            var submodelsContainingSelectedModules = selectedModules.Select(e => e.FindParentFirstIdentifiable() as Submodel).ToHashSet();

            if (submodelsContainingSelectedModules.Count == 0)
            {
                log?.Error("Unable to determine BOM submodel that contains the selected modules!");
                return;
            }

            if (submodelsContainingSelectedModules.Count > 1)
            {
                log?.Error("Modules from more than 1 BOM submodel selected. This is not supported!");
                return;
            }

            var associatedSubassemblies = selectedModules.SelectMany(m => FindAssociatedSubassemblies(m, env));

            if (associatedSubassemblies.Any(s => s == null))
            {
                log?.Error("At least one subassembly associated with a selected module could not be determined!");
                return;
            }

            orderAas = CreateOrderAas(aas.idShort + "_Order_" + orderNumber);

            var orderedModulesSubmodelIdShort = "LS_BOM_OrderedModules";
            orderedModulesSubmodel = InitializeBomSubmodel(orderedModulesSubmodelIdShort);
            var orderedModuleEntryNode = CreateEntryNode(orderedModulesSubmodel, this.aas.assetRef);

            foreach(var module in selectedModules)
            {
                CreateHasPartRelationship(orderedModuleEntryNode, module);
            }

            orderBuildingBlocksSubmodel = InitializeBuildingBlocksSubmodel();
            var orderBuildingBlocksEntryNode = CreateEntryNode(orderBuildingBlocksSubmodel, this.aas.assetRef);

            foreach(var associatedSubassembly in associatedSubassemblies)
            {
                var buildingBlockEntity = CreateEntity(associatedSubassembly.idShort, orderBuildingBlocksEntryNode, semanticId: associatedSubassembly.semanticId);
                buildingBlockEntity.entityType = ASSET_TYPE_SELF_MANAGED_ENTITY; // set to 'self-managed' although we do not yet now the specific instance that will be used in production

                CreateHasPartRelationship(orderBuildingBlocksEntryNode, buildingBlockEntity);
                CreateSameAsRelationship(buildingBlockEntity, associatedSubassembly, buildingBlockEntity);
            }
        }

        protected AdministrationShell CreateOrderAas(string idShort)
        {
            var aas = new AdministrationShell();
            aas.idShort = idShort;
            aas.identification = new Identification(new Key("AssetAdministrationShell", false, "IRI", GenerateIdAccordingTemplate(options.TemplateIdAas)));

            var asset = new Asset();
            asset.idShort = aas.idShort + "_Asset";
            asset.identification = new Identification(new Key("Asset", false, "IRI", GenerateIdAccordingTemplate(options.TemplateIdAsset)));
            aas.assetRef = asset.GetAssetReference();

            this.orderAas = aas;
            this.env.AdministrationShells.Add(aas);
            this.env.Assets.Add(asset);

            return aas;
        }

        protected Submodel InitializeBomSubmodel(string idShort)
        {
            var id = GenerateIdAccordingTemplate(options.TemplateIdSubmodel);

            // create the BOM submodel
            var bomSubmodel = CreateBomSubmodel(idShort, id);

            env.Submodels.Add(bomSubmodel);
            orderAas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());

            return bomSubmodel;
        }
        protected Submodel InitializeBuildingBlocksSubmodel()
        {
            var id = GenerateIdAccordingTemplate(options.TemplateIdSubmodel);
            var idShort = "LS_BOM_BuildingBlocks";

            var buildingBlocksSubmodel = CreateBomSubmodel(idShort, id);

            env.Submodels.Add(buildingBlocksSubmodel);
            orderAas.AddSubmodelRef(buildingBlocksSubmodel.GetSubmodelRef());

            return buildingBlocksSubmodel;
        }
    }
}
