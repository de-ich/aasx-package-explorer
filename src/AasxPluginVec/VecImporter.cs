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
        }

        protected AdminShellPackageEnv packageEnv;
        protected AdminShell.AdministrationShellEnv env;
        protected AdminShellV20.AdministrationShell aas;
        protected AdminShell.Submodel vecSubmodel;
        protected List<AdminShell.Submodel> bomSubmodels;
        protected string fn;
        private VecOptions options;
        protected LogInstance log;
        protected XDocument vecFile;

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
            file.value =
            file.idShort = "VEC";
            file.mimeType = "text/xml";
            file.value = localFilePath;
            vecSubmodel.AddChild(new AdminShellV20.SubmodelElementWrapper(file));
        }

        protected void CreateBomSubmodel(XElement harnessDescription)
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

            // create the main entity
            var mainEntity = new AdminShell.Entity();
            mainEntity.idShort = harnessDescription.Element(XName.Get("DocumentNumber"))?.Value;
            bomSubmodel.Add(mainEntity);

        }

        protected XDocument ParseVecFile(string fn)
        {
            return XDocument.Load(fn);
        }
    }
}
