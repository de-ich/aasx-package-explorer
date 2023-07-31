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

        public static ISubmodel ImportVecFromFile(
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
                return null;
            }

            // safe
            try
            {
                var importer = new VecImporter(packageEnv, env, aas, pathToVecFile, options, log);
                return importer.ImportVec();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"importing VEC file {pathToVecFile}");
                return null;
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
            this.vecProvider = new VecProvider(pathToVecFile);
            this.vecFileSubmodelElement = null;
        }

        protected AdminShellPackageEnv packageEnv;
        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected ISubmodel vecSubmodel;
        protected string pathToVecFile;
        protected VecOptions options;
        protected LogInstance log;
        protected VecProvider vecProvider;
        protected AasCore.Aas3_0.File vecFileSubmodelElement;


        protected ISubmodel ImportVec()
        {
            vecSubmodel = CreateVecSubmodel();
            vecFileSubmodelElement = vecSubmodel.GetVecFileElement();

            if (vecFileSubmodelElement == null)
            {
                log?.Error("Unable to find VEC file element in created VEC submodel...");
                return null;
            }

            if (vecProvider.HarnessDescriptions.Count == 0)
            {
                log?.Error("Unable to find HarnessDescription in VEC file...");
                return null;
            }

            ImportHarnessDescriptions();

            return vecSubmodel;
        }

        private void ImportHarnessDescriptions()
        {
            for (int i = 0; i < vecProvider.HarnessDescriptions.Count; i++)
            {
                var indexSuffix = vecProvider.HarnessDescriptions.Count > 0 ? "_" + (i + 1).ToString().PadLeft(2, '0') : "";
                var harnessDescription = vecProvider.HarnessDescriptions[i];
                ImportHarnessDescription(indexSuffix, harnessDescription);
            }
        }

        private void ImportHarnessDescription(string indexSuffix, XElement harnessDescription)
        {
            var componentsSubmodel = CreateComponentsSubmodel(indexSuffix);
            var bomComponentsEntryNode = componentsSubmodel.FindEntryNode();

            var modulesSubmodel = CreateModulesSubmodel(indexSuffix);
            var bomModulesEntryNode = modulesSubmodel.FindEntryNode();

            var entitiesByXmlElement = new List<(XElement xmlElement, IEntity entity)>()
                {
                    (harnessDescription, bomComponentsEntryNode),
                    (harnessDescription, bomModulesEntryNode)
                };

            entitiesByXmlElement.AddRange(CreateComponentEntities(bomComponentsEntryNode, harnessDescription));
            entitiesByXmlElement.AddRange(CreateModuleEntities(bomModulesEntryNode, harnessDescription));

            foreach (var (xmlElement, entity) in entitiesByXmlElement)
            {
                // create the fragment relationship pointing to the Component element for the current component
                CreateVecRelationship(entity, GetElementFragment(xmlElement), this.vecFileSubmodelElement);
            }
        }

        protected ISubmodel CreateVecSubmodel()
        {
            // create the VEC submodel
            return VecSMUtils.CreateVecSubmodel(pathToVecFile, options.TemplateIdSubmodel, aas, env, packageEnv);
        }

        protected ISubmodel CreateComponentsSubmodel(string indexSuffix = "")
        {
            var bomComponentsSubmodelIdShort = ID_SHORT_PRODUCT_BOM_SM + indexSuffix;
            var bomComponentsSubmodel = CreateBomSubmodel(bomComponentsSubmodelIdShort, options.TemplateIdSubmodel, aas: aas, env: env, supplementarySemanticId: SEM_ID_PRODUCT_BOM_SM);

            return bomComponentsSubmodel;
        }

        protected ISubmodel CreateModulesSubmodel(string indexSuffix = "")
        {
            var bomModulesSubmodelIdShort = ID_SHORT_CONFIGURATION_BOM_SM + indexSuffix;
            var bomModulesSubmodel = CreateBomSubmodel(bomModulesSubmodelIdShort, options.TemplateIdSubmodel, aas: aas, env: env, supplementarySemanticId: SEM_ID_CONFIGURATION_BOM_SM);

            return bomModulesSubmodel;
        }

        private IEnumerable<(XElement xmlElement, IEntity entity)> CreateComponentEntities(IEntity mainEntity, XElement harnessDescription)
        {
            var createdEntities = new List<(XElement xmlElement, IEntity entity)>();

            var compositionSpecifications = FindCompositionSpecifications(harnessDescription);

            foreach (var component in compositionSpecifications.SelectMany(spec => FindComponentsInComposition(spec)))
            {
                var entity = CreateComponentEntity(mainEntity, component);
                if (entity != null)
                {
                    createdEntities.Add((component, entity));
                }
            }

            return createdEntities;
        }

        private Entity CreateComponentEntity(IEntity mainEntity, XElement component)
        {
            string componentName = GetIdentification(component);

            if (componentName == null)
            {
                log?.Error("Unable to determine name of component");
                return null;
            }

            var partId = GetPartId(component);

            // try to determine an assetId for the given part number
            var partNumber = this.vecProvider.GetPartNumber(partId);
            string assetId = null;
            if (partNumber != null)
            {
                // first option: check if a component AAS with a matching specific asset ID is defined in the current environment
                assetId = this.env.AssetAdministrationShells.FirstOrDefault(aas => AasHasSpecificAssetIdForPartNumber(aas, partNumber))?.AssetInformation.GlobalAssetId;

                // second option: use an asset ID that is defined in the plugin options
                if (assetId == null)
                {
                    this.options.AssetIdByPartNumberDict.TryGetValue(partNumber, out assetId);
                }
            }
            
            // create the entity
            var componentEntity = CreateNode(componentName, mainEntity, assetId, true);

            return componentEntity;
        }

        private bool AasHasSpecificAssetIdForPartNumber(IAssetAdministrationShell aas, string partNumber)
        {
            var globalAssetIdOfWireHarness = aas.AssetInformation.GlobalAssetId;

            if (globalAssetIdOfWireHarness == null)
            {
                // a global asset ID needs to be present because we need to compare this to the 'externalSubjectID' of the specific asset IDs
                return false;
            }

            return aas.AssetInformation.OverSpecificAssetIdsOrEmpty().Any(id =>
            {
                var externalSubjectIdValue = id.ExternalSubjectId?.Keys.First()?.Value;
                var semanticIdValue = id.SemanticId?.Keys.First()?.Value;

                return externalSubjectIdValue != null && 
                    (globalAssetIdOfWireHarness?.Contains(externalSubjectIdValue) ?? false) &&
                    semanticIdValue == "0173-1#02-AAO676#003" &&
                    id.Value == partNumber;
            });
        }

        private IEnumerable<(XElement xmlElement, IEntity entity)> CreateModuleEntities(Entity mainEntity, XElement harnessDescription)
        {
            var createdEntities = new List<(XElement xmlElement, IEntity entity)>();

            var partStructureSpecifications = FindPartStructureSpecifications(harnessDescription);

            foreach (var spec in partStructureSpecifications)
            {
                var moduleEntity = CreateModuleEntity(mainEntity, spec);
                if (moduleEntity!= null)
                {
                    createdEntities.Add((spec, moduleEntity));
                }
            }

            return createdEntities;
        }

        private Entity CreateModuleEntity(Entity mainEntity, XElement component)
        {
            string componentName = GetIdentification(component);

            if (componentName == null)
            {
                log?.Error("Unable to determine name of component");
                return null;
            }

            return CreateNode(componentName, mainEntity, createHasPartRel: true);
        }
    }
}
