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
            IEnumerable<Entity> selectedConfigurations,
            string orderNumber,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var creator = new OrderCreator(env, aas, selectedConfigurations, orderNumber, options, log);
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
            IEnumerable<Entity> selectedConfigurations,
            string orderNumber,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.selectedConfigurations = selectedConfigurations ?? throw new ArgumentNullException(nameof(selectedConfigurations));
            this.orderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected IEnumerable<Entity> selectedConfigurations;
        protected string orderNumber;
        protected VecOptions options;
        protected LogInstance log;

        // the bom models and elements in the existing AAS
        protected ISubmodel existingConfigurationBom;
        protected IEnumerable<IEntity> subassembliesAssociatedWithSelectedConfigurations;

        // the aas to be created (representing the specific order)
        protected AssetAdministrationShell orderAas;

        // te models in the new aas to be created (representing the order)
        protected Submodel orderConfigurationBom;
        protected Submodel orderManufacturingBom;

        

        protected IAssetAdministrationShell CreateOrder()
        {
            if(!DetermineExistingSubmodels())
            {
                return null;
            }

            // create the new aas representing the order
            var orderAasIdShort = aas.IdShort + "_Order_" + orderNumber;
            orderAas = CreateAAS(orderAasIdShort, options.TemplateIdAas, options.TemplateIdAsset, env);
            orderAas.DerivedFrom = aas.GetReference();

            // create the configuration bom in the new aas
            orderConfigurationBom = CreateBomSubmodel(ID_SHORT_CONFIGURATION_BOM_SM, options.TemplateIdSubmodel, aas: orderAas, env: env);

            foreach(var configuration in selectedConfigurations)
            {
                // create the configuration in the order configuration bom
                var configurationInOrderConfigurationBom = CreateNode(configuration, orderConfigurationBom.FindEntryNode());

                // link the entity repesenting the configuration in the order configuration bom to the original configuration bom
                CreateSameAsRelationship(configurationInOrderConfigurationBom, configuration);
            }

            // create the manufacturing bom in the new aas
            orderManufacturingBom = CreateBomSubmodel(ID_SHORT_MANUFACTURING_BOM_SM, options.TemplateIdSubmodel, aas: orderAas, env: env);
            var orderBuildingBlocksEntryNode = orderManufacturingBom.FindEntryNode();

            foreach(var associatedSubassembly in subassembliesAssociatedWithSelectedConfigurations)
            {
                // create the entity in the new manufacturing bom
                var subassemblyInOrderManufacturingBom = CreateNode(associatedSubassembly, orderManufacturingBom.FindEntryNode());
                subassemblyInOrderManufacturingBom.GlobalAssetId = null; // reset the global asset id because we do not yet now the specific subassembly instance used in product
                subassemblyInOrderManufacturingBom.EntityType = EntityType.SelfManagedEntity; // set to 'self-managed' although we do not yet now the specific instance that will be used in production

                // link the entity in the new manufacturing bom to the subassembly in the original manufacturing bom
                CreateSameAsRelationship(subassemblyInOrderManufacturingBom, associatedSubassembly);
            }

            return orderAas;
        }

        private bool DetermineExistingSubmodels()
        {
            var allBomSubmodels = FindBomSubmodels(env, aas);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (!selectedConfigurations.All(HasAssociatedSubassemblies))
            {
                log?.Error("It seems that a module was selected that has no associated subassemblies required for production!");
                return false;
            }

            existingConfigurationBom = FindCommonSubmodelParent(selectedConfigurations);

            if (existingConfigurationBom == null)
            {
                log?.Error("Unable to determine single common configuration BOM that contains the selected configurations!");
                return false;
            }

            subassembliesAssociatedWithSelectedConfigurations = selectedConfigurations.SelectMany(m => FindAssociatedSubassemblies(m, env)).ToHashSet();

            if (subassembliesAssociatedWithSelectedConfigurations.Any(s => s == null))
            {
                log?.Error("At least one subassembly associated with a selected module could not be determined!");
                return false;
            }

            return true;
        }
    }
}
