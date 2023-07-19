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
            var vecReference = FindEntryNode(associatedBomSubmodel)?.FindFirstIdShortAs< RelationshipElement>(VEC_REFERENCE_ID_SHORT);
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
            var entryNode = FindEntryNode(buildingBlocksSubmodel);
            entryNode.AddChild(vecReference);

            return buildingBlocksSubmodel;
        }

        public static ISubmodel FindBuildingBlocksSubmodel(IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            return FindAllSubmodels(aas, env).FirstOrDefault(IsBuildingBlocksSubmodel);
        }

        public static bool IsBuildingBlocksSubmodel(ISubmodel submodel)
        {
            var entryNode = FindEntryNode(submodel);
            var entities = entryNode?.EnumerateChildren().Where(c => c is Entity).Select(c => c as Entity) ?? new List<Entity>();
            return entities?.Any(RepresentsSubAssembly) ?? false;
        }

        public static bool RepresentsSubAssembly(IEntity entity)
        {
            var parentSubmodel = entity.FindParentFirstIdentifiable();
            var sameAsRelationships = GetSameAsRelationships(entity);
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

            var hasPartRelationships = GetHasPartRelationships(entity);
            return hasPartRelationships.Count > 0;
        }

        public static bool RepresentsBasicComponent(Entity entity)
        {
            var parentSubmodel = entity.FindParentFirstIdentifiable();
            var sameAsRelationships = GetSameAsRelationships(entity);

            if (GetVecRelationship(entity) == null)
            {
                return false;
            }

            var hasPartRelationships = GetHasPartRelationships(entity);
            return hasPartRelationships.Count == 0;
        }

        public static RelationshipElement AssociateSubassemblyWithModule(Entity subassembly, Entity orderableModule)
        {
            return CreateHasPartRelationship(orderableModule, subassembly);
        }

        public static bool HasAssociatedSubassemblies(IEntity orderableModule)
        {
            return GetHasPartRelationships(orderableModule).Count() > 0;
        }

        public static List<Entity> FindAssociatedSubassemblies(IEntity orderableModule, AasCore.Aas3_0.Environment env)
        {
            var relationshipsToAssociatedSubassemblies = GetHasPartRelationships(orderableModule);
            return relationshipsToAssociatedSubassemblies.Select(r => env.FindReferableByReference(r.Second) as Entity).ToList();
        }
    }
}
