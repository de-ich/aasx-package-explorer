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
    /// This class allows to associate a set of subassemblies to a configuration (that can e.g. be ordered by an OEM).
    /// </summary>
    public class SubassemblyToConfigurationAssociator
    {
        //
        // Public interface
        //

        public static void AssociateSubassemblies(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> subassembliesToAssociate,
            IEntity orderableModule,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var associator = new SubassemblyToConfigurationAssociator(env, aas, subassembliesToAssociate, orderableModule, options, log);
                associator.AssociateSubassemblies();
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"deriving subassembly");
            }
        }

        //
        // Internal
        //

        protected SubassemblyToConfigurationAssociator(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            IEnumerable<Entity> subassembliesToAssociate,
            IEntity configuration,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.subassembliesToAssociate = subassembliesToAssociate ?? throw new ArgumentNullException(nameof(subassembliesToAssociate));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected IEnumerable<Entity> subassembliesToAssociate;
        protected IEntity configuration;
        protected VecOptions options;
        protected LogInstance log;

        protected void AssociateSubassemblies()
        {
            var allBomSubmodels = FindBomSubmodels(env, aas);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (!subassembliesToAssociate.All(RepresentsSubAssembly))
            {
                log?.Error("Entities were selected that are not part of the manufacturing BOM. This is not supported!");
                return;
            }

            foreach (var subassembly in subassembliesToAssociate)
            {
                // create the node for the subassembly in the configuration bom
                var subassemblyInConfigurationBom = CreateNode(subassembly, configuration);

                // linke the node for the subassembly in the configuration bom with the subassembly in the manufacturing bom
                CreateSameAsRelationship(subassemblyInConfigurationBom, subassembly);
            }
        }        
    }
}
