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

namespace AasxPluginVec
{
    /// <summary>
    /// This class allows importing a VEC file into an existing submodel.
    /// Additionally, it allows to generate a BOM submodel based on the contents
    /// in the VEC file.
    /// </summary>
    public class VecImporter
    {
        //
        // Public interface
        //

        public static void ImportVecFromFile(
            AdminShellPackageEnv packageEnv,
            AdministrationShellEnv env,
            AdministrationShell aas,
            string fn,
            VecOptions options,
            LogInstance log = null)
        {
            // access
            if (!fn.HasContent())
            {
                log?.Error("Import VEC: no valid filename!");
                return;
            }

            // safe
            try
            {
                var importer = new VecImporter(packageEnv, env, aas, fn, options, log);
                importer.ImportVec();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"importing VEC file {fn}");
            }
        }

        //
        // Internal
        //

        protected VecImporter(
            AdminShellPackageEnv packageEnv,
            AdministrationShellEnv env,
            AdministrationShell aas,
            string fn,
            VecOptions options,
            LogInstance log = null)
        {
            if (string.IsNullOrEmpty(fn))
            {
                throw new ArgumentException($"'{nameof(fn)}' cannot be null or empty.", nameof(fn));
            }

            this.packageEnv = packageEnv ?? throw new ArgumentNullException(nameof(packageEnv));
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.fn = fn;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.vecSubmodel = null;
            this.bomSubmodels = new List<Submodel>();
            this.vecFile = ParseVecFile(fn);
            this.vecFileSubmodelElement = null;
            this.ComponentEntitiesById = new Dictionary<string, Entity>();
        }

        protected AdminShellPackageEnv packageEnv;
        protected AdministrationShellEnv env;
        protected AdministrationShell aas;
        protected Submodel vecSubmodel;
        protected List<Submodel> bomSubmodels;
        protected string fn;
        protected VecOptions options;
        protected LogInstance log;
        protected XDocument vecFile;
        protected AdminShell.File vecFileSubmodelElement;
        protected Dictionary<String, Entity> ComponentEntitiesById;


        protected void ImportVec()
        {
            CreateVecSubmodel();

            var harnessDescriptions = vecFile.Descendants(XName.Get("DocumentVersion")).Where(doc => doc.Element(XName.Get("DocumentType"))?.Value == "HarnessDescription").ToList();

            if (harnessDescriptions.Count == 0)
            {
                log?.Error("Unable to find HarnessDescription in VEC file...");
                return;
            }

            foreach (var harness in harnessDescriptions)
            {
                CreateBomSubmodel(harness);
            }
        }

        protected void CreateVecSubmodel()
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
            bomSubmodel.semanticId = new SemanticId(new Key("Submodel", false, "IRI", "http://example.com/id/type/submodel/BOM/1/1"));
            env.Submodels.Add(bomSubmodel);
            aas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());

            return bomSubmodel;
        }

        private Entity CreateMainEntity(Submodel bomSubmodel, XElement harnessDescription)
        {
            // create the main entity
            var mainEntity = new AdminShell.Entity();
            mainEntity.idShort = GetDocumentNumber(harnessDescription);
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
            componentEntity.idShort = componentName;
            bomSubmodel.Add(componentEntity);
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
            rel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("ConceptDescription", false, "IRI", "http://arena2036.de/vws4ls/vec/VecPartReference/1/0"));

            var second = this.vecSubmodel.GetReference();
            second.Keys.AddRange(this.vecFileSubmodelElement.GetReference().Keys);
            second.Keys.Add(new AdminShellV20.Key("FragmentReference", true, "FragmentId", xpathToSecond));

            rel.Set(first.GetReference(), second);

            return new AdminShellV20.SubmodelElementWrapper(rel);
        }

        protected SubmodelElementWrapper CreateBomRelationship(string idShort, SubmodelElement first, SubmodelElement second)
        {
            var rel = new RelationshipElement();
            rel.idShort = idShort;
            rel.semanticId = new SemanticId(new Key("ConceptDescription", false, "IRI", "http://admin-shell.io/sandbox/CompositeComponent/General/IsPartOfForBOM/1/0"));
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
        }
    }
}
