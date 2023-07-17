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
    /// This class allows to derive a subassembly based on a set of selected entities
    /// in a BOM submodel.
    /// </summary>
    public class SubassemblyDeriver
    {
        //
        // Public interface
        //

        public static void DeriveSubassembly(
            AasCore.Aas3_0.Environment env,
            AssetAdministrationShell aas,
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
            AasCore.Aas3_0.Environment env,
            AssetAdministrationShell aas,
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
            this.newMBomSubmodel = null;
            this.newVecSubmodel = null;
            this.newVecFileSME = null;
            this.subassemblyAas = null;
        }

        protected AasCore.Aas3_0.Environment env;
        protected AssetAdministrationShell aas;
        protected IEnumerable<Entity> entitiesToBeMadeSubassembly;
        protected string subassemblyAASName;
        protected string subassemblyEntityName;
        protected Dictionary<string, string> partNames;
        protected Submodel newVecSubmodel;
        protected AasCore.Aas3_0.File newVecFileSME;
        protected ISubmodel existingComponentBomSubmodel;
        protected ISubmodel existingBuildingBlocksBomSubmodel;
        protected ISubmodel newBomSubmodel;
        protected ISubmodel newMBomSubmodel;
        protected AssetAdministrationShell subassemblyAas;
        protected VecOptions options;
        protected LogInstance log;

        protected void DeriveSubassembly()
        {
            var allBomSubmodels = FindBomSubmodels(aas, env);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (entitiesToBeMadeSubassembly.All(RepresentsSubAssembly))
            {
                log?.Error("It seems that only subassemblies where selected. This is currently not supported. At least one basic component needs to be selected!");
                return;
            }

            var submodelsContainingSelectedEntities = FindCommonSubmodelParents(entitiesToBeMadeSubassembly);

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

            existingBuildingBlocksBomSubmodel = FindBuildingBlocksSubmodel(aas, env);
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
                    try
                    {
                        existingBuildingBlocksBomSubmodel = CreateBuildingBlocksSubmodel(options.TemplateIdSubmodel, bomSubmodel, aas, env);
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
            this.subassemblyAas = CreateAAS(this.subassemblyAASName, options.TemplateIdAas, options.TemplateIdAsset, env, AssetKind.Type);

            // FIXME probably, we should not just copy the whole existing VEC file but extract the relevant parts only into a new file
            newVecSubmodel = InitializeVecSubmodel(subassemblyAas, env, existingVecFileSME);
            newVecFileSME = newVecSubmodel.FindFirstIdShortAs<AasCore.Aas3_0.File>(VEC_FILE_ID_SHORT);

            newBomSubmodel = CreateBomSubmodel(ID_SHORT_COMPONENTS_SM, options.TemplateIdSubmodel, aas: subassemblyAas, env: env);
            newMBomSubmodel = CreateBomSubmodel(ID_SHORT_BUILDING_BLOCKS_SM, options.TemplateIdSubmodel, aas: subassemblyAas, env: env);

            // the entity representing the sub-assembly in the building blocks SM of the original AAS (the harness AAS)
            var subassemblyEntityInOriginalAAS = CreateNode(subassemblyEntityName, buildingBlocksSubmodelEntryNode, subassemblyAas.AssetInformation.GlobalAssetId);
            CreateHasPartRelationship(buildingBlocksSubmodelEntryNode, subassemblyEntityInOriginalAAS);

            foreach (var partEntityInOriginalAAS in entitiesToBeMadeSubassembly)
            {
                var partEntityInNewAAS = CreateRelatedEntitiesInNewAdminShell(partEntityInOriginalAAS);

                if (RepresentsSubAssembly(partEntityInOriginalAAS))
                {
                    // move each child of the "old" subassembly to the new one because we only keep the uppermost layer of subassemblies in the building blocks submodel
                    foreach(var child in partEntityInOriginalAAS.EnumerateChildren()) {
                        var rel = child as RelationshipElement;
                        if (IsSameAsRelationship(rel))
                        {
                            // redirect the "same as" relationship pointing to the inner part entity from the old subassembly AAS to the new subassembly AAS
                            rel.Second.Keys.First().Value = newBomSubmodel.Id;
                        } else if(IsHasPartRelationship(rel))
                        {
                            // change the "has part" relationship so that the parent is the new subassembly entity
                            rel.First.Keys.Last().Value = subassemblyEntityInOriginalAAS.IdShort;
                        }
                        subassemblyEntityInOriginalAAS.AddChild(child);
                    }

                    // delete the "old" subassembly because we only keep the uppermost layer of subassemblies in the building blocks submodel
                    var parent = partEntityInOriginalAAS.Parent as Entity;
                    var isPartOfRel = parent.FindFirstIdShortAs<RelationshipElement>("HasPart_" + partEntityInOriginalAAS.IdShort);
                    parent.Remove(partEntityInOriginalAAS);
                    parent.Remove(isPartOfRel);
                } else
                { 
                    CreateHasPartRelationship(subassemblyEntityInOriginalAAS, partEntityInOriginalAAS);
                    var sameAsRelName = partEntityInOriginalAAS.IdShort + "_SameAs_" + this.partNames[partEntityInOriginalAAS.IdShort];
                    CreateSameAsRelationship(partEntityInOriginalAAS, partEntityInNewAAS, subassemblyEntityInOriginalAAS, sameAsRelName);
                }
            }
        }        

        protected Entity CreateRelatedEntitiesInNewAdminShell(Entity partEntityInOriginalAAS)
        {
            var idShort = this.partNames[partEntityInOriginalAAS.IdShort];
            var entityInNewMBomSubmodel = CreatePartEntity(FindEntryNode(newMBomSubmodel), partEntityInOriginalAAS, idShort);

            if (RepresentsSubAssembly(partEntityInOriginalAAS))
            {
                var sameAsRelationships = GetSameAsRelationships(partEntityInOriginalAAS);
                var hasPartRelationships = GetHasPartRelationships(partEntityInOriginalAAS);

                foreach (var rel in hasPartRelationships)
                {
                    var partElementRef = rel.Second;
                    Entity subPartEntityInOriginalAAS = null;
                    if (partElementRef.Keys.First().Matches(existingComponentBomSubmodel?.ToKey()))
                    {
                        subPartEntityInOriginalAAS = FindReferencedElementInSubmodel<Entity>(existingComponentBomSubmodel, partElementRef);
                    }
                    else if (partElementRef.Keys.First().Matches(existingBuildingBlocksBomSubmodel?.ToKey()))
                    {
                        subPartEntityInOriginalAAS = FindReferencedElementInSubmodel<Entity>(existingBuildingBlocksBomSubmodel, partElementRef);
                    }

                    if (subPartEntityInOriginalAAS == null)
                    {
                        this.log?.Error("Unable to find targetEntity for hasPart relationship " + rel.IdShort);
                        continue;
                    }

                    var sameAsRelationship = sameAsRelationships.Find(sameAsRel => sameAsRel.First.Matches(rel.Second));
                    var subPartIdShort = sameAsRelationship.Second.Keys.Last().Value;
                    var subPartEntityInNewSubmodel = CreatePartEntity(FindEntryNode(newBomSubmodel), subPartEntityInOriginalAAS, subPartIdShort);
                    CopyVecRelationship(subPartEntityInOriginalAAS, subPartEntityInNewSubmodel);

                    CreateHasPartRelationship(entityInNewMBomSubmodel, subPartEntityInNewSubmodel);
                    CreateSameAsRelationship(subPartEntityInNewSubmodel.GetReference(), new Reference(ReferenceTypes.ModelReference, sameAsRelationship.Second.Keys), entityInNewMBomSubmodel, subPartEntityInNewSubmodel.IdShort + "_SameAs_" + sameAsRelationship.Second.Keys.Last().Value);
                }
            } else
            {
                var entityInNewBomSubmodel = CreatePartEntity(FindEntryNode(newBomSubmodel), partEntityInOriginalAAS, idShort);
                CopyVecRelationship(partEntityInOriginalAAS, entityInNewBomSubmodel);
                CreateHasPartRelationship(entityInNewMBomSubmodel, entityInNewBomSubmodel);
            }

            return entityInNewMBomSubmodel;
        }

        private void CopyVecRelationship(Entity partEntityInOriginalAAS, Entity partEntityInNewAAS)
        {
            var vecRelationship = GetVecRelationship(partEntityInOriginalAAS);
            if (vecRelationship != null)
            {
                var xpathToVecElement = vecRelationship.Second.Keys.Last().Value;
                var vecFileElement = GetVecFileElement(newVecSubmodel);
                CreateVecRelationship(partEntityInNewAAS, xpathToVecElement, vecFileElement);
            }
        }

        protected Entity CreatePartEntity(Entity mainEntity, Entity sourceEntity, string idShort = null)
        {
            // create the entity
            var componentEntity = CreateNode(idShort ?? sourceEntity.IdShort, mainEntity, sourceEntity.GlobalAssetId);

            // create the relationship between the main and the component entity
            CreateHasPartRelationship(mainEntity, componentEntity);

            return componentEntity;
        }

        protected Submodel InitializeVecSubmodel(AssetAdministrationShell aas, AasCore.Aas3_0.Environment env, AasCore.Aas3_0.File existingVecFileSME)
        {
            var id = GenerateIdAccordingTemplate(options.TemplateIdSubmodel);
            // create the VEC submodel
            var vecSubmodel = CreateVecSubmodel(id, existingVecFileSME);
            
            env.Submodels.Add(vecSubmodel);
            aas.AddSubmodelReference(vecSubmodel.GetReference());

            return vecSubmodel;
        }
    }
}
