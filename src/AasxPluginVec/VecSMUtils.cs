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
        public const string VEC_FILE_ID_SHORT = "VEC";
        public const string VEC_REFERENCE_ID_SHORT = "VEC_Reference";
        public const string SEM_ID_VEC_SUBMODEL = "http://arena2036.de/vws4ls/vec/VecSubmodel/1/0";
        public const string SEM_ID_VEC_FILE_REFERENCE = "http://arena2036.de/vws4ls/vec/VecFileReference/1/0";
        public const string SEM_ID_VEC_FRAGMENT_REFERENCE = "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0";

        public static Submodel CreateVecSubmodel(string iri, string pathToVecFile, AdminShellPackageEnv packageEnv)
        {
            // add the file to the package
            var localFilePath = "/aasx/files/" + System.IO.Path.GetFileName(pathToVecFile);
            packageEnv.AddSupplementaryFileToStore(pathToVecFile, localFilePath, false);

            // create the VEC file submodel element
            var file = new File("text/xml", idShort: VEC_FILE_ID_SHORT, value: localFilePath);

            // create the VEC submodel
            var vecSubmodel = CreateVecSubmodel(iri, file);

            return vecSubmodel;
        }

        public static Submodel CreateVecSubmodel(string iri, File vecFile)
        {
            // create the VEC submodel
            var vecSubmodel = new Submodel(iri, idShort: VEC_SUBMODEL_ID_SHORT)
            {
                SemanticId = CreateSemanticId(KeyTypes.Submodel, SEM_ID_VEC_SUBMODEL)
            };

            // create the VEC file submodel element
            var file = new File(vecFile.ContentType, value: vecFile.Value, idShort: vecFile.IdShort);
            file.SemanticId = CreateSemanticId(KeyTypes.Submodel, SEM_ID_VEC_FILE_REFERENCE);
            vecSubmodel.Add(file);

            return vecSubmodel;
        }

        public static File GetVecFileElement(Submodel submodel)
        {
            return submodel?.FindFirstIdShortAs<File>(VEC_FILE_ID_SHORT);
        }

        public static RelationshipElement CreateVecRelationship(Entity source, string xpathToVecElement, File vecFileSubmodelElement)
        {

            var idShort = VEC_REFERENCE_ID_SHORT;
            var semanticId = CreateSemanticId(KeyTypes.ConceptDescription, SEM_ID_VEC_FRAGMENT_REFERENCE);

            var second = vecFileSubmodelElement.GetReference();
            second.Keys.Add(new Key(KeyTypes.FragmentReference, xpathToVecElement));

            return CreateRelationship(source.GetReference(), second, source, idShort, semanticId);
        }

        public static RelationshipElement GetVecRelationship(Entity entity)
        {
            var rel = entity?.FindFirstIdShortAs<RelationshipElement>(VEC_REFERENCE_ID_SHORT);

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
