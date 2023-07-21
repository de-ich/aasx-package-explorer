using AasCore.Aas3_0;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AasxPluginVec.BasicAasUtils;

namespace AasxPluginVec
{
    public static class BomSMUtils
    {
        public const string SEM_ID_BOM_SM = "https://admin-shell.io/idta/HierarchicalStructures/1/0/Submodel";
        public const string SEM_ID_ARCHE_TYPE = "https://admin-shell.io/idta/HierarchicalStructures/ArcheType/1/0";
        public const string SEM_ID_ENTRY_NODE = "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0";
        public const string SEM_ID_NODE = "https://admin-shell.io/idta/HierarchicalStructures/Node/1/0";
        public const string SEM_ID_HAS_PART = "https://admin-shell.io/idta/HierarchicalStructures/HasPart/1/0";
        public const string SEM_ID_SAME_AS = "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0";
        public const string ID_SHORT_ENTRY_NODE = "EntryNode";
        public const string ID_SHORT_ARCHE_TYPE = "ArcheType";

        public static Submodel CreateBomSubmodel(string idShort, string iriTemplate, string archeType = "Full", IAssetAdministrationShell aas = null, AasCore.Aas3_0.Environment env = null)
        {
            var bomSubmodel = CreateSubmodel(idShort, iriTemplate, SEM_ID_BOM_SM, aas, env);
            
            var archeTypeProperty = new Property(DataTypeDefXsd.String, idShort: ID_SHORT_ARCHE_TYPE);
            archeTypeProperty.SemanticId = CreateSemanticId(KeyTypes.Property, SEM_ID_ARCHE_TYPE);
            archeTypeProperty.Value = archeType;
            bomSubmodel.Add(archeTypeProperty);

            CreateEntryNode(bomSubmodel, aas?.AssetInformation.GlobalAssetId);

            return bomSubmodel;
        }

        public static ISubmodel FindFirstBomSubmodel(IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            var submodels = FindBomSubmodels(aas, env);
            return submodels?.FirstOrDefault(sm => sm.SemanticId.Last().Value == SEM_ID_BOM_SM);
        }

        public static IEnumerable<ISubmodel> FindBomSubmodels(IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            var submodels = FindAllSubmodels(aas, env);
            return submodels.Where(sm => sm.SemanticId?.Matches(KeyTypes.Submodel, SEM_ID_BOM_SM) ?? false);
        }

        public static Entity CreateEntryNode(this ISubmodel parent, string referencedAsset)
        {
            var semanticId = CreateSemanticId(KeyTypes.Entity, SEM_ID_ENTRY_NODE);
            return CreateEntity(ID_SHORT_ENTRY_NODE, parent, referencedAsset, semanticId);
        }

        public static Entity FindEntryNode(this ISubmodel bomSubmodel) {
            return bomSubmodel?.FindSubmodelElementByIdShort(ID_SHORT_ENTRY_NODE) as Entity;
        }

        public static Entity CreateNode(string idShort, IEntity parent, string referencedAsset = null)
        {
            var semanticId = CreateSemanticId(KeyTypes.Entity, SEM_ID_NODE);
            return CreateEntity(idShort, parent, referencedAsset, semanticId);
        }

        public static Entity CreateEntity(string idShort, IEntity parent, string referencedAsset = null, IReference semanticId = null)
        {
            var entity = new Entity(
                referencedAsset == null ? EntityType.CoManagedEntity : EntityType.SelfManagedEntity,
                semanticId: semanticId,
                idShort: idShort,
                globalAssetId: referencedAsset);
            
            parent.Add(entity);
            return entity;
        }

        public static Entity CreateEntity(string idShort, ISubmodel parent, string referencedAsset = null, IReference semanticId = null)
        {
            var entity = new Entity(
                referencedAsset == null ? EntityType.CoManagedEntity : EntityType.SelfManagedEntity,
                semanticId: semanticId,
                idShort: idShort,
                globalAssetId: referencedAsset);

            parent.Add(entity);
            return entity;
        }

        public static RelationshipElement CreateHasPartRelationship(IEntity first, IEntity second, string relName = null)
        {
            return CreateRelationship(
                first,
                second,
                first,
                relName ?? "HasPart_" + second.IdShort,
                CreateSemanticId(KeyTypes.ConceptDescription, SEM_ID_HAS_PART)
            );
        }

        public static bool IsHasPartRelationship(this RelationshipElement rel)
        {
            return rel?.SemanticId?.Matches(KeyTypes.ConceptDescription, SEM_ID_HAS_PART) ?? false;
        }

        public static RelationshipElement CreateSameAsRelationship(IEntity first, IEntity second, IEntity parent, string relName = null)
        {
            return CreateSameAsRelationship(
                first.GetReference(),
                second.GetReference(),
                parent,
                relName ?? "SameAs_" + second.IdShort
            );
        }

        public static RelationshipElement CreateSameAsRelationship(IReference first, IReference second, IEntity parent, string relName)
        {
            return CreateRelationship(
                first,
                second,
                parent,
                relName,
                CreateSemanticId(KeyTypes.ConceptDescription, SEM_ID_SAME_AS)
            );
        }

        public static bool IsSameAsRelationship(this RelationshipElement rel)
        {
            return rel?.SemanticId?.Matches(KeyTypes.ConceptDescription, SEM_ID_SAME_AS) ?? false;
        }

        public static RelationshipElement CreateRelationship(ISubmodelElement first, ISubmodelElement second, IEntity parent, string idShort, IReference semanticId = null)
        {
            return CreateRelationship(first.GetReference(), second.GetReference(), parent, idShort, semanticId);
        }

        public static RelationshipElement CreateRelationship(IReference first, IReference second, IEntity parent, string idShort, IReference semanticId = null)
        {
            var rel = new RelationshipElement(first, second, idShort: idShort, semanticId: semanticId);
            parent.Add(rel);

            return rel;
        }

        public static IEnumerable<IRelationshipElement> GetHasPartRelationships(this IEntity entity)
        {
            return entity.GetChildRelationships().Where(r => r.SemanticId.Matches(KeyTypes.ConceptDescription, SEM_ID_HAS_PART)).ToList();
        }

        public static IEnumerable<IRelationshipElement> GetSameAsRelationships(this IEntity entity)
        {
            return entity.GetChildRelationships().Where(r => r.SemanticId.Matches(KeyTypes.ConceptDescription, SEM_ID_SAME_AS)).ToList();
        }

        public static IEnumerable<IEntity> GetLeafNodes(this ISubmodel submodel) {
            var entryNode = submodel.FindEntryNode();
            return entryNode?.GetChildEntities().Where(IsLeafNode).ToList() ?? new List<IEntity>();
        }

        public static bool IsLeafNode(this IEntity node)
        {
            if (node == null)
            {
                return false;
            }
            return GetHasPartRelationships(node).Count() == 0;
        }

        public static IEnumerable<IEntity> GetChildEntities(this IEntity entity)
        {
            return entity?.EnumerateChildren().Where(c => c is IEntity).Select(c => c as IEntity) ?? new List<IEntity>();
        }

        public static IEnumerable<IRelationshipElement> GetChildRelationships(this IEntity entity)
        {
            return entity?.EnumerateChildren().Where(c => c is IRelationshipElement).Select(c => c as IRelationshipElement) ?? new List<IRelationshipElement>();
        }
    }
}
