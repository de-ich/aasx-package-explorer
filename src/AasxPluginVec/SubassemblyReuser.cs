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
    /// This class allows to reuse an existing subassembly for a set of selected entities
    /// in a BOM submodel.
    /// </summary>
    public class SubassemblyReuser
    {
        //
        // Public interface
        //

        public static void ReuseSubassembly(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> entities,
            AdministrationShell aasToReuse,
            string subassemblyEntityName,
            Dictionary<string, string> partNames,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var reuser = new SubassemblyReuser(env, aas, entities, aasToReuse, subassemblyEntityName, partNames, options, log);
                reuser.ReuseSubassembly();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"reusing subassembly");
            }
        }

        //
        // Internal
        //

        protected SubassemblyReuser(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> entities,
            AdministrationShell aasToReuse,
            string subassemblyEntityName,
            Dictionary<string, string> partNames,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.entitiesToBeMadeSubassembly = entities ?? throw new ArgumentNullException(nameof(entities));
            this.aasToReuse = aasToReuse ?? throw new ArgumentNullException(nameof(aasToReuse));
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
        protected AdministrationShell aasToReuse;
        protected string subassemblyEntityName;
        protected Dictionary<string, string> partNames;
        protected Submodel newVecSubmodel;
        protected AdminShellV20.File newVecFileSME;
        protected Submodel existingBomSubmodel;
        protected Submodel newBomSubmodel;
        protected AdministrationShell subassemblyAas;
        protected VecOptions options;
        protected LogInstance log;

        protected void ReuseSubassembly()
        {
            existingBomSubmodel = FindCommonSubmodelParent(entitiesToBeMadeSubassembly);
            if (existingBomSubmodel == null)
            {
                log?.Error("Unable to determine the single common BOM submodel that contains the selected entities!");
                return;
            }

            var bomSubmodelInSubAssemblyAAS = FindFirstBomSubmodel(aasToReuse, env);
            bomSubmodelInSubAssemblyAAS.SetAllParents();
            var atomicComponentEntitiesInSubAssemblyAAS = GetLeafNodes(bomSubmodelInSubAssemblyAAS);

            // get or create the 'building blocks' submodel 
            var existingBuildingBlocksBomSubmodel = FindBuildingBlocksSubmodel(aas, env);
            if (existingBuildingBlocksBomSubmodel == null)
            {
               
                // no building blocks submodel seems to exist -> create a new one
                try
                {
                    existingBuildingBlocksBomSubmodel = CreateBuildingBlocksSubmodel(options.TemplateIdSubmodel, existingBuildingBlocksBomSubmodel, aas, env);
                }
                catch (Exception e)
                {
                    log?.Error(e.Message);
                    return;
                }
            }

            var buildingBlocksSubmodelEntryNode = FindEntryNode(existingBuildingBlocksBomSubmodel);

            // the entity representing the sub-assembly in the BOM SM of the original AAS (the harness AAS)
            var subassemblyEntityInOriginalAAS = CreateNode(subassemblyEntityName, buildingBlocksSubmodelEntryNode, aasToReuse.assetRef);
            CreateHasPartRelationship(buildingBlocksSubmodelEntryNode, subassemblyEntityInOriginalAAS);

            foreach (var partEntityInOriginalAAS in entitiesToBeMadeSubassembly)
            {
                CreateHasPartRelationship(subassemblyEntityInOriginalAAS, partEntityInOriginalAAS);

                var idShort = this.partNames[partEntityInOriginalAAS.idShort];
                var partEntityInNewAAS = atomicComponentEntitiesInSubAssemblyAAS.First(e => e.idShort == idShort);

                CreateSameAsRelationship(partEntityInOriginalAAS, partEntityInNewAAS, subassemblyEntityInOriginalAAS, partEntityInOriginalAAS.idShort + "_SameAs_" + idShort);
            }
        }    
    }
}
