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

namespace AasxPluginVec
{
    /// <summary>
    /// This class allows importing a VEC file into an existing submodel.
    /// Additionally, it allows to generate a BOM submodel based on the contents
    /// in the VEC file.
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
            string subassemblyName,
            Dictionary<string, string> partNames,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var deriver = new SubassemblyDeriver(env, aas, entities, subassemblyName, partNames, options, log);
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
            string subassemblyName,
            Dictionary<string, string> partNames,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
            this.subassemblyName = subassemblyName ?? throw new ArgumentNullException(nameof(subassemblyName));
            this.partNames = partNames ?? throw new ArgumentNullException(nameof(partNames));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.entitiesInNewSubAssemblyByOriginalEntities = new Dictionary<Entity, Entity>();
            this.vecSubmodel = null;
            this.bomSubmodel = null;
            this.subassemblyAas = null;
        }

        protected AdministrationShellEnv env;
        protected AdministrationShell aas;
        protected IEnumerable<Entity> entities;
        protected string subassemblyName;
        protected Dictionary<string, string> partNames;
        protected Submodel vecSubmodel;
        protected Submodel bomSubmodel;
        protected AdministrationShell subassemblyAas;
        protected VecOptions options;
        protected LogInstance log;
        protected Dictionary<Entity, Entity> entitiesInNewSubAssemblyByOriginalEntities;




        protected void DeriveSubassembly()
        {
            // parent is the (common) container of all entities that are to be converted into a sub-assembly
            var parent = entities.First().parent as Entity;
            if (parent == null)
            {
                return;
            }

            // the AAS for the new sub-assembly
            var aas = CreateSubassemblyAas();

            // the entity representing the sub-assembly in the BOM SM of the original AAS (the harness AAS)
            var subAssemblyEntityIdShort = "Subassembly_" + string.Join("_", entities.Select(e => e.idShort));
            var subassemblyEntity = CreateNode(subAssemblyEntityIdShort, parent, aas.assetRef);
            CreateHasPartRelationship(parent, subassemblyEntity);

            foreach (var entity in entities)
            {
                var entityInNewSubAssembly = this.entitiesInNewSubAssemblyByOriginalEntities[entity];

                CreateHasPartRelationship(subassemblyEntity, entity);
                CreateSameAsRelationship(entity, entityInNewSubAssembly, subassemblyEntity);

                if (RepresentsSubAssembly(entity))
                {
                    log?.Error(entity.ToIdShortString() + "is sub assembly");
                    // TODO add 'same as' relationship between 
                }
            }      
            
            
        }

        protected bool RepresentsSubAssembly(Entity entity)
        {
            var sameAsRels = entity.EnumerateChildren().
                Where(c => c.submodelElement is RelationshipElement).
                Select(c => c.submodelElement as RelationshipElement).
                Where(r => r.semanticId.Matches(new Key("ConceptDescription", false, "IRI", SEM_ID_SAME_AS)));

            return sameAsRels.Any(r =>
            {
                var firstKeyOfFirst = r.first.Keys[0];
                return r.first.Keys.Last().type == "Entity" && r.second.Keys.Last().type == "Entity" &&
                    r.first.Keys.First().type == "Submodel" && r.second.Keys.Last().type == "Submodel" &&
                    r.first.Keys.First().Matches(entity.FindParentFirstIdentifiable().ToKey()) && !r.first.Keys.First().Matches(entity.FindParentFirstIdentifiable().ToKey());
            });
        }

        protected AdministrationShell CreateSubassemblyAas()
        {
            var aas = new AdministrationShell();
            aas.idShort = this.subassemblyName;
            aas.identification = new Identification(new Key("AssetAdministrationShell", false, "IRI", AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdAas)));

            var asset = new Asset();
            asset.idShort = this.subassemblyName + "_Asset";
            asset.identification = new Identification(new Key("Asset", false, "IRI", AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdAsset)));
            aas.assetRef = asset.GetAssetReference();

            var bomSubmodel = InitializeBomSubmodel(aas, env);
            var mainEntity = CreateMainEntity(bomSubmodel);
            foreach (var entity in entities) {
                var subAssemblyEntity = CreatePartEntity(bomSubmodel, mainEntity, entity);
                this.entitiesInNewSubAssemblyByOriginalEntities[entity] = subAssemblyEntity;
            }

            this.subassemblyAas = aas;
            this.env.AdministrationShells.Add(aas);
            this.env.Assets.Add(asset);

            return aas;
        }

        private Submodel InitializeBomSubmodel(AdministrationShell aas, AdministrationShellEnv env)
        {
            var id = AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdSubmodel);
            var idShort = "LS_BOM";

            // create the BOM submodel
            var bomSubmodel = CreateBomSubmodel(idShort, id);

            env.Submodels.Add(bomSubmodel);
            aas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());

            return bomSubmodel;
        }

        private Entity CreateMainEntity(Submodel bomSubmodel)
        {
            // create the main entity
            return CreateEntryNode(bomSubmodel, this.aas.assetRef);
        }

        private Entity CreatePartEntity(Submodel bomSubmodel, Entity mainEntity, Entity sourceEntity)
        {
            // create the entity
            var idShort = this.partNames[sourceEntity.idShort];
            AssetRef assetRef = sourceEntity.assetRef;
            var componentEntity = CreateNode(idShort, mainEntity, assetRef);

            // create the relationship between the main and the component entity
            CreateHasPartRelationship(mainEntity, componentEntity);

            return componentEntity;
        }

        /*protected void CreateVecSubmodel()
        {
            // add the file to the package
            var localFilePath = "/aasx/files/" + Path.GetFileName(fn);
            packageEnv.AddSupplementaryFileToStore(fn, localFilePath, false);

            // create the VEC submodel
            vecSubmodel = new Submodel();
            vecSubmodel.SetIdentification(Identification.IRI, AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdConceptDescription), "VEC");
            vecSubmodel.semanticId = new SemanticId(new Key("Submodel", true, "IRI", "http://arena2036.de/vws4ls/vec/VecFileReference/1/0"));
            env.Submodels.Add(vecSubmodel);
            aas.AddSubmodelRef(vecSubmodel.GetSubmodelRef());

            // create the VEC file submodel element
            var file = new AdminShell.File();
            file.idShort = "VEC";
            file.mimeType = "text/xml";
            file.value = localFilePath;
            vecSubmodel.AddChild(new SubmodelElementWrapper(file));
            this.vecFileSubmodelElement = file;
        }

        protected void CreateBomSubmodel(XElement harnessDescription)
        {
            

            var bomSubmodel = InitializeBomSubmodel();
            var mainEntity = CreateMainEntity(bomSubmodel, harnessDescription);
            CreateComponentEntities(bomSubmodel, mainEntity, harnessDescription);
            CreateModuleEntities(bomSubmodel, mainEntity, harnessDescription);
        }

        private Submodel InitializeBomSubmodel()
        {
            // create the BOM submodel
            var bomSubmodel = new Submodel();
            bomSubmodels.Add(bomSubmodel);

            var id = AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdConceptDescription);

            // 'GenerateIdAccordingTemplate' does not seem to generate unique ids when called multiple times
            // in too short of a time span so we ensure uniqueness manually
            id = id.Substring(0, id.Length - 1) + bomSubmodels.Count();

            var idShort = "LS_BOM_" + bomSubmodels.Count().ToString().PadLeft(2, '0');
            bomSubmodel.SetIdentification(Identification.IRI, id, idShort);
            bomSubmodel.semanticId = new SemanticId(new Key("Submodel", false, "IRI", "https://admin-shell.io/idta/HierarchicalStructures/1/0/Submodel"));

            var archeTypeProperty = new Property();
            archeTypeProperty.semanticId = new SemanticId(new Key("Property", false, "IRI", "https://admin-shell.io/idta/HierarchicalStructures/ArcheType/1/0"));
            archeTypeProperty.idShort = "ArcheType";
            archeTypeProperty.value = "Full";
            bomSubmodel.AddChild(new SubmodelElementWrapper(archeTypeProperty));

            env.Submodels.Add(bomSubmodel);
            aas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());

            return bomSubmodel;
        }

        private Entity CreateMainEntity(Submodel bomSubmodel, XElement harnessDescription)
        {
            // create the main entity
            var mainEntity = new AdminShell.Entity();
            mainEntity.semanticId = new SemanticId(new Key("Entity", false, "IRI", "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0"));
            mainEntity.idShort = "EntryNode"; // GetDocumentNumber(harnessDescription);
            mainEntity.entityType = "SelfManagedEntity";
            mainEntity.assetRef = this.aas.assetRef;
            bomSubmodel.Add(mainEntity);

            // create the fragment relationship pointing to the DocumentVersion for the current harness
            var fragmentRelationship = CreateVecRelationship(mainEntity, GetElementFragment(harnessDescription));
            mainEntity.AddChild(fragmentRelationship);

            return mainEntity;
        }

        private void CreateComponentEntities(Submodel bomSubmodel, Entity mainEntity, XElement harnessDescription)
        {
            var compositionSpecifications = harnessDescription.Elements(XName.Get("Specification")).
                            Where(spec => spec.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "vec:CompositionSpecification");

            foreach (var spec in compositionSpecifications)
            {
                var components = spec.Elements(XName.Get("Component"));
                foreach (var component in components)
                {
                    CreateComponentEntity(bomSubmodel, mainEntity, component);
                }
            }
        }

        private Entity CreateComponentEntity(Submodel bomSubmodel, Entity mainEntity, XElement component)
        {
            string componentId = component.Attribute(XName.Get("id"))?.Value ?? null;
            string componentName = component.Element(XName.Get("Identification"))?.Value ?? null;
            var partId = component.Element(XName.Get("Part"))?.Value ?? null;

            if (componentId == null)
            {
                log?.Error("Unable to determine ID of component!");
                return null;
            }

            if (componentName == null)
            {
                log?.Error("Unable to determine name of component");
                return null;
            }

            // create the entity
            var componentEntity = new Entity();
            componentEntity.semanticId = new SemanticId(new Key("Entity", false, "IRI", "https://admin-shell.io/idta/HierarchicalStructures/Node/1/0"));
            componentEntity.idShort = componentName;
            mainEntity.Add(componentEntity);
            this.ComponentEntitiesById[component.Attribute(XName.Get("id"))?.Value] = componentEntity;

            // if an asset ID is defined for the referenced part (in the plugin options), use this as asset reference
            var partNumber = GetPartNumberByPartId(partId);
            if (partNumber == null || !this.options.AssetIdByPartNumberDict.ContainsKey(partNumber))
            {
                componentEntity.entityType = "CoManagedEntity";
            } else
            {
                componentEntity.entityType = "SelfManagedEntity";
                componentEntity.assetRef = new AssetRef(new Reference(new Key("AssetAdministrationShell", false, "IRI", this.options.AssetIdByPartNumberDict[partNumber])));
            }

            // create the fragment relationship pointing to the Component element for the current component
            var fragmentRelationship = CreateVecRelationship(componentEntity, GetElementFragment(component));
            componentEntity.AddChild(fragmentRelationship);

            // create the relationship between the main and the component entity
            mainEntity.AddChild(CreateBomRelationship(componentName, mainEntity, componentEntity));

            return componentEntity;
        }
        private void CreateModuleEntities(Submodel bomSubmodel, Entity mainEntity, XElement harnessDescription)
        {
            var partStructureSpecifications = harnessDescription.Elements(XName.Get("Specification")).
                            Where(spec => spec.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "vec:PartStructureSpecification");

            foreach (var spec in partStructureSpecifications)
            {
                var moduleEntity = CreateComponentEntity(bomSubmodel, mainEntity, spec);

                var inBillOfMaterial = spec.Element(XName.Get("InBillOfMaterial"))?.Value ?? null;

                if (inBillOfMaterial == null)
                {
                    continue;
                }

                var componentIds = inBillOfMaterial.Split(' ');
                
                foreach (var id in componentIds)
                {
                    var componentEntity = this.ComponentEntitiesById[id];
                    moduleEntity.AddChild(CreateBomRelationship(componentEntity.idShort, moduleEntity, componentEntity));
                }
            }
        }

        protected SubmodelElementWrapper CreateVecRelationship(SubmodelElement first, string xpathToSecond)
        {
            var rel = new AdminShellV20.RelationshipElement();
            rel.idShort = "VEC_Reference";
            rel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("ConceptDescription", false, "IRI", "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0"));

            var second = this.vecSubmodel.GetReference();
            second.Keys.AddRange(this.vecFileSubmodelElement.GetReference().Keys);
            second.Keys.Add(new AdminShellV20.Key("FragmentReference", true, "FragmentId", xpathToSecond));

            rel.Set(first.GetReference(), second);

            return new AdminShellV20.SubmodelElementWrapper(rel);
        }

        protected SubmodelElementWrapper CreateBomRelationship(string idShort, SubmodelElement first, SubmodelElement second)
        {
            var rel = new RelationshipElement();
            rel.idShort = "HasPart_" + idShort;
            rel.semanticId = new SemanticId(new Key("ConceptDescription", false, "IRI", "https://admin-shell.io/idta/HierarchicalStructures/HasPart/1/0"));
            rel.Set(first.GetReference(), second.GetReference());
            return new SubmodelElementWrapper(rel);
        }

        protected string GetPartNumberByPartId(string partId)
        {
            if (partId == null)
            {
                return null;
            }

            var parts = this.vecFile?.Descendants(XName.Get("PartVersion")).ToList() ?? new List<XElement>();

            return parts.Find(part => part.Attribute("id")?.Value == partId)?.Element(XName.Get("PartNumber"))?.Value ?? null;
        }
        
        protected string GetElementFragment(XElement element)
        {
            string name = element.Name.LocalName;

            if (name == "DocumentVersion")
            {
                string companyName = GetCompanyName(element);
                string documentNumber = GetDocumentNumber(element);
                string documentVersion = GetDocumentVersion(element);

                return $"//DocumentVersion[./CompanyName='{companyName}'][./DocumentNumber='{documentNumber}']​[./DocumentVersion='{documentVersion}']";
            } 

            string identification = GetIdentification(element);

            if (identification != null)
            {
                return GetElementFragment(element.Parent) + $"/{name}[./Identification='{identification}']";
            }

            throw new Exception($"Unable to compile XPath fragment for element type {name}!");

        }

        protected string GetIdentification(XElement element)
        {
            return element.Element(XName.Get("Identification"))?.Value ?? null;
        }

        protected string GetCompanyName(XElement documentVersionElement)
        {
            string companyName = documentVersionElement.Element(XName.Get("CompanyName"))?.Value ?? null;
            
            if (companyName == null)
            {
                throw new Exception("Unable to determine CompanyName of harness description!");
            }

            return companyName;
        }

        protected string GetDocumentNumber(XElement documentVersionElement)
        {
            string documentNumber = documentVersionElement.Element(XName.Get("DocumentNumber"))?.Value ?? null;

            if (documentNumber == null)
            {
                throw new Exception("Unable to determine DocumentNumber of harness description!");
            }

            return documentNumber;
        }

        protected string GetDocumentVersion(XElement documentVersionElement)
        {
            string documentVersion = documentVersionElement.Element(XName.Get("DocumentVersion"))?.Value ?? null;

            if (documentVersion == null)
            {
                throw new Exception("Unable to determine DocumentVersion of harness description!");
            }

            return documentVersion;
        }

        protected XDocument ParseVecFile(string fn)
        {
            return XDocument.Load(fn);
        }*/
    }
}
