using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using static AasxPluginVec.BomSMUtils;
using System.Xml.Linq;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVec.BasicAasUtils;

namespace AasxPluginVec
{
    public class VecSMUtils
    {
        public const string VEC_SUBMODEL_ID_SHORT = "VEC";
        public const string VEC_FILE_ID_SHORT = "VecFile";
        public const string VEC_REFERENCE_ID_SHORT = "SameAs";
        public const string SEM_ID_VEC_SUBMODEL = "http://arena2036.de/vws4ls/vec/VecSubmodel/1/0";
        public const string SEM_ID_VEC_FILE_REFERENCE = "http://arena2036.de/vws4ls/vec/VecFileReference/1/0";
        public const string SEM_ID_VEC_FRAGMENT_REFERENCE = "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0";

        public static Submodel CreateVecSubmodel(string pathToVecFile, string iriTemplate, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env, AdminShellPackageEnv packageEnv = null)
        {
            // add the file to the package
            var localFilePath = "/aasx/files/" + System.IO.Path.GetFileName(pathToVecFile);
            packageEnv?.AddSupplementaryFileToStore(pathToVecFile, localFilePath, false);

            // create the VEC file submodel element
            var file = new File("text/xml", idShort: VEC_FILE_ID_SHORT, value: localFilePath);

            // create the VEC submodel
            var vecSubmodel = CreateVecSubmodel(file, iriTemplate, aas, env);

            return vecSubmodel;
        }

        public static Submodel CreateVecSubmodel(File vecFile, string iriTemplate, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            // create the VEC submodel
            var vecSubmodel = CreateSubmodel(VEC_SUBMODEL_ID_SHORT, iriTemplate, SEM_ID_VEC_SUBMODEL, aas, env);

            // create the VEC file submodel element
            var file = new File(vecFile.ContentType, value: vecFile.Value, idShort: vecFile.IdShort)
            {
                SemanticId = CreateSemanticId(KeyTypes.Submodel, SEM_ID_VEC_FILE_REFERENCE)
            };
            vecSubmodel.Add(file);

            return vecSubmodel;
        }

        public static File GetVecFileElement(Submodel submodel)
        {
            return submodel?.FindFirstIdShortAs<File>(VEC_FILE_ID_SHORT);
        }

        public static RelationshipElement CreateVecRelationship(IEntity source, string xpathToVecElement, File vecFileSubmodelElement, IReferable parent = null)
        {

            var idShort = VEC_REFERENCE_ID_SHORT + "_" + source.GetParentSubmodel().IdShort + "_" + source.IdShort;
            var semanticId = CreateSemanticId(KeyTypes.ConceptDescription, SEM_ID_VEC_FRAGMENT_REFERENCE);

            var first = vecFileSubmodelElement.GetReference();
            first.Keys.Add(new Key(KeyTypes.FragmentReference, xpathToVecElement));

            return CreateRelationship(first, source.GetReference(), parent ?? source, idShort, semanticId);
        }

        public static RelationshipElement GetVecRelationship(IEntity entity)
        {
            var rel = (entity as Entity)?.FindFirstIdShortAs<RelationshipElement>(VEC_REFERENCE_ID_SHORT);

            return IsVecRelationship(rel) ? rel : null;
        }

        public static bool IsVecRelationship(RelationshipElement rel)
        {
            return rel?.IdShort == VEC_REFERENCE_ID_SHORT && rel?.SemanticId?.Last()?.Value == SEM_ID_VEC_FRAGMENT_REFERENCE;
        }

        public static File FindReferencedVecFileSME(Entity entityWithVecRelationship, AasCore.Aas3_0.Environment env)
        {
            var entryNodeVecRelationship = GetVecRelationship(entityWithVecRelationship);
            var fragmentReferenceKeys = entryNodeVecRelationship?.Second?.Keys;
            var keysToVecFile = fragmentReferenceKeys?.Take(fragmentReferenceKeys.ToList().Count - 1);
            var referenceToVecFile = new Reference(ReferenceTypes.ModelReference, keysToVecFile?.ToList() ?? new List<IKey>());
            return env.FindReferableByReference(referenceToVecFile) as File;
        }
    }
}
