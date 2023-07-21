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
using AasCore.Aas3_0;
using Extensions;
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

        public static IAssetAdministrationShell CreateOrder(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> selectedModules,
            string orderNumber,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var creator = new OrderCreator(env, aas, selectedModules, orderNumber, options, log);
                return creator.CreateOrder();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"creating order");
                return null;
            }
        }

        //
        // Internal
        //

        protected OrderCreator(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
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

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected IEnumerable<Entity> selectedModules;
        protected string orderNumber;
        protected AssetAdministrationShell orderAas;
        protected Submodel orderedModulesSubmodel;
        protected Submodel orderBuildingBlocksSubmodel;
        protected VecOptions options;
        protected LogInstance log;

        protected IAssetAdministrationShell CreateOrder()
        {
            var allBomSubmodels = FindBomSubmodels(aas, env);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (!selectedModules.All(HasAssociatedSubassemblies))
            {
                log?.Error("It seems that a module was selected that has no associated subassemblies required for production!");
                return null;
            }

            var submodelContainingSelectedModules = FindCommonSubmodelParent(selectedModules);

            if (submodelContainingSelectedModules == null)
            {
                log?.Error("Unable to determine single common BOM submodel that contains the selected modules!");
                return null;
            }

            var associatedSubassemblies = selectedModules.SelectMany(m => FindAssociatedSubassemblies(m, env));

            if (associatedSubassemblies.Any(s => s == null))
            {
                log?.Error("At least one subassembly associated with a selected module could not be determined!");
                return null;
            }

            var orderAasIdShort = aas.IdShort + "_Order_" + orderNumber;
            orderAas = CreateAAS(orderAasIdShort, options.TemplateIdAas, options.TemplateIdAsset, env);
            orderAas.DerivedFrom = aas.GetReference();

            orderedModulesSubmodel = CreateBomSubmodel(ID_SHORT_ORDERED_MODULES_SM, options.TemplateIdSubmodel, aas: orderAas, env: env);
            var orderedModuleEntryNode = orderedModulesSubmodel.FindEntryNode();

            foreach(var module in selectedModules)
            {
                CreateHasPartRelationship(orderedModuleEntryNode, module);
            }

            orderBuildingBlocksSubmodel = CreateBomSubmodel(ID_SHORT_BUILDING_BLOCKS_SM, options.TemplateIdSubmodel, aas: orderAas, env: env);
            var orderBuildingBlocksEntryNode = orderBuildingBlocksSubmodel.FindEntryNode();

            foreach(var associatedSubassembly in associatedSubassemblies)
            {
                var buildingBlockEntity = CreateEntity(associatedSubassembly.IdShort, orderBuildingBlocksEntryNode, semanticId: associatedSubassembly.SemanticId);
                buildingBlockEntity.EntityType = EntityType.SelfManagedEntity; // set to 'self-managed' although we do not yet now the specific instance that will be used in production

                CreateHasPartRelationship(orderBuildingBlocksEntryNode, buildingBlockEntity);
                CreateSameAsRelationship(buildingBlockEntity, associatedSubassembly, buildingBlockEntity);
            }

            return orderAas;
        }
    }
}
