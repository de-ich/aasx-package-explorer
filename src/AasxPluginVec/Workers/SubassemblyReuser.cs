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
    /// This class allows to reuse an existing subassembly for a set of selected entities
    /// in a product BOM submodel.
    /// 
    /// Naming conventions:
    /// - The AAS/submodels containing the selected entities are prefixed 'original' (e.g. 'originalManufacturingBom')
    /// - The AAS/submodels containing the subassembly to be reused are prefixed 'reused' (e.g. 'reusedManufacturingBom')
    /// - The building blocks of a subassembly (either an existing one or the new one to be created) are called 'part'.
    /// </summary>
    public class SubassemblyReuser
    {
        //
        // Public interface
        //

        public static IEntity ReuseSubassembly(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            IAssetAdministrationShell subassemblyAasToReuse,
            string nameOfSubassemblyEntityInOriginalMbom,
            Dictionary<string, string> reusedPartNamesByOriginalPartNames,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var reuser = new SubassemblyReuser(env, aas, entitiesToBeMadeSubassembly, subassemblyAasToReuse, nameOfSubassemblyEntityInOriginalMbom, reusedPartNamesByOriginalPartNames, options, log);
                return reuser.ReuseSubassembly();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"reusing subassembly");
                return null;
            }
        }

        //
        // Internal
        //

        protected SubassemblyReuser(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            IAssetAdministrationShell subassemblyAasToReuse,
            string nameOfSubassemblyEntityInOriginalMbom,
            Dictionary<string, string> reusedPartNamesByOriginalPartNames,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.entitiesToBeMadeSubassembly = entitiesToBeMadeSubassembly ?? throw new ArgumentNullException(nameof(entitiesToBeMadeSubassembly));
            this.subassemblyAasToReuse = subassemblyAasToReuse ?? throw new ArgumentNullException(nameof(subassemblyAasToReuse));
            this.nameOfSubassemblyEntityInOriginalMbom = nameOfSubassemblyEntityInOriginalMbom ?? throw new ArgumentNullException(nameof(nameOfSubassemblyEntityInOriginalMbom));
            this.reusedPartNamesByOriginalPartNames = reusedPartNamesByOriginalPartNames ?? throw new ArgumentNullException(nameof(reusedPartNamesByOriginalPartNames));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected IEnumerable<Entity> entitiesToBeMadeSubassembly;
        protected IAssetAdministrationShell subassemblyAasToReuse;
        protected string nameOfSubassemblyEntityInOriginalMbom;
        protected Dictionary<string, string> reusedPartNamesByOriginalPartNames;
        protected VecOptions options;
        protected LogInstance log;

        // the bom models and elements in the existing AAS
        protected ISubmodel existingProductBom;
        protected ISubmodel existingManufacturingBom;
        protected IEntity subassemblyInOriginalManufacturingBom;

        // the models in the AAS to be reused (representing the subassembly)
        protected ISubmodel reusedProductBom;

        protected IEntity ReuseSubassembly()
        {
            if (!DetermineExistingSubmodels())
            {
                return null;
            }

            // create the entity representing the subassembly in the orginal mbom
            subassemblyInOriginalManufacturingBom = CreateNode(nameOfSubassemblyEntityInOriginalMbom, existingManufacturingBom.FindEntryNode(), subassemblyAasToReuse, true);

            var partsInReusedProductBom = reusedProductBom.FindEntryNode()?.GetChildEntities();
            foreach (var entity in entitiesToBeMadeSubassembly)
            {
                // create the part of the subassembly in the original mbom
                var partInOriginalMBom = CreateNode(entity, subassemblyInOriginalManufacturingBom);

                // link the entity in the original mbom to the part in the original product bom
                CreateSameAsRelationship(partInOriginalMBom, entity);

                // determine the entity in the reused product bom that represents the selected entity in the original product bom
                var idShort = this.reusedPartNamesByOriginalPartNames[entity.IdShort];
                var partInReusedProductBom = partsInReusedProductBom?.First(e => e.IdShort == idShort);

                // link the entity in the original mbom to eh part in the reused product bom
                CreateSameAsRelationship(partInOriginalMBom, partInReusedProductBom);
            }

            return subassemblyInOriginalManufacturingBom;
        }

        private bool DetermineExistingSubmodels()
        {
            var allBomSubmodels = FindBomSubmodels(env, aas);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            existingProductBom = FindCommonSubmodelParent(entitiesToBeMadeSubassembly);
            if (existingProductBom == null)
            {
                log?.Error("Unable to determine the single common BOM that contains the selected entities!");
                return false;
            }

            if (!existingProductBom.IsProductBom())
            {
                log?.Error("Only entities from a product BOM may be selected!");
                return false;
            }

            reusedProductBom = FindFirstBomSubmodel(env, subassemblyAasToReuse);
            reusedProductBom.SetAllParents();

            // look for an existing mbom submodel in the existing aas
            existingManufacturingBom = FindManufacturingBom(aas, env);

            // no mbom submodel was found in the aas so we create a new one
            existingManufacturingBom ??= CreateManufacturingBom(options.TemplateIdSubmodel, existingProductBom, aas, env);

            return true;
        }
    }
}
