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
    /// This class allows to associate a set of subassemblies to a module that can be ordered by an OEM.
    /// </summary>
    public class SubassemblyToModuleAssociator
    {
        //
        // Public interface
        //

        public static void AssociateSubassemblies(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> subassembliesToAssociate,
            Entity orderableModule,
            VecOptions options,
            LogInstance log = null)
        {
            // safe
            try
            {
                var associator = new SubassemblyToModuleAssociator(env, aas, subassembliesToAssociate, orderableModule, options, log);
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

        protected SubassemblyToModuleAssociator(
            AdministrationShellEnv env,
            AdministrationShell aas,
            IEnumerable<Entity> subassembliesToAssociate,
            Entity orderableModule,
            VecOptions options,
            LogInstance log = null)
        {
            

            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));
            this.subassembliesToAssociate = subassembliesToAssociate ?? throw new ArgumentNullException(nameof(subassembliesToAssociate));
            this.orderableModule = orderableModule ?? throw new ArgumentNullException(nameof(orderableModule));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected AdministrationShellEnv env;
        protected AdministrationShell aas;
        protected IEnumerable<Entity> subassembliesToAssociate;
        protected Entity orderableModule;
        protected VecOptions options;
        protected LogInstance log;

        protected void AssociateSubassemblies()
        {
            var allBomSubmodels = FindBomSubmodels(aas, env);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (!subassembliesToAssociate.All(e => RepresentsSubAssembly(e) || RepresentsBasciComponent(e)))
            {
                log?.Error("It seems that entities were selected that do not represent a subassembly or a basic part/component. This is currently supported!");
                return;
            }

            foreach (var subassembly in subassembliesToAssociate)
            {
                AssociateSubassemblyWithModule(subassembly, this.orderableModule);
            }
        }        
    }
}
