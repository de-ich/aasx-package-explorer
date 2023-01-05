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
            this.vecFile = ParseVecFile(fn);
        }

        protected AdminShellPackageEnv packageEnv;
        protected AdminShell.AdministrationShellEnv env;
        protected AdminShellV20.AdministrationShell aas;
        protected AdminShell.Submodel vecSubmodel = null;
        protected AdminShell.Submodel bomSubmodel = null;
        protected string fn;
        private VecOptions options;
        protected LogInstance log = null;
        protected XDocument vecFile = null;

        protected void ImportVec()
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

            // create the BOM submodel
            bomSubmodel = new AdminShell.Submodel();
            bomSubmodel.SetIdentification(AdminShell.Identification.IRI, AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdConceptDescription), "LS_BOM");
            bomSubmodel.semanticId = new AdminShellV20.SemanticId(new AdminShellV20.Key("Submodel", true, "IRI", "http://example.com/id/type/submodel/BOM/1/1"));
            env.Submodels.Add(bomSubmodel);
            aas.AddSubmodelRef(bomSubmodel.GetSubmodelRef());
        }

        protected XDocument ParseVecFile(string fn)
        {
            return XDocument.Load(fn);
        }
    }
}
