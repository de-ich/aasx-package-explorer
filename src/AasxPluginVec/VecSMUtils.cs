using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using static AdminShellNS.AdminShellV20;
using static AasxPluginVec.BomSMUtils;
using System.Xml.Linq;

namespace AasxPluginVec
{
    public class VecSMUtils
    {
        public const string VEC_SUBMODEL_ID_SHORT = "VEC";
        public const string VEC_FILE_ID_SHORT = "VEC";
        public const string VEC_REFERENCE_ID_SHORT = "VEC_Reference";
        public const string SEM_ID_VEC_FILE_REFERENCE = "http://arena2036.de/vws4ls/vec/VecFileReference/1/0";
        public const string SEM_ID_VEC_FRAGMENT_REFERENCE = "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0";

        public static Submodel CreateVecSubmodel(string iri, string pathToVecFile, AdminShellPackageEnv packageEnv)
        {
            // add the file to the package
            var localFilePath = "/aasx/files/" + System.IO.Path.GetFileName(pathToVecFile);
            packageEnv.AddSupplementaryFileToStore(pathToVecFile, localFilePath, false);

            // create the VEC submodel
            var vecSubmodel = new Submodel();
            vecSubmodel.SetIdentification(Identification.IRI, iri, VEC_SUBMODEL_ID_SHORT);
            vecSubmodel.semanticId = new SemanticId(new Key("Submodel", true, "IRI", SEM_ID_VEC_FILE_REFERENCE));

            // create the VEC file submodel element
            var file = new File();
            file.idShort = VEC_FILE_ID_SHORT;
            file.mimeType = "text/xml";
            file.value = localFilePath;
            vecSubmodel.AddChild(new SubmodelElementWrapper(file));

            return vecSubmodel;
        }

        public static RelationshipElement CreateVecRelationship(Entity source, string xpathToVecElement, File vecFileSubmodelElement)
        {

            var idShort = VEC_REFERENCE_ID_SHORT;
            var semanticId = new SemanticId(new Key("ConceptDescription", false, "IRI", SEM_ID_VEC_FRAGMENT_REFERENCE));

            var second = vecFileSubmodelElement.GetReference();
            second.Keys.Add(new Key("FragmentReference", true, "FragmentId", xpathToVecElement));

            return CreateRelationship(source.GetReference(), second, source, idShort, semanticId);
        }
    }
}
