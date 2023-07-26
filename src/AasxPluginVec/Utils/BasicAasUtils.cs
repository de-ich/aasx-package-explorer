using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using System.Xml.Linq;

namespace AasxPluginVec
{
    public static class BasicAasUtils
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

        public static AssetAdministrationShell CreateAAS(string aasIdShort, string aasIriTemplate, string assetIriTemplate, AasCore.Aas3_0.Environment env, AssetKind assetKind = AssetKind.Instance)
        {
            var assetInformation = new AssetInformation(assetKind, GenerateIdAccordingTemplate(assetIriTemplate));

            var aas = new AssetAdministrationShell(
                GenerateIdAccordingTemplate(aasIriTemplate),
                assetInformation,
                idShort: aasIdShort);
            
            env.AssetAdministrationShells.Add(aas);

            return aas;
        }

        public static T FindReferencedElementInSubmodel<T>(ISubmodel submodel, IReference elementReference) where T : ISubmodelElement
        {
            if (submodel == null || submodel.ToKey() == null || elementReference == null || elementReference.Keys == null || elementReference.Keys.IsEmpty())
            {
                return default(T);
            }

            if (!submodel.ToKey().Matches(elementReference.Keys.First())) {
                return default(T);
            }

            return submodel.SubmodelElements.FindDeep<T>(e => e.GetReference().Matches(elementReference)).FirstOrDefault();
        }

        public static Submodel CreateSubmodel(string idShort, string iriTemplate, string semanticId = null, IAssetAdministrationShell aas = null, AasCore.Aas3_0.Environment env = null, string supplementarySemanticId = null)
        {
            var iri = GenerateIdAccordingTemplate(iriTemplate);

            var submodel = new Submodel(iri, idShort: idShort);

            if (semanticId != null)
            {
                submodel.SemanticId = CreateSemanticId(KeyTypes.Submodel, semanticId);
            }

            if (supplementarySemanticId != null)
            {
                submodel.SupplementalSemanticIds = new List<IReference>() {
                    CreateSemanticId(KeyTypes.Submodel, supplementarySemanticId)
                };
            }

            if (env != null)
            {
                env.Submodels.Add(submodel);
            }

            if (aas != null)
            {
                aas.AddSubmodelReference(submodel.GetReference());
            }

            return submodel;
        }

        public static IReference CreateSemanticId(KeyTypes keyType, string value)
        {
            return new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(keyType, value) });
        }

        public static bool HasSemanticId(this IHasSemantics element, KeyTypes keyType, string value)
        {
            var requestedSemanticId = CreateSemanticId(keyType, value);

            // check the main semantic id
            if (requestedSemanticId.Matches(element.SemanticId))
            {
                return true;
            }

            // check the supplementary semanticids
            return element.OverSupplementalSemanticIdsOrEmpty().Any(semId => requestedSemanticId.Matches(semId));
        }

        public static IEnumerable<ISubmodel> FindAllSubmodels(AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas = null)
        {
            if (aas == null)
            {
                return env.Submodels;
            } else
            {
                var submodelRefs = aas?.Submodels ?? new List<IReference>();
                var submodels = submodelRefs.ToList().Select(smRef => env?.Submodels.Find(sm => sm.GetReference().Matches(smRef)));
                return submodels;
            }
        }

        public static HashSet<Submodel> FindCommonSubmodelParents(IEnumerable<ISubmodelElement> elements)
        {
            return elements.Select(e => e.FindParentFirstIdentifiable() as Submodel).ToHashSet();
        }

        public static Submodel FindCommonSubmodelParent(IEnumerable<ISubmodelElement> elements)
        {
            var submodel = elements.First().FindParentFirstIdentifiable() as Submodel;
            submodel.SetAllParents();
            
            if (elements.Any(e => e.FindParentFirstIdentifiable() != submodel))
            {
                return null;
            }

            return submodel;
        }

        public static IAssetAdministrationShell GetAasContainingElements(IEnumerable<ISubmodelElement> elements, AasCore.Aas3_0.Environment env)
        {
            var submodelReferences = new HashSet<Reference>(elements.Select(e => e.GetParentSubmodel().GetReference() as Reference));
            var aas = env.AssetAdministrationShells.FirstOrDefault(aas => submodelReferences.All(r => aas.HasSubmodelReference(r)));
            return aas;
        }
    }
}
