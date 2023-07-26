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

        // the bom models in the existing AAS
        protected ISubmodel existingProductBom;
        protected ISubmodel existingManufacturingBom;

        // the models in the AAS to be reused (representing the subassembly)
        protected ISubmodel reusedProductBom;

        protected IEntity ReuseSubassembly()
        {
            if (!DetermineExistingSubmodels())
            {
                return null;
            }

            reusedProductBom = FindFirstBomSubmodel(env, subassemblyAasToReuse);
            reusedProductBom.SetAllParents();
            var atomicComponentEntitiesInSubAssemblyAAS = reusedProductBom.GetLeafNodes();

            // get or create the 'building blocks' submodel 
            var existingManufacturingBom = FindManufacturingBom(aas, env);
            if (existingManufacturingBom == null)
            {
               
                // no building blocks submodel seems to exist -> create a new one
                try
                {
                    existingManufacturingBom = CreateManufacturingBom(options.TemplateIdSubmodel, existingManufacturingBom, aas, env);
                }
                catch (Exception e)
                {
                    log?.Error(e.Message);
                    return null;
                }
            }

            var buildingBlocksSubmodelEntryNode = existingManufacturingBom.FindEntryNode();

            // the entity representing the sub-assembly in the BOM SM of the original AAS (the harness AAS)
            var subassemblyEntityInOriginalAAS = CreateNode(nameOfSubassemblyEntityInOriginalMbom, buildingBlocksSubmodelEntryNode, subassemblyAasToReuse.AssetInformation.GlobalAssetId, true);

            foreach (var partEntityInOriginalAAS in entitiesToBeMadeSubassembly)
            {
                CreateHasPartRelationship(subassemblyEntityInOriginalAAS, partEntityInOriginalAAS);

                var idShort = this.reusedPartNamesByOriginalPartNames[partEntityInOriginalAAS.IdShort];
                var partEntityInNewAAS = atomicComponentEntitiesInSubAssemblyAAS.First(e => e.IdShort == idShort);

                CreateSameAsRelationship(partEntityInOriginalAAS, partEntityInNewAAS, subassemblyEntityInOriginalAAS, partEntityInOriginalAAS.IdShort + "_SameAs_" + idShort);
            }

            return subassemblyEntityInOriginalAAS;
        }

        private bool DetermineExistingSubmodels()
        {
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

            return true;
        }
    }
}
