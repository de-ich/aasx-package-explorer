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
    /// This class allows to derive a subassembly based on a set of selected entities
    /// in a BOM submodel.
    /// </summary>
    public class SubassemblyDeriver
    {
        //
        // Public interface
        //

        public static void DeriveSubassembly(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> entities,
            string subassemblyAASName,
            string subassemblyEntityName,
            Dictionary<string, string> partNames,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var deriver = new SubassemblyDeriver(env, aas, entities, subassemblyAASName, subassemblyEntityName, partNames, options, log);
                deriver.DeriveSubassembly();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"deriving subassembly");
            }
        }

        //
        // Internal
        //

        protected SubassemblyDeriver(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> entities,
            string subassemblyAASName,
            string subassemblyEntityName,
            Dictionary<string, string> partNames,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.entitiesToBeMadeSubassembly = entities ?? throw new ArgumentNullException(nameof(entities));
            this.subassemblyAASName = subassemblyAASName ?? throw new ArgumentNullException(nameof(subassemblyAASName));
            this.subassemblyEntityName = subassemblyEntityName ?? throw new ArgumentNullException(nameof(subassemblyEntityName));
            this.partNames = partNames ?? throw new ArgumentNullException(nameof(partNames));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.existingComponentBomSubmodel = null;
            this.existingBuildingBlocksBomSubmodel = null;
            this.newBomSubmodel = null;
            this.newVecSubmodel = null;
            this.newVecFileSME = null;
            this.subassemblyAas = null;
        }

        protected AdministrationShellEnv env;
        protected AdministrationShell aas;
        protected IEnumerable<Entity> entitiesToBeMadeSubassembly;
        protected string subassemblyAASName;
        protected string subassemblyEntityName;
        protected Dictionary<string, string> partNames;
        protected Submodel newVecSubmodel;
        protected AdminShellV20.File newVecFileSME;
        protected Submodel existingComponentBomSubmodel;
        protected Submodel existingBuildingBlocksBomSubmodel;
        protected Submodel newBomSubmodel;
        protected AdministrationShell subassemblyAas;
        protected VecOptions options;
        protected LogInstance log;

        protected void DeriveSubassembly()
        {
            var allBomSubmodels = FindBomSubmodels(aas, env);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            var submodelsContainingSelectedEntities = entitiesToBeMadeSubassembly.Select(e => e.FindParentFirstIdentifiable() as Submodel).ToHashSet();

            if (submodelsContainingSelectedEntities.Count == 0)
            {
                log?.Error("Unable to determine BOM submodel(s) that contain(s) the selected entities!");
                return;
            }

            if (submodelsContainingSelectedEntities.Count > 2)
            {
                log?.Error("Entities from more than 2 BOM submodels selected. This is not supported!");
                return;
            }

            var referencedVecFileSMEs = submodelsContainingSelectedEntities.Select(sm => FindEntryNode(sm)).Select(n => FindReferencedVecFileSME(n, env)).Where(v => v != null);

            if (referencedVecFileSMEs.Count() != submodelsContainingSelectedEntities.Count)
            {
                log?.Error("Not every BOM submodel containing one of the selected entities references a VEC file!");
                return;
            }

            if (referencedVecFileSMEs.ToHashSet().Count != 1)
            {
                log?.Error("Unable to determine VEC file referenced by the BOM submodel(s)!");
                return;
            }

            var existingVecFileSME = referencedVecFileSMEs.First();

            existingBuildingBlocksBomSubmodel = allBomSubmodels.FirstOrDefault(IsBuildingBlocksSubmodel);
            if (existingBuildingBlocksBomSubmodel == null)
            {
                if (submodelsContainingSelectedEntities.Count == 2)
                {
                    log?.Error("Found entities from 2 selected BOM submodels but none of these is a building blocks submodel!");
                    return;
                }
                else
                {
                    // no building blocks submodel seems to exist -> create a new one
                    var bomSubmodel = submodelsContainingSelectedEntities.First();
                    var id = GenerateIdAccordingTemplate(options.TemplateIdSubmodel);
                    try
                    {
                        existingBuildingBlocksBomSubmodel = CreateBuildingBlocksSubmodel(id, bomSubmodel, aas, env);
                    } catch (Exception e)
                    {
                        log?.Error(e.Message);
                        return;
                    }
                }
            }

            var buildingBlocksSubmodelEntryNode = FindEntryNode(existingBuildingBlocksBomSubmodel);
            existingComponentBomSubmodel = submodelsContainingSelectedEntities.First(sm => sm != existingBuildingBlocksBomSubmodel);

            // the AAS for the new sub-assembly
            var newSubassemblyAAS = CreateSubassemblyAas();
            newBomSubmodel = InitializeBomSubmodel(newSubassemblyAAS, env);   
            var mainEntityInNewBomSubmodel = CreateEntryNode(newBomSubmodel, this.aas.assetRef);
            // FIXME probably, we should not just copy the whole existing VEC file but extract the relevant parts only into a new file
            newVecSubmodel = InitializeVecSubmodel(newSubassemblyAAS, env, existingVecFileSME);
            newVecFileSME = newVecSubmodel.FindSubmodelElementWrapper(VEC_FILE_ID_SHORT)?.submodelElement as AdminShellV20.File;

            // the entity representing the sub-assembly in the building blocks SM of the original AAS (the harness AAS)
            var subassemblyEntityInOriginalAAS = CreateNode(subassemblyEntityName, buildingBlocksSubmodelEntryNode, newSubassemblyAAS.assetRef);
            CreateHasPartRelationship(buildingBlocksSubmodelEntryNode, subassemblyEntityInOriginalAAS);

            foreach (var partEntityInOriginalAAS in entitiesToBeMadeSubassembly)
            {
                CreateHasPartRelationship(subassemblyEntityInOriginalAAS, partEntityInOriginalAAS);

                var idShort = this.partNames[partEntityInOriginalAAS.idShort];
                var partEntityInNewAAS = CreatePartEntitiesInNewSubmodelRecursively(partEntityInOriginalAAS, mainEntityInNewBomSubmodel, mainEntityInNewBomSubmodel, idShort);

                CreateSameAsRelationship(partEntityInOriginalAAS, partEntityInNewAAS, subassemblyEntityInOriginalAAS, partEntityInOriginalAAS.idShort + "_SameAs_" + idShort);
            }
        }        

        protected Entity CreatePartEntitiesInNewSubmodelRecursively(Entity partEntityInOriginalAAS, Entity subassemblyEntityInNewAAS, Entity parent, string idShort = null)
        {
            var partEntityInNewAAS = CreatePartEntity(parent, partEntityInOriginalAAS, idShort);

            var vecRelationship = GetVecRelationship(partEntityInOriginalAAS);
            if (vecRelationship != null)
            {
                var xpathToVecElement = vecRelationship.second.Keys.Last().value;
                CreateVecRelationship(partEntityInNewAAS, xpathToVecElement, newVecFileSME);

            }

            if (RepresentsSubAssembly(partEntityInOriginalAAS))
            {
                var sameAsRelationships = GetSameAsRelationships(partEntityInOriginalAAS);
                var hasPartRelationships = GetHasPartRelationships(partEntityInOriginalAAS);

                foreach (var rel in hasPartRelationships)
                {
                    Reference partElementRef = rel.second;
                    Entity subPartEntityInOriginalAAS = null;
                    if (partElementRef.Keys.First().Matches(existingComponentBomSubmodel.ToKey()))
                    {
                        subPartEntityInOriginalAAS = FindReferencedElementInSubmodel<Entity>(existingComponentBomSubmodel, partElementRef);
                    }
                    else if (partElementRef.Keys.First().Matches(existingBuildingBlocksBomSubmodel.ToKey()))
                    {
                        subPartEntityInOriginalAAS = FindReferencedElementInSubmodel<Entity>(existingBuildingBlocksBomSubmodel, partElementRef);
                    }

                    if (subPartEntityInOriginalAAS == null)
                    {
                        this.log?.Error("Unable to find targetEntity for hasPart relationship " + rel.idShort);
                        continue;
                    }

                    var sameAsRelationship = sameAsRelationships.Find(sameAsRel => sameAsRel.first.Matches(rel.second));
                    var subPartIdShort = sameAsRelationship.second.Keys.Last().value;
                    var subPartEntityInNewSubmodel = CreatePartEntitiesInNewSubmodelRecursively(subPartEntityInOriginalAAS, subassemblyEntityInNewAAS, parent, subPartIdShort);

                    CreateHasPartRelationship(partEntityInNewAAS, subPartEntityInNewSubmodel);
                    CreateSameAsRelationship(GetReference(subPartEntityInNewSubmodel), sameAsRelationship.second, partEntityInNewAAS, subPartEntityInNewSubmodel.idShort + "_SameAs_" + sameAsRelationship.second.Keys.Last().value);
                }
            }

            return partEntityInNewAAS;
        }

        protected AdministrationShell CreateSubassemblyAas()
        {
            var aas = new AdministrationShell();
            aas.idShort = this.subassemblyAASName;
            aas.identification = new Identification(new Key("AssetAdministrationShell", false, "IRI", GenerateIdAccordingTemplate(options.TemplateIdAas)));

            var asset = new Asset();
            asset.idShort = this.subassemblyAASName + "_Asset";
            asset.identification = new Identification(new Key("Asset", false, "IRI", GenerateIdAccordingTemplate(options.TemplateIdAsset)));
            aas.assetRef = asset.GetAssetReference();

            this.subassemblyAas = aas;
            this.env.AdministrationShells.Add(aas);
            this.env.Assets.Add(asset);

            return aas;
        }

        protected Submodel InitializeBomSubmodel(AdministrationShell aas, AdministrationShellEnv env)
        {
            var id = GenerateIdAccordingTemplate(options.TemplateIdSubmodel);
            var idShort = "LS_BOM";

            // create the BOM submodel
            var bomSubmodel = CreateBomSubmodel(idShort, id);

            env.Submodels.Add(bomSubmodel);
            aas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());

            return bomSubmodel;
        }

        protected Entity CreatePartEntity(Entity mainEntity, Entity sourceEntity, string idShort = null)
        {
            // create the entity
            AssetRef assetRef = sourceEntity.assetRef;
            var componentEntity = CreateNode(idShort ?? sourceEntity.idShort, mainEntity, assetRef);

            // create the relationship between the main and the component entity
            CreateHasPartRelationship(mainEntity, componentEntity);

            return componentEntity;
        }

       protected Submodel InitializeVecSubmodel(AdministrationShell aas, AdministrationShellEnv env, AdminShellV20.File existingVecFileSME)
        {
            var id = GenerateIdAccordingTemplate(options.TemplateIdSubmodel);
            // create the VEC submodel
            var vecSubmodel = CreateVecSubmodel(id, existingVecFileSME);
            
            env.Submodels.Add(vecSubmodel);
            aas.AddSubmodelRef(vecSubmodel.GetSubmodelRef());

            return vecSubmodel;
        }
    }
}
