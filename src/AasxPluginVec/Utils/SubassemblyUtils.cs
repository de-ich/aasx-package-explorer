using System;
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

    public class SubassemblyUtils
    {
        public const string ID_SHORT_COMPONENTS_SM = "LS_Product_BOM";
        public const string ID_SHORT_ORDERABLE_MODULES_SM = "LS_OrderableModules_BOM";
        public const string ID_SHORT_BUILDING_BLOCKS_SM = "LS_Manufacturing_BOM";
        public const string ID_SHORT_ORDERED_MODULES_SM = "LS_OrderedModules_BOM";

        public static Submodel CreateBuildingBlocksSubmodel(string iriTemplate, ISubmodel associatedBomSubmodel, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            var vecReference = associatedBomSubmodel.FindEntryNode()?.FindFirstIdShortAs<RelationshipElement>(VEC_REFERENCE_ID_SHORT);
            if (vecReference == null)
            {
                throw new Exception("Unable to find VEC reference in existing components BOM submodel!");
            }
            
            var idShort = ID_SHORT_BUILDING_BLOCKS_SM;

            var counterMatches = Regex.Matches(associatedBomSubmodel.IdShort, @"_(\d+)$");
            if (counterMatches.Count > 0)
            {
                idShort = idShort + counterMatches[0].Value;
            }
            var buildingBlocksSubmodel = CreateBomSubmodel(idShort, iriTemplate, aas: aas, env: env);
            var entryNode = buildingBlocksSubmodel.FindEntryNode();
            entryNode.AddChild(vecReference);

            return buildingBlocksSubmodel;
        }

        public static ISubmodel FindBuildingBlocksSubmodel(IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            return FindAllSubmodels(aas, env).FirstOrDefault(IsBuildingBlocksSubmodel);
        }

        public static bool IsBuildingBlocksSubmodel(ISubmodel submodel)
        {
            submodel.SetAllParents();
            var entryNode = submodel.FindEntryNode();
            var entities = entryNode?.GetChildEntities();
            return entities?.Any(RepresentsSubAssembly) ?? false;
        }

        public static bool IsOrderableModulesSubmodel(ISubmodel submodel)
        {
            submodel.SetAllParents();
            var entryNode = submodel.FindEntryNode();
            var entities = entryNode?.GetChildEntities();
            return entities?.Any(RepresentsOrderableModule) ?? false;
        }

        public static bool RepresentsSubAssembly(IEntity entity)
        {
            var parentSubmodel = entity.FindParentFirstIdentifiable();
            var sameAsRelationships = entity.GetSameAsRelationships();
            var hasSameAsRelationshipToOtherEntityInDifferentBOM = sameAsRelationships.Any(r =>
            {
                return r.First.Keys.Last().Type == KeyTypes.Entity && r.Second.Keys.Last().Type == KeyTypes.Entity &&
                    r.First.Keys.First().Type == KeyTypes.Submodel && r.Second.Keys.First().Type == KeyTypes.Submodel &&
                    !r.Second.Keys.First().Matches(parentSubmodel.ToKey());
            });

            if (!hasSameAsRelationshipToOtherEntityInDifferentBOM)
            {
                return false;
            }

            var hasPartRelationships = entity.GetHasPartRelationships();
            return hasPartRelationships.Count() > 0;
        }

        public static bool RepresentsBasicComponent(IEntity entity)
        {
            if (entity.EntityType == EntityType.CoManagedEntity)
            {
                return false;
            }

            if (GetVecRelationship(entity) == null)
            {
                return false;
            }

            var hasPartRelationships = entity.GetHasPartRelationships();
            return hasPartRelationships.Count() == 0;
        }

        public static bool RepresentsOrderableModule(IEntity entity)
        {
            if (entity.EntityType == EntityType.SelfManagedEntity)
            {
                return false;
            }

            if (GetVecRelationship(entity) == null)
            {
                return false;
            }

            return true;
        }

        public static RelationshipElement AssociateSubassemblyWithModule(IEntity subassembly, IEntity orderableModule)
        {
            return CreateHasPartRelationship(orderableModule, subassembly);
        }

        public static bool HasAssociatedSubassemblies(IEntity orderableModule)
        {
            return orderableModule.GetHasPartRelationships().Count() > 0;
        }

        public static List<Entity> FindAssociatedSubassemblies(IEntity orderableModule, AasCore.Aas3_0.Environment env)
        {
            var relationshipsToAssociatedSubassemblies = orderableModule.GetHasPartRelationships();
            return relationshipsToAssociatedSubassemblies.Select(r => env.FindReferableByReference(r.Second) as Entity).ToList();
        }
    }
}
