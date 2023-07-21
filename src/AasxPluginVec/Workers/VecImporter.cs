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
using static AasxPluginVec.VecProvider;
using static AasxPluginVec.SubassemblyUtils;

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
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            string pathToVecFile,
            VecOptions options,
            LogInstance log = null)
        {
            // access
            if (!pathToVecFile.HasContent())
            {
                log?.Error("Import VEC: no valid filename!");
                return;
            }

            // safe
            try
            {
                var importer = new VecImporter(packageEnv, env, aas, pathToVecFile, options, log);
                importer.ImportVec();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"importing VEC file {pathToVecFile}");
            }
        }

        //
        // Internal
        //

        protected VecImporter(
            AdminShellPackageEnv packageEnv,
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            string pathToVecFile,
            VecOptions options,
            LogInstance log = null)
        {
            if (string.IsNullOrEmpty(pathToVecFile))
            {
                throw new ArgumentException($"'{nameof(pathToVecFile)}' cannot be null or empty.", nameof(pathToVecFile));
            }

            this.packageEnv = packageEnv ?? throw new ArgumentNullException(nameof(packageEnv));
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.pathToVecFile = pathToVecFile;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.vecSubmodel = null;
            this.bomComponentSubmodels = new List<Submodel>();
            this.bomModuleSubmodels = new List<Submodel>();
            this.vecProvider = new VecProvider(pathToVecFile);
            this.vecFileSubmodelElement = null;
            this.ComponentEntitiesById = new Dictionary<string, Entity>();
        }

        protected AdminShellPackageEnv packageEnv;
        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected Submodel vecSubmodel;
        protected List<Submodel> bomComponentSubmodels;
        protected List<Submodel> bomModuleSubmodels;
        protected string pathToVecFile;
        protected VecOptions options;
        protected LogInstance log;
        protected VecProvider vecProvider;
        protected AasCore.Aas3_0.File vecFileSubmodelElement;
        protected Dictionary<String, Entity> ComponentEntitiesById;


        protected void ImportVec()
        {
            CreateVecSubmodel();

            if (vecProvider.HarnessDescriptions.Count == 0)
            {
                log?.Error("Unable to find HarnessDescription in VEC file...");
                return;
            }

            foreach (var harness in vecProvider.HarnessDescriptions)
            {
                CreateBomSubmodels(harness);
            }
        }

        protected void CreateVecSubmodel()
        {
            var id = AdminShellUtil.GenerateIdAccordingTemplate(options.TemplateIdSubmodel);

            // create the VEC submodel
            var vecSubmodel = VecSMUtils.CreateVecSubmodel(id, pathToVecFile, this.packageEnv);

            this.env.Submodels.Add(vecSubmodel);
            this.aas.AddSubmodelReference(vecSubmodel.GetReference());

            this.vecFileSubmodelElement = vecSubmodel.FindFirstIdShortAs<AasCore.Aas3_0.File>(VEC_FILE_ID_SHORT);

            if (this.vecFileSubmodelElement == null)
            {
                log?.Error("Unable to find VEC file element in created VEC submodel...");
            }
        }

        protected void CreateBomSubmodels(XElement harnessDescription)
        {
            var index = (bomComponentSubmodels.Count() + 1).ToString().PadLeft(2, '0');
            var harnessFragment = GetElementFragment(harnessDescription);

            var bomComponentsSubmodelIdShort = ID_SHORT_COMPONENTS_SM + "_" + index;
            var bomComponentsSubmodel = CreateBomSubmodel(bomComponentsSubmodelIdShort, options.TemplateIdSubmodel, aas: aas, env: env);
            var bomComponentsEntryNode = bomComponentsSubmodel.FindEntryNode();
            CreateVecRelationship(bomComponentsEntryNode, harnessFragment, this.vecFileSubmodelElement);
            CreateComponentEntities(bomComponentsEntryNode, harnessDescription);

            var bomModulesSubmodelIdShort = ID_SHORT_ORDERABLE_MODULES_SM + "_" + index;
            var bomModulesSubmodel = CreateBomSubmodel(bomModulesSubmodelIdShort, options.TemplateIdSubmodel, aas: aas, env: env);
            var bomModulesEntryNode = bomModulesSubmodel.FindEntryNode();
            CreateVecRelationship(bomModulesEntryNode, harnessFragment, this.vecFileSubmodelElement);
            CreateModuleEntities(bomModulesEntryNode, harnessDescription);
        }

        private void CreateComponentEntities(Entity mainEntity, XElement harnessDescription)
        {
            var compositionSpecifications = FindCompositionSpecifications(harnessDescription);

            foreach (var component in compositionSpecifications.SelectMany(spec => FindComponentsInComposition(spec)))
            {
                CreateComponentEntity(mainEntity, component);
            }
        }

        private Entity CreateComponentEntity(Entity mainEntity, XElement component)
        {
            string componentName = GetIdentification(component);

            if (componentName == null)
            {
                log?.Error("Unable to determine name of component");
                return null;
            }

            var partId = GetPartId(component);

            // if an asset ID is defined for the referenced part (in the plugin options), use this as asset reference
            var partNumber = this.vecProvider.GetPartNumber(partId);
            string assetId = null;
            if (partNumber != null)
            {
                this.options.AssetIdByPartNumberDict.TryGetValue(partNumber, out assetId);
            }
            
            // create the entity
            var componentEntity = CreateNode(componentName, mainEntity, assetId);
            this.ComponentEntitiesById[GetXmlId(component)] = componentEntity;

            // create the fragment relationship pointing to the Component element for the current component
            CreateVecRelationship(componentEntity, GetElementFragment(component), this.vecFileSubmodelElement);

            // create the relationship between the main and the component entity
            CreateHasPartRelationship(mainEntity, componentEntity);

            return componentEntity;
        }

        private void CreateModuleEntities(Entity mainEntity, XElement harnessDescription)
        {
            var partStructureSpecifications = FindPartStructureSpecifications(harnessDescription);

            foreach (var spec in partStructureSpecifications)
            {
                var moduleEntity = CreateModuleEntity(mainEntity, spec);
                
                /*foreach (var id in FindIdsOfContainedParts(spec))
                {
                    var componentEntity = this.ComponentEntitiesById[id];
                    CreateHasPartRelationship(moduleEntity, componentEntity);
                }*/
            }
        }

        private Entity CreateModuleEntity(Entity mainEntity, XElement component)
        {
            string componentName = GetIdentification(component);

            if (componentName == null)
            {
                log?.Error("Unable to determine name of component");
                return null;
            }

            // create the entity
            var componentEntity = CreateNode(componentName, mainEntity);

            // create the fragment relationship pointing to the Component element for the current component
            CreateVecRelationship(componentEntity, GetElementFragment(component), this.vecFileSubmodelElement);

            // create the relationship between the main and the component entity
            CreateHasPartRelationship(mainEntity, componentEntity);

            return componentEntity;
        }
    }
}
