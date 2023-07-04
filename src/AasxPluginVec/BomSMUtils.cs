using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AdminShellNS.AdminShellV20;
using static AasxPluginVec.BasicAasUtils;

namespace AasxPluginVec
{
    public class BomSMUtils
    {
        public const string SEM_ID_BOM_SM = "https://admin-shell.io/idta/HierarchicalStructures/1/0/Submodel";
        public const string SEM_ID_ARCHE_TYPE = "https://admin-shell.io/idta/HierarchicalStructures/ArcheType/1/0";
        public const string SEM_ID_ENTRY_NODE = "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0";
        public const string SEM_ID_NODE = "https://admin-shell.io/idta/HierarchicalStructures/Node/1/0";
        public const string SEM_ID_HAS_PART = "https://admin-shell.io/idta/HierarchicalStructures/HasPart/1/0";
        public const string SEM_ID_SAME_AS = "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0";
        public const string ASSET_TYPE_CO_MANAGED_ENTITY = "CoManagedEntity";
        public const string ASSET_TYPE_SELF_MANAGED_ENTITY = "SelfManagedEntity";

        public static Submodel CreateBomSubmodel(string idShort, string iri, string archeType = "Full")
        {
            var bomSubmodel = new Submodel();
            bomSubmodel.SetIdentification(Identification.IRI, iri, idShort);
            bomSubmodel.semanticId = new SemanticId(new Key("Submodel", false, "IRI", SEM_ID_BOM_SM));

            var archeTypeProperty = new Property();
            archeTypeProperty.semanticId = new SemanticId(new Key("Property", false, "IRI", SEM_ID_ARCHE_TYPE));
            archeTypeProperty.idShort = "ArcheType";
            archeTypeProperty.value = archeType;
            bomSubmodel.Add(archeTypeProperty);

            return bomSubmodel;
        }

        public static Submodel FindFirstBomSubmodel(AdministrationShell aas, AdministrationShellEnv env)
        {
            var submodels = FindBomSubmodels(aas, env);
            return submodels?.First(sm => sm.semanticId.Last.value == SEM_ID_BOM_SM);
        }

        public static IEnumerable<Submodel> FindBomSubmodels(AdministrationShell aas, AdministrationShellEnv env)
        {
            var submodelRefs = aas?.submodelRefs;
            var submodels = submodelRefs?.ToList().Select(smRef => env?.Submodels.Find(sm => sm.GetReference().Matches(smRef)));
            return submodels.Where(sm => sm.semanticId?.Matches("Submodel", false, "IRI", SEM_ID_BOM_SM) ?? false) ?? new List<Submodel>();
        }

        public static Entity CreateEntryNode(Submodel parent, AssetRef referencedAsset)
        {
            var semanticId = new SemanticId(new Key("Entity", false, "IRI", SEM_ID_ENTRY_NODE));
            return CreateEntity("EntryNode", parent, referencedAsset, semanticId);
        }

        public static Entity FindEntryNode(Submodel bomSubmodel) {
            return bomSubmodel?.FindSubmodelElementWrapper("EntryNode")?.submodelElement as Entity;
        }

        public static Entity CreateNode(string idShort, IManageSubmodelElements parent, AssetRef referencedAsset = null)
        {
            var semanticId = new SemanticId(new Key("Entity", false, "IRI", SEM_ID_NODE));
            return CreateEntity(idShort, parent, referencedAsset, semanticId);
        }

        public static Entity CreateEntity(string idShort, IManageSubmodelElements parent, AssetRef referencedAsset = null, SemanticId semanticId = null)
        {
            var entity = new Entity();
            if (semanticId != null)
            {
                entity.semanticId = semanticId;
            }
            entity.idShort = idShort;
            parent.Add(entity);

            if (referencedAsset == null)
            {
                entity.entityType = ASSET_TYPE_CO_MANAGED_ENTITY;
            }
            else
            {
                entity.entityType = ASSET_TYPE_SELF_MANAGED_ENTITY;
                entity.assetRef = referencedAsset;
            }
            return entity;
        }

        public static RelationshipElement CreateHasPartRelationship(Entity first, Entity second, string relName = null)
        {
            return CreateRelationship(
                first,
                second,
                first,
                relName ?? "HasPart_" + second.idShort,
                new SemanticId(new Key("ConceptDescription", false, "IRI", SEM_ID_HAS_PART))
            );
        }

        public static RelationshipElement CreateSameAsRelationship(Entity first, Entity second, Entity parent, string relName = null)
        {
            return CreateSameAsRelationship(
                GetReference(first),
                GetReference(second),
                parent,
                relName ?? "SameAs_" + second.idShort
            );
        }

        public static RelationshipElement CreateSameAsRelationship(Reference first, Reference second, Entity parent, string relName)
        {
            return CreateRelationship(
                first,
                second,
                parent,
                relName,
                new SemanticId(new Key("ConceptDescription", false, "IRI", SEM_ID_SAME_AS))
            );
        }

        public static RelationshipElement CreateRelationship(SubmodelElement first, SubmodelElement second, IManageSubmodelElements parent, string idShort, SemanticId semanticId = null)
        {
            return CreateRelationship(GetReference(first), GetReference(second), parent, idShort, semanticId);
        }

        public static RelationshipElement CreateRelationship(Reference first, Reference second, IManageSubmodelElements parent, string idShort, SemanticId semanticId = null)
        {
            var rel = new RelationshipElement();
            rel.idShort = idShort;
            if (semanticId != null)
            {
                rel.semanticId = semanticId;
            }
            rel.first = first;
            rel.second = second;
            parent.Add(rel);

            return rel;
        }

        public static List<RelationshipElement> GetHasPartRelationships(Entity entity)
        {
            return entity?.EnumerateChildren().
               Where(c => c.submodelElement is RelationshipElement).
               Select(c => c.submodelElement as RelationshipElement).
               Where(r => r.semanticId.Matches(new Key("ConceptDescription", false, "IRI", SEM_ID_HAS_PART))).ToList() ?? new List<RelationshipElement>();
        }

        public static List<RelationshipElement> GetSameAsRelationships(Entity entity)
        {
            return entity?.EnumerateChildren().
               Where(c => c.submodelElement is RelationshipElement).
               Select(c => c.submodelElement as RelationshipElement).
               Where(r => r.semanticId.Matches(new Key("ConceptDescription", false, "IRI", SEM_ID_SAME_AS))).ToList() ?? new List<RelationshipElement>();
        }

        public static List<Entity> GetLeafNodes(Submodel submodel) {
            var entryNode = FindEntryNode(submodel);
            return entryNode?.EnumerateChildren().Select(c => c.submodelElement as Entity).Where(IsLeafNode).ToList() ?? new List<Entity>();
        }

        public static bool IsLeafNode(Entity node)
        {
            if (node == null)
            {
                return false;
            }
            return GetHasPartRelationships(node).Count() == 0;
        }
    }
}
