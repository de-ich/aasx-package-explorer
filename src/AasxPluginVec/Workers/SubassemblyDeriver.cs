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
    /// This class allows to derive a subassembly based on a set of selected entities.
    /// The entities need to be part of either a product BOM or a manufacturing BOM submodel - a mix of both is allowed as well.
    /// 
    /// Naming conventions:
    /// - The AAS/submodels containing the selected entities are prefixed 'original' (e.g. 'originalManufacturingBom')
    /// - The AAS/submodels containing the new subassembly to be created are prefixed 'new' (e.g. 'newManufacturingBom')
    /// - If a selected entity already represents a subassembly (that is to be incorporated in the 'new' subassembly), this AAS
    ///   and the respective submodels/elements are called 'source' (e.g. 'sourceManufacturingBom').
    /// - The building blocks of a subassembly (either an existing one or the new one to be created) are called 'part'.
    /// </summary>
    public class SubassemblyDeriver
    {
        //
        // Public interface
        //

        public static IEntity DeriveSubassembly(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            string newSubassemblyAasName,
            string nameOfSubassemblyEntityInOriginalMbom,
            Dictionary<string, string> newPartNamesByOriginalPartNames,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var deriver = new SubassemblyDeriver(env, aas, entitiesToBeMadeSubassembly, newSubassemblyAasName, 
                    nameOfSubassemblyEntityInOriginalMbom, newPartNamesByOriginalPartNames, options, log);
                return deriver.DeriveSubassembly();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"deriving subassembly");
                return null;
            }
        }

        //
        // Internal
        //

        protected SubassemblyDeriver(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            string newSubassemblyAasName,
            string nameOfSubassemblyEntityInOriginalMbom,
            Dictionary<string, string> newPartNamesByOriginalPartNames,
            VecOptions options,
            LogInstance log = null)
        {


            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.entitiesToBeMadeSubassembly = entitiesToBeMadeSubassembly ?? throw new ArgumentNullException(nameof(entitiesToBeMadeSubassembly));
            this.newSubassemblyAasName = newSubassemblyAasName ?? throw new ArgumentNullException(nameof(newSubassemblyAasName));
            this.nameOfSubassemblyEntityInOriginalMbom = nameOfSubassemblyEntityInOriginalMbom ?? throw new ArgumentNullException(nameof(nameOfSubassemblyEntityInOriginalMbom));
            this.newPartNamesByOriginalPartNames = newPartNamesByOriginalPartNames ?? throw new ArgumentNullException(nameof(newPartNamesByOriginalPartNames));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected IEnumerable<IEntity> entitiesToBeMadeSubassembly;
        protected string newSubassemblyAasName;
        protected string nameOfSubassemblyEntityInOriginalMbom;
        protected Dictionary<string, string> newPartNamesByOriginalPartNames;
        protected VecOptions options;
        protected LogInstance log;

        // the bom models and elements in the existing AAS
        protected ISubmodel existingProductBom;
        protected ISubmodel existingManufacturingBom;
        protected IEntity subassemblyInOriginalManufacturingBom;

        // the new AAS to be created (representing the subassembly)
        protected AssetAdministrationShell newSubassemblyAas;

        // the models in the new AAS to be created (representing the subassembly)
        protected Submodel newVecSubmodel;
        protected ISubmodel newProductBom;
        protected ISubmodel newManufacturingBom;


        protected IEntity DeriveSubassembly()
        {
            if (!DetermineExistingSubmodels())
            {
                return null;
            }

            if(!InitializeNewAasAndSubmodels())
            {
                return null;
            }

            // create the entity representing the subassembly in the original mbom
            subassemblyInOriginalManufacturingBom = CreateNode(nameOfSubassemblyEntityInOriginalMbom, existingManufacturingBom.FindEntryNode(), newSubassemblyAas, true);

            // for each entity to be incorporated into the subassembly create the required elements and relationships
            foreach (var entity in entitiesToBeMadeSubassembly)
            {
                if (IsPartOfWireHarnessBom(entity))
                {
                    // a single part from the product bom
                    AddSimpleComponentToSubassembly(entity);

                }
                else if (IsPartOfWireHarnessMBom(entity))
                {
                    // a subassembly entity (from the manufacturing bom) that consists of one or multiple parts
                    AddSubassemblyComponentToSubassembly(entity);
                }
            }
            
            return subassemblyInOriginalManufacturingBom;
        }

        private bool DetermineExistingSubmodels()
        {
            var allBomSubmodels = FindBomSubmodels(env, aas);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            var submodelsContainingSelectedEntities = FindCommonSubmodelParents(entitiesToBeMadeSubassembly);

            if (submodelsContainingSelectedEntities.Count == 0)
            {
                log?.Error("Unable to determine BOM submodel(s) that contain(s) the selected entities!");
                return false;
            }

            if (submodelsContainingSelectedEntities.Count > 2)
            {
                log?.Error("Entities from more than 2 BOM submodels selected. This is not supported!");
                return false;
            }

            if (submodelsContainingSelectedEntities.Count == 1)
            {
                if (!submodelsContainingSelectedEntities.First().IsProductBom())
                {
                    log?.Error("The selected entities do not seem to be part of a product BOM submodel!");
                    return false;
                }

                existingProductBom = submodelsContainingSelectedEntities.First();
            }

            if (submodelsContainingSelectedEntities.Count == 2)
            {
                existingProductBom = submodelsContainingSelectedEntities.FirstOrDefault(sm => sm.IsProductBom());
                existingManufacturingBom = submodelsContainingSelectedEntities.FirstOrDefault(sm => sm.IsManufacturingBom());

                if (existingProductBom == null || existingManufacturingBom == null)
                {
                    log?.Error("Selected entities may only be part of the product or manufacturing BOM submodels of an AAS!");
                    return false;
                }
            }

            // no mbom submodel was selected so we look for an existing one in the aas that is associated with the selected bom submodel
            existingManufacturingBom ??= FindManufacturingBom(existingProductBom, aas, env);

            // no mbom submodel was found in the aas so we create a new one
            existingManufacturingBom ??= CreateManufacturingBom(options.TemplateIdSubmodel, existingProductBom, aas, env);

            return true;
        }

        public bool InitializeNewAasAndSubmodels()
        {
            var referencedVecFileSMEs = entitiesToBeMadeSubassembly.Select(e => e.FindReferencedVecFileSME(env, aas)).Where(v => v != null);

            if (referencedVecFileSMEs.ToHashSet().Count != 1)
            {
                log?.Error("Unable to determine VEC file referenced by the BOM submodel(s)!");
                return false;
            }

            var existingVecFileSME = referencedVecFileSMEs.First();

            // the AAS for the new sub-assembly
            this.newSubassemblyAas = CreateAAS(this.newSubassemblyAasName, options.TemplateIdAas, options.TemplateIdAsset, env, AssetKind.Type);

            // FIXME probably, we should not just copy the whole existing VEC file but extract the relevant parts only into a new file
            newVecSubmodel = InitializeVecSubmodel(newSubassemblyAas, env, existingVecFileSME);

            newProductBom = CreateBomSubmodel(ID_SHORT_PRODUCT_BOM_SM, options.TemplateIdSubmodel, aas: newSubassemblyAas, env: env, supplementarySemanticId: SEM_ID_PRODUCT_BOM_SM);
            CopyVecRelationship(existingManufacturingBom.FindEntryNode(), newProductBom.FindEntryNode());

            newManufacturingBom = CreateManufacturingBom(options.TemplateIdSubmodel, newProductBom, aas: newSubassemblyAas, env: env);

            return true;
        }

        private void AddSimpleComponentToSubassembly(IEntity simpleComponentToAdd)
        {
            var partIdShortInNewAas = this.newPartNamesByOriginalPartNames[simpleComponentToAdd.IdShort];

            // create the part of the subassembly in the original mbom
            var partInOriginalMBom = CreateNode(simpleComponentToAdd, subassemblyInOriginalManufacturingBom);

            // link the entity in the original mbom to the part in the original product bom
            CreateSameAsRelationship(partInOriginalMBom, simpleComponentToAdd);

            // create the entity in the new product bom
            var partInNewBom = CreateNode(simpleComponentToAdd, newProductBom.FindEntryNode(), partIdShortInNewAas);
            CopyVecRelationship(simpleComponentToAdd, partInNewBom);

            // create the entity in the new mbom
            var subassemblyInNewMBom = CreateNode(simpleComponentToAdd, newManufacturingBom.FindEntryNode(), partIdShortInNewAas);

            // link the entity in the new mbom to the part in the new product bom
            CreateSameAsRelationship(subassemblyInNewMBom, partInNewBom);

            // link the part in the original mbom to the part in the new mbom
            CreateSameAsRelationship(partInOriginalMBom, partInNewBom);
        }

        private void AddSubassemblyComponentToSubassembly(IEntity subassemblyComponentToAdd)
        {
            var subassemblyIdShortInNewAas = this.newPartNamesByOriginalPartNames[subassemblyComponentToAdd.IdShort];

            var existingSubassemblyAas = env.AssetAdministrationShells.FirstOrDefault(aas => aas.AssetInformation.GlobalAssetId == subassemblyComponentToAdd.GlobalAssetId);

            if (existingSubassemblyAas == null)
            {
                log?.Error("Unable to determine referenced AAS for selected entity!");
                return;
            }

            var mbomSubmodelInExistingSubassemblyAas = FindManufacturingBom(existingSubassemblyAas, env);
            var bomSubmodelInExistingSubassemblyAas = FindProductBom(existingSubassemblyAas, env);

            // create the entity for the subassembly in the new mbom
            var subassemblyInNewMBom = CreateNode(subassemblyComponentToAdd, newManufacturingBom.FindEntryNode(), subassemblyIdShortInNewAas);

            var partsOfSelectedSubassembly = subassemblyComponentToAdd.GetChildEntities();
            foreach (var part in partsOfSelectedSubassembly)
            {
                var partInOriginalBom = part.GetSameAsEntity(env, existingProductBom);
                var partInBomOfExistingSubassembly = part.GetSameAsEntity(env, bomSubmodelInExistingSubassemblyAas);

                // create the entity in the original mbom
                var partInOriginalMBom = CreateNode(partInOriginalBom, subassemblyInOriginalManufacturingBom);

                // link the entity in the original mbom to the part in the original bom
                CreateSameAsRelationship(partInOriginalMBom, partInOriginalBom);

                // create the entity in the new bom
                var partInNewBom = CreateNode(partInBomOfExistingSubassembly, newProductBom.FindEntryNode());
                CopyVecRelationship(partInOriginalBom, partInNewBom);

                // create the entity in the new mbom
                var partInNewMBom = CreateNode(partInNewBom, subassemblyInNewMBom);

                // link the part in the original mbom to the part in the new bom
                CreateSameAsRelationship(partInOriginalMBom, partInNewBom);

                // link the part in the new mbom to the part in the new bom
                CreateSameAsRelationship(partInNewMBom, partInNewBom);

                // link the part in the new mbom to the part in the source bom
                CreateSameAsRelationship(partInNewMBom, partInBomOfExistingSubassembly);
            }

            // delete the old subassembly that is now incorporated in the new subassembly
            var hasPartRelationshipToSelectedEntity = subassemblyComponentToAdd.GetHasPartRelationshipFromParent();
            existingManufacturingBom.FindEntryNode().Remove(subassemblyComponentToAdd);
            existingManufacturingBom.FindEntryNode().Remove(hasPartRelationshipToSelectedEntity);
        }
        
        private void CopyVecRelationship(IEntity partEntityInOriginalAAS, IEntity partEntityInNewAAS)
        {
            var vecRelationship = partEntityInOriginalAAS.GetVecRelationship(env, aas);
            if (vecRelationship != null)
            {
                var xpathToVecElement = vecRelationship.First.Keys.Last().Value;
                var vecFileElement = newVecSubmodel.GetVecFileElement();
                CreateVecRelationship(partEntityInNewAAS, xpathToVecElement, vecFileElement);
            }
        }

        protected Submodel InitializeVecSubmodel(AssetAdministrationShell aas, AasCore.Aas3_0.Environment env, AasCore.Aas3_0.File existingVecFileSME)
        {
            // create the VEC submodel
            return CreateVecSubmodel(existingVecFileSME, options.TemplateIdSubmodel, aas, env);
        }

        protected bool IsPartOfWireHarnessBom(IEntity entity)
        {
            return entity.GetParentSubmodel() == this.existingProductBom;
        }

        protected bool IsPartOfWireHarnessMBom(IEntity entity)
        {
            return entity.GetParentSubmodel() == this.existingManufacturingBom;
        }
    }
}
