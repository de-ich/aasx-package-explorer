﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVec.BomSMUtils;
using static AasxPluginVec.VecSMUtils;
using static AasxPluginVec.BasicAasUtils;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace AasxPluginVec
{

    public static class SubassemblyUtils
    {
        public const string ID_SHORT_PRODUCT_BOM_SM = "LS_Product_BOM";
        public const string SEM_ID_PRODUCT_BOM_SM = "https://arena2036.de/vws4ls/submodels/product-bom/1/0";
        public const string ID_SHORT_CONFIGURATION_BOM_SM = "LS_Configuration_BOM";
        public const string SEM_ID_CONFIGURATION_BOM_SM = "https://arena2036.de/vws4ls/submodels/configuration-bom/1/0";
        public const string ID_SHORT_MANUFACTURING_BOM_SM = "LS_Manufacturing_BOM";
        public const string SEM_ID_MANUFACTURING_BOM_SM = "https://arena2036.de/vws4ls/submodels/manufacturing-bom/1/0";

        public static Submodel CreateManufacturingBom(string iriTemplate, ISubmodel associatedProductBom, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            var entryNodeInAssociatedProductBom = associatedProductBom.FindEntryNode();
            var vecReference = entryNodeInAssociatedProductBom?.GetVecRelationship(env, aas);

            if (vecReference == null)
            {
                throw new Exception("Unable to find VEC reference in existing product BOM!");
            }
            
            var idShort = ID_SHORT_MANUFACTURING_BOM_SM;

            // as there may be multiple product boms, we check if the idshort of the associated product bom has an extension that we reuse
            var counterMatches = Regex.Matches(associatedProductBom.IdShort, @"_(\d+)$");
            if (counterMatches.Count > 0)
            {
                idShort += counterMatches[0].Value;
            }

            var manufacturingBom = CreateBomSubmodel(idShort, iriTemplate, aas: aas, env: env, supplementarySemanticId: SEM_ID_MANUFACTURING_BOM_SM);
            var entryNodeInManufacturingBom = manufacturingBom.FindEntryNode();

            CreateVecRelationship(entryNodeInManufacturingBom, vecReference, env, vecReference.GetParentSubmodel());

            return manufacturingBom;
        }

        public static ISubmodel FindManufacturingBom(ISubmodel associatedProductBom, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            var manufacturingBoms = FindAllSubmodels(env, aas).Where(IsManufacturingBom);

            return manufacturingBoms.FirstOrDefault(sm => sm.IsAssociatedWithProductBom(associatedProductBom, env));
        }

        public static bool IsAssociatedWithProductBom(this ISubmodel manufacturingBom, ISubmodel associatedProductBom, AasCore.Aas3_0.Environment env)
        {
            var subassemblies = manufacturingBom.FindEntryNode()?.GetChildEntities() ?? new List<IEntity>();

            return subassemblies.Any(sa => sa.GetChildEntities().Any(c => c.GetSameAsEntity(env, associatedProductBom) != null));
        }

        public static ISubmodel FindProductBom(IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            return FindAllSubmodels(env, aas).FirstOrDefault(IsProductBom);
        }

        public static bool IsProductBom(this ISubmodel submodel)
        {
            return submodel.HasSemanticId(KeyTypes.Submodel, SEM_ID_PRODUCT_BOM_SM);
        }

        public static ISubmodel FindManufacturingBom(IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            return FindAllSubmodels(env, aas).FirstOrDefault(IsManufacturingBom);
        }

        public static bool IsManufacturingBom(this ISubmodel submodel)
        {
            return submodel?.HasSemanticId(KeyTypes.Submodel, SEM_ID_MANUFACTURING_BOM_SM) ?? false;
        }

        public static bool IsConfigurationBom(this ISubmodel submodel)
        {
            return submodel?.HasSemanticId(KeyTypes.Submodel, SEM_ID_CONFIGURATION_BOM_SM) ?? false;
        }

        public static bool RepresentsSubAssembly(IEntity entity)
        {
            var parentSubmodel = entity?.FindParentFirstIdentifiable() as ISubmodel;
            return IsManufacturingBom(parentSubmodel);
        }

        public static bool RepresentsBasicComponent(IEntity entity)
        {
            var parentSubmodel = entity?.FindParentFirstIdentifiable() as ISubmodel;
            return IsProductBom(parentSubmodel);
        }

        public static bool RepresentsConfiguration(IEntity entity)
        {
            var parentSubmodel = entity?.FindParentFirstIdentifiable() as ISubmodel;
            return IsConfigurationBom(parentSubmodel);
        }

        public static RelationshipElement AssociateSubassemblyWithConfiguration(IEntity subassembly, IEntity configuration)
        {
            return CreateHasPartRelationship(configuration, subassembly);
        }

        public static bool HasAssociatedSubassemblies(IEntity configuration)
        {
            return configuration.GetHasPartRelationships().Count() > 0;
        }

        public static IEnumerable<IEntity> FindAssociatedSubassemblies(IEntity configuration, AasCore.Aas3_0.Environment env)
        {
            var relationshipsToAssociatedSubassemblies = configuration?.GetHasPartRelationships();
            return relationshipsToAssociatedSubassemblies?.Select(r => env.FindReferableByReference(r.Second) as IEntity).ToList() ?? new List<IEntity>();
        }
    }
}
