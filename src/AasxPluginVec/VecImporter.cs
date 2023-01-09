/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Newtonsoft.Json;

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
            AdminShell.AdministrationShellEnv env,
            AdminShellV20.AdministrationShell aas,
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
            AdminShell.AdministrationShellEnv env,
            AdminShellV20.AdministrationShell aas,
            string fn,
            VecOptions options,
            LogInstance log = null)
        {
            this.packageEnv = packageEnv;
            this.env = env;
            this.aas = aas;
            this.fn = fn;
            this.options = options;
            this.log = log;
            this.vecSubmodel = null;
            this.bomSubmodels = new List<AdminShellV20.Submodel>();
            this.vecFile = ParseVecFile(fn);
            this.vecFileSubmodelElement = null;
        }

        protected AdminShellPackageEnv packageEnv;
        protected AdminShell.AdministrationShellEnv env;
        protected AdminShellV20.AdministrationShell aas;
        protected AdminShell.Submodel vecSubmodel;
        protected List<AdminShell.Submodel> bomSubmodels;
        protected string fn;
        protected VecOptions options;
        protected LogInstance log;
        protected XDocument vecFile;
        protected AdminShellV20.File vecFileSubmodelElement;


        protected void ImportVec()
        {
            CreateVecSubmodel();

            var harnessDescriptions = vecFile.Descendants(XName.Get("DocumentVersion")).Where(doc => doc.Element(XName.Get("DocumentType"))?.Value == "HarnessDescription").ToList();

            if (harnessDescriptions.Count == 0)
            {
                log.Error("Unable to find HarnessDescription in VEC file...");
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
            vecSubmodel = new AdminShell.Submodel();
            vecSubmodel.SetIdentification(AdminShell.Identification.IRI, AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdConceptDescription), "VEC");
            vecSubmodel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("Submodel", true, "IRI", "http://arena2036.de/vws4ls/vec/VecFileReference/1/0"));
            env.Submodels.Add(vecSubmodel);
            aas.AddSubmodelRef(vecSubmodel.GetSubmodelRef());

            // create the VEC file submodel element
            var file = new AdminShell.File();
            file.idShort = "VEC";
            file.mimeType = "text/xml";
            file.value = localFilePath;
            vecSubmodel.AddChild(new AdminShellV20.SubmodelElementWrapper(file));
            this.vecFileSubmodelElement = file;
        }

        protected void CreateBomSubmodel(XElement harnessDescription)
        {
            string harnessId = harnessDescription.Attribute(XName.Get("id"))?.Value ?? null;
            string harnessDocumentNumber = harnessDescription.Element(XName.Get("DocumentNumber"))?.Value ?? null;

            if (harnessId == null)
            {
                log.Error("Unable to determine ID of harness description!");
                return;
            }

            if (harnessDocumentNumber == null)
            {
                log.Error("Unable to determine DocumentNumber of harness description!");
                return;
            }

            var bomSubmodel = InitializeBomSubmodel();
            var mainEntity = CreateMainEntity(bomSubmodel, harnessId, harnessDocumentNumber);
            CreateComponentEntities(bomSubmodel, mainEntity, harnessDescription, harnessId);
        }

        private AdminShellV20.Submodel InitializeBomSubmodel()
        {
            // create the BOM submodel
            var bomSubmodel = new AdminShell.Submodel();
            bomSubmodels.Add(bomSubmodel);

            var id = AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdConceptDescription);

            // 'GenerateIdAccordingTemplate' does not seem to generate unique ids when called multiple times
            // in too short of a time span so we ensure uniqueness manually
            id = id.Substring(0, id.Length - 1) + bomSubmodels.Count();

            var idShort = "LS_BOM_" + bomSubmodels.Count().ToString().PadLeft(2, '0');
            bomSubmodel.SetIdentification(AdminShell.Identification.IRI, id, idShort);
            bomSubmodel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("Submodel", false, "IRI", "http://example.com/id/type/submodel/BOM/1/1"));
            env.Submodels.Add(bomSubmodel);
            aas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());

            return bomSubmodel;
        }

        private AdminShellV20.Entity CreateMainEntity(AdminShellV20.Submodel bomSubmodel, string harnessId, string harnessDocumentNumber)
        {
            // create the main entity
            var mainEntity = new AdminShell.Entity();
            mainEntity.idShort = harnessDocumentNumber;
            mainEntity.entityType = "SelfManagedEntity";
            mainEntity.assetRef = this.aas.assetRef;
            bomSubmodel.Add(mainEntity);

            // create the fragment relationship pointing to the DocumentVersion for the current harness
            var fragmentRelationship = CreateVecRelationship(mainEntity, GetDocumentVersionFragment(harnessId));
            mainEntity.AddChild(fragmentRelationship);

            return mainEntity;
        }

        private void CreateComponentEntities(AdminShellV20.Submodel bomSubmodel, AdminShellV20.Entity mainEntity, XElement harnessDescription, string harnessId)
        {
            var compositionSpecifications = harnessDescription.Elements(XName.Get("Specification")).
                            Where(spec => spec.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "vec:CompositionSpecification");

            foreach (var spec in compositionSpecifications)
            {
                var components = spec.Elements(XName.Get("Component"));
                foreach (var component in components)
                {
                    CreateComponentEntity(bomSubmodel, mainEntity, component, harnessId);
                }
            }
        }

        private AdminShellV20.Entity CreateComponentEntity(AdminShellV20.Submodel bomSubmodel, AdminShellV20.Entity mainEntity, XElement component, string harnessId)
        {
            string componentId = component.Attribute(XName.Get("id"))?.Value ?? null;
            string componentName = component.Element(XName.Get("Identification"))?.Value ?? null;

            if (componentId == null)
            {
                log.Error("Unable to determine ID of component!");
                return null;
            }

            if (componentName == null)
            {
                log.Error("Unable to determine name of component");
                return null;
            }

            // create the entity
            var componentEntity = new AdminShell.Entity();
            componentEntity.idShort = componentName;
            componentEntity.entityType = "SelfManagedEntity";
            bomSubmodel.Add(componentEntity);

            // create the fragment relationship pointing to the Component element for the current component
            var fragmentRelationship = CreateVecRelationship(componentEntity, GetComponentFragment(harnessId, componentId));
            componentEntity.AddChild(fragmentRelationship);

            // create the relationship between the main and the component entity
            mainEntity.AddChild(CreateBomRelationship(componentName, mainEntity, componentEntity));

            return componentEntity;
        }

        protected AdminShellV20.SubmodelElementWrapper CreateVecRelationship(AdminShellV20.SubmodelElement first, string xpathToSecond)
        {
            var rel = new AdminShellV20.RelationshipElement();
            rel.idShort = "VEC_Reference";
            rel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("ConceptDescription", false, "IRI", "http://arena2036.de/vws4ls/vec/VecPartReference/1/0"));

            var second = this.vecFileSubmodelElement.GetReference();
            second.Keys.Add(new AdminShellV20.Key("FragmentReference", true, "FragmentId", xpathToSecond));

            rel.Set(first.GetReference(), second);

            return new AdminShellV20.SubmodelElementWrapper(rel);
        }

        protected AdminShellV20.SubmodelElementWrapper CreateBomRelationship(string idShort, AdminShellV20.SubmodelElement first, AdminShellV20.SubmodelElement second)
        {
            var rel = new AdminShellV20.RelationshipElement();
            rel.idShort = idShort;
            rel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("ConceptDescription", false, "IRI", "http://admin-shell.io/sandbox/CompositeComponent/General/IsPartOfForBOM/1/0"));
            rel.Set(first.GetReference(), second.GetReference());
            return new AdminShellV20.SubmodelElementWrapper(rel);
        }

        protected string GetDocumentVersionFragment(string documentVersionId)
        {
            return "//DocumentVersion[@id=" + documentVersionId + "]";
        }

        protected string GetComponentFragment(string documentVersionID, string componentVersionId)
        {
            return GetDocumentVersionFragment(documentVersionID) +  "//Component[@id=" + componentVersionId + "]";
        }

        protected XDocument ParseVecFile(string fn)
        {
            return XDocument.Load(fn);
        }
    }
}
