using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using static AdminShellNS.AdminShellV20;
using static AasxPluginVec.BomSMUtils;
using static AasxPluginVec.VecSMUtils;
using static AasxPluginVec.BasicAasUtils;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace AasxPluginVec
{

    public class SubassemblyUtils
    {
        public const string ID_SHORT_LS_BOM_SM = "LS_BOM";
        public const string ID_SHORT_COMPONENTS_SM = "LS_BOM_Components";
        public const string ID_SHORT_ORDERABLE_MODULES_SM = "LS_BOM_Modules";
        public const string ID_SHORT_BUILDING_BLOCKS_SM = "LS_BOM_BuildingBlocks";
        public const string ID_SHORT_ORDERED_MODULES_SM = "LS_BOM_OrderedModules";

        public static Submodel CreateBuildingBlocksSubmodel(string iriTemplate, Submodel associatedBomSubmodel, AdministrationShell aas, AdministrationShellEnv env)
        {
            var vecReference = FindEntryNode(associatedBomSubmodel)?.FindSubmodelElementWrapper(VEC_REFERENCE_ID_SHORT)?.submodelElement as RelationshipElement;
            if (vecReference == null)
            {
                throw new Exception("Unable to find VEC reference in existing components BOM submodel!");
            }
            
            var idShort = ID_SHORT_BUILDING_BLOCKS_SM;

            var counterMatches = Regex.Matches(associatedBomSubmodel.idShort, @"_(\d+)$");
            if (counterMatches.Count > 0)
            {
                idShort = idShort + counterMatches[0].Value;
            }
            var buildingBlocksSubmodel = CreateBomSubmodel(idShort, iriTemplate, aas: aas, env: env);
            var entryNode = FindEntryNode(buildingBlocksSubmodel);
            entryNode.AddChild(new SubmodelElementWrapper(vecReference));

            return buildingBlocksSubmodel;
        }

        public static Submodel FindBuildingBlocksSubmodel(AdministrationShell aas, AdministrationShellEnv env)
        {
            return FindAllSubmodels(aas, env).FirstOrDefault(IsBuildingBlocksSubmodel);
        }

        public static bool IsBuildingBlocksSubmodel(Submodel submodel)
        {
            var entryNode = FindEntryNode(submodel);
            var entities = entryNode?.EnumerateChildren().Select(c => c.submodelElement).Where(c => c is Entity).Select(c => c as Entity) ?? new List<Entity>();
            return entities?.Any(RepresentsSubAssembly) ?? false;
        }

        public static bool RepresentsSubAssembly(Entity entity)
        {
            var parentSubmodel = entity.FindParentFirstIdentifiable();
            var sameAsRelationships = GetSameAsRelationships(entity);
            var hasSameAsRelationshipToOtherEntityInDifferentBOM = sameAsRelationships.Any(r =>
            {
                return r.first.Keys.Last().type == "Entity" && r.second.Keys.Last().type == "Entity" &&
                    r.first.Keys.First().type == "Submodel" && r.second.Keys.First().type == "Submodel" &&
                    !r.second.Keys.First().Matches(parentSubmodel.ToKey());
            });

            if (!hasSameAsRelationshipToOtherEntityInDifferentBOM)
            {
                return false;
            }

            var hasPartRelationships = GetHasPartRelationships(entity);
            return hasPartRelationships.Count > 0;
        }

        public static bool RepresentsBasciComponent(Entity entity)
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
            return CreateHasPartRelationship(orderableModule, subassembly, "Production_Requires_" + subassembly.idShort);
        }

        public static bool HasAssociatedSubassemblies(Entity orderableModule)
        {
            return GetHasPartRelationships(orderableModule).Any(r => r.idShort.StartsWith("Production_Requires_"));
        }

        public static List<Entity> FindAssociatedSubassemblies(Entity orderableModule, AdministrationShellEnv env)
        {
            var relationshipsToAssociatedSubassemblies = GetHasPartRelationships(orderableModule).Where(r => r.idShort.StartsWith("Production_Requires_"));
            return relationshipsToAssociatedSubassemblies.Select(r => env.FindReferableByReference(r.second) as Entity).ToList();
        }
    }
}
