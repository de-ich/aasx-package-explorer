using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using static AdminShellNS.AdminShellV20;
using System.Xml.Linq;

namespace AasxPluginVec
{
    public class BasicAasUtils
    {
        private static Random MyRnd = new Random();

        // The version of 'GenerateIdAccordingTemplate' from 'AdminShellUtil' does not ensure unique IDs when
        // being called multiple times in rapid succession (more than two times in one ten thousandths of a second).
        // Hence, we dupliate and adapt this method to use a random time insstead of 'UTCNow' as base for id generation.
        public static string GenerateIdAccordingTemplate(string tpl)
        {
             // generate a deterministic decimal digit string
             var decimals = String.Format("{0:fffffffyyMMddHHmmss}", new DateTime(MyRnd.Next(Int32.MaxValue)));
             decimals = new string(decimals.Reverse().ToArray());
             // convert this to an int
             if (!Int64.TryParse(decimals, out Int64 decii))
                decii = MyRnd.Next(Int32.MaxValue);
            
            // make an hex out of this
            string hexamals = decii.ToString("X");
            // make an alphanumeric string out of this
            string alphamals = "";
            var dii = decii;
            while (dii >= 1)
            {
                var m = dii % 26;
                alphamals += Convert.ToChar(65 + m);
                dii = dii / 26;
            }

            // now, "salt" the strings
            for (int i = 0; i < 32; i++)
            {
                var c = Convert.ToChar(48 + MyRnd.Next(10));
                decimals += c;
                hexamals += c;
                alphamals += c;
            }

            // now, can just use the template
            var id = "";
            foreach (var tpli in tpl)
            {
                if (tpli == 'D' && decimals.Length > 0)
                {
                    id += decimals[0];
                    decimals = decimals.Remove(0, 1);
                }
                else
                if (tpli == 'X' && hexamals.Length > 0)
                {
                    id += hexamals[0];
                    hexamals = hexamals.Remove(0, 1);
                }
                else
                if (tpli == 'A' && alphamals.Length > 0)
                {
                    id += alphamals[0];
                    alphamals = alphamals.Remove(0, 1);
                }
                else
                    id += tpli;
            }

            // ok
            return id;
        }

        /**
         * An adapted version of 'GetReference' from 'AdminShell.cs'. This version does not include
         * the id of the containing AAS in the reference keys because this leads to errors with
         * the 'jump' function in the package explorer
         */
        public static Reference GetReference(SubmodelElement element, bool includeParents = true)
        {
            Reference r = new Reference();
            // this is the tail of our referencing chain ..
            r.Keys.Add(Key.CreateNew(element.GetElementName(), true, "IdShort", element.idShort));
            // try to climb up ..
            var current = element.parent;
            while (includeParents && current != null)
            {
                if (current is Identifiable cid)
                {
                    // add big information set
                    r.Keys.Insert(0, Key.CreateNew(
                        current.GetElementName(),
                        true,
                        cid.identification.idType,
                        cid.identification.id));
                    break; // changed from the official version
                }
                else
                if (current is Referable crf)
                {
                    // reference via idShort
                    r.Keys.Insert(0, Key.CreateNew(
                        current.GetElementName(),
                        true,
                        "IdShort", crf.idShort));
                }

                if (current is Referable crf2)
                    current = crf2.parent;
                else
                    current = null;
            }
            return r;
        }

        public static AdministrationShell CreateAAS(string aasIdShort, string assetIdShort, string aasIriTemplate, string assetIriTemplate, AdministrationShellEnv env)
        {
            var aas = new AdministrationShell();
            aas.idShort = aasIdShort;
            aas.identification = new Identification(new Key("AssetAdministrationShell", false, "IRI", GenerateIdAccordingTemplate(aasIriTemplate)));

            var asset = new Asset();
            asset.idShort = assetIdShort;
            asset.identification = new Identification(new Key("Asset", false, "IRI", GenerateIdAccordingTemplate(assetIriTemplate)));
            aas.assetRef = asset.GetAssetReference();

            env.AdministrationShells.Add(aas);
            env.Assets.Add(asset);

            return aas;
        }

        public static T FindReferencedElementInSubmodel<T>(Submodel submodel, Reference elementReference) where T : SubmodelElement
        {
            if (submodel == null || submodel.ToKey() == null || elementReference == null || elementReference.Keys == null || elementReference.Keys.IsEmpty)
            {
                return null;
            }

            if (!submodel.ToKey().Matches(elementReference.Keys.First())) {
                return null;
            }

            return submodel.FindDeep<T>(e => GetReference(e).Matches(elementReference)).FirstOrDefault();
        }

        public static Submodel CreateSubmodel(string idShort, string iriTemplate, string semanticId = null, AdministrationShell aas = null, AdministrationShellEnv env = null)
        {
            var iri = GenerateIdAccordingTemplate(iriTemplate);

            var submodel = new Submodel();
            submodel.SetIdentification(Identification.IRI, iri, idShort);

            if (semanticId != null)
            {
                submodel.semanticId = new SemanticId(new Key("Submodel", false, "IRI", semanticId));
            }

            if (env != null)
            {
                env.Submodels.Add(submodel);
            }

            if (aas != null)
            {
                aas.AddSubmodelRef(submodel.GetSubmodelRef());
            }

            return submodel;
        }

        public static IEnumerable<Submodel> FindAllSubmodels(AdministrationShell aas, AdministrationShellEnv env)
        {
            var submodelRefs = aas?.submodelRefs ?? new List<SubmodelRef>();
            var submodels = submodelRefs.ToList().Select(smRef => env?.Submodels.Find(sm => sm.GetReference().Matches(smRef)));
            return submodels;
        }

        public static HashSet<Submodel> FindCommonSubmodelParents(IEnumerable<SubmodelElement> elements)
        {
            return elements.Select(e => e.FindParentFirstIdentifiable() as Submodel).ToHashSet();
        }

        public static Submodel FindCommonSubmodelParent(IEnumerable<SubmodelElement> elements)
        {
            var submodel = elements.First().FindParentFirstIdentifiable() as Submodel;
            submodel.SetAllParents();
            
            if (elements.Any(e => e.FindParentFirstIdentifiable() != submodel))
            {
                return null;
            }

            return submodel;
        }
    }
}
