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
            this.existingBomSubmodel = null;
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
        protected Submodel existingBomSubmodel;
        protected Submodel newBomSubmodel;
        protected AdministrationShell subassemblyAas;
        protected VecOptions options;
        protected LogInstance log;

        protected void DeriveSubassembly()
        {
            // parent is the (common) container of all entities that are to be converted into a sub-assembly
            var parent = entitiesToBeMadeSubassembly.First().parent as Entity;
            existingBomSubmodel = parent?.FindParentFirstIdentifiable() as Submodel;
            if (existingBomSubmodel == null)
            {
                log?.Error("Unable to determine BOM submodel that contains the selected entities!");
                return;
            }

            var entryNode = FindEntryNode(existingBomSubmodel);
            var existingVecFileSME = FindReferencedVecFileSME(entryNode);
            if (existingVecFileSME == null)
            {
                log?.Error("Unable to determine VEC file referenced by the BOM submodel!");
                return;
            }

            // the AAS for the new sub-assembly
            var newSubassemblyAAS = CreateSubassemblyAas();
            newBomSubmodel = InitializeBomSubmodel(newSubassemblyAAS, env);   
            var mainEntityInNewBomSubmodel = CreateMainEntity(newBomSubmodel);
            // FIXME probably, we should not just copy the whole existing VEC file but extract the relevant parts only into a new file
            newVecSubmodel = InitializeVecSubmodel(newSubassemblyAAS, env, existingVecFileSME);
            newVecFileSME = newVecSubmodel.FindSubmodelElementWrapper(VEC_FILE_ID_SHORT)?.submodelElement as AdminShellV20.File;

            // the entity representing the sub-assembly in the BOM SM of the original AAS (the harness AAS)
            var subassemblyEntityInOriginalAAS = CreateNode(subassemblyEntityName, parent, newSubassemblyAAS.assetRef);
            CreateHasPartRelationship(parent, subassemblyEntityInOriginalAAS);

            foreach (var partEntityInOriginalAAS in entitiesToBeMadeSubassembly)
            {
                CreateHasPartRelationship(subassemblyEntityInOriginalAAS, partEntityInOriginalAAS);

                var idShort = this.partNames[partEntityInOriginalAAS.idShort];
                var partEntityInNewAAS = CreatePartEntitiesInNewSubmodelRecursively(partEntityInOriginalAAS, mainEntityInNewBomSubmodel, mainEntityInNewBomSubmodel, idShort);

                CreateSameAsRelationship(partEntityInOriginalAAS, partEntityInNewAAS, subassemblyEntityInOriginalAAS, partEntityInOriginalAAS.idShort + "_SameAs_" + idShort);
            }
        }

        protected AdminShellV20.File FindReferencedVecFileSME(Entity entityWithVecRelationship)
        {
            var entryNodeVecRelationship = GetVecRelationship(entityWithVecRelationship);
            var fragmentReferenceKeys = entryNodeVecRelationship?.second?.Keys;
            var keysToVecFile = fragmentReferenceKeys?.Take(fragmentReferenceKeys.ToList().Count - 1);
            var referenceToVecFile = Reference.CreateNew(keysToVecFile?.ToList() ?? new List<Key>());
            return env.FindReferableByReference(referenceToVecFile) as AdminShellV20.File;
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
                    Entity subPartEntityInOriginalAAS = existingBomSubmodel.FindDeep<Entity>(e => GetReference(e).Matches(rel.second)).FirstOrDefault();
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

        protected bool RepresentsSubAssembly(Entity entity)
        {
            var sameAsRelationships = GetSameAsRelationships(entity);
            var hasSameAsRelationshipToOtherEntityInDifferentBOM = sameAsRelationships.Any(r =>
            {
                return r.first.Keys.Last().type == "Entity" && r.second.Keys.Last().type == "Entity" &&
                    r.first.Keys.First().type == "Submodel" && r.second.Keys.First().type == "Submodel" &&
                    r.first.Keys.First().Matches(entity.FindParentFirstIdentifiable().ToKey()) && !r.second.Keys.First().Matches(entity.FindParentFirstIdentifiable().ToKey());
            });

            if (!hasSameAsRelationshipToOtherEntityInDifferentBOM)
            {
                return false;
            }

            var hasPartRelationships = GetHasPartRelationships(entity);
            return hasPartRelationships.Count > 0;
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

        protected Entity CreateMainEntity(Submodel bomSubmodel)
        {
            // create the main entity
            return CreateEntryNode(bomSubmodel, this.aas.assetRef);
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
