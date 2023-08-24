﻿/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AasxIntegrationBase;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVec.BasicAasUtils;
using static AasxPluginVec.CapabilitySMUtils;
using Namotion.Reflection;
using AasxPackageLogic;

namespace AasxPluginVec
{
    /// <summary>
    /// This class allows to find one or more ressource able to fulfil a required capability.
    /// </summary>
    public class CapabilityMatcher
    {
        public CapabilityMatcher(
            AasCore.Aas3_0.Environment env,
            VecOptions options)
        {
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected AasCore.Aas3_0.Environment env;
        protected VecOptions options;

        public void ValidateSelection(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count() == 0)
            {
                throw new ArgumentException("Nothing selected!");
            } else if (selectedElements.Count() == 1)
            {
                if (selectedElements.First() is not ISubmodelElementCollection)
                {
                    throw new ArgumentException("Invalid selection: An SMC representing a CapabilityContainer needs to be selected!");
                }

                if (!(selectedElements.First() as ISubmodelElementCollection).IsCapabilityContainer())
                {
                    throw new ArgumentException("Invalid selection: An SMC representing a CapabilityContainer needs to be selected!");
                }
            } else
            {
                throw new ArgumentException("Invalid selection: Only a single element may be selected!");
            }
        }

        public CapabiltyCheckResult FindRessourceForRequiredCapability(ISubmodelElementCollection requiredCapabilityContainer)
        {
            var result = new CapabiltyCheckResult()
            {
                RequiredCapabilityContainer = requiredCapabilityContainer                
            };

            var offeredCapabilityContainers = FindOfferedCapabilitiesWithSemanticId(result.RequiredCapabilitySemId);

            if(!offeredCapabilityContainers.Any())
            {
                return result;
            }

            var requiredProperties = requiredCapabilityContainer.FindPropertySet()?.FindProperties();

            foreach(var (aas, offeredCapabilityContainer) in offeredCapabilityContainers)
            {
                var offeredCapabilityResult = new OfferedCapabilityResult()
                {
                    RessourceAas = aas,
                    OfferedCapabilityContainer = offeredCapabilityContainer
                };

                foreach(var requiredProperty in requiredProperties)
                {
                    var propertyResult = CheckProperty(requiredProperty, offeredCapabilityContainer);
                    offeredCapabilityResult.PropertyMatchResults[requiredProperty] = propertyResult;
                }

                var dependencyTree = FindRequiredRessourcesRecursively(aas);
                offeredCapabilityResult.DependencyTree = dependencyTree;

                result.OfferedCapabilityResults.Add(offeredCapabilityResult);
            }

            return result;
        }

        protected IEnumerable<Tuple<IAssetAdministrationShell, ISubmodelElementCollection>> FindOfferedCapabilitiesWithSemanticId(string capabilitySemId)
        {
            return env.AssetAdministrationShells.SelectMany(aas => FindOfferedCapabilitiesWithSemanticId(capabilitySemId, aas));
        }

        protected IEnumerable<Tuple<IAssetAdministrationShell, ISubmodelElementCollection>> FindOfferedCapabilitiesWithSemanticId(string capabilitySemId, IAssetAdministrationShell aas)
        {
            var offeredCapabilitySubmodels = FindAllSubmodels(env, aas).Where(sm => sm.IsOfferedCapabilitySubmodel());

            var suitableCapabilityContainers = offeredCapabilitySubmodels.SelectMany(sm => FindOfferedCapabilitiesWithSemanticId(capabilitySemId, sm));

            return suitableCapabilityContainers.Select(cap => new Tuple<IAssetAdministrationShell, ISubmodelElementCollection>(aas, cap));
        }

        protected static IEnumerable<ISubmodelElementCollection> FindOfferedCapabilitiesWithSemanticId(string capabilitySemId, ISubmodel capabilitySubmodel)
        {
            var capabilityContainers = capabilitySubmodel.FindCapabilityContainers();

            foreach (var capabilityContainer in capabilityContainers.Where(cap => cap.GetCapabilitySemanticId() == capabilitySemId))
            {
                yield return capabilityContainer;
            }
        }
    
        protected PropertyMatchResult CheckProperty(IProperty requiredProperty, ISubmodelElementCollection offeredCapabilityContainer)
        {
            var offeredPropertySet = offeredCapabilityContainer.FindPropertySet();

            if (offeredPropertySet == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.NotFound
                };
            }

            var offeredProperty = offeredPropertySet.FindProperty(requiredProperty.IdShort);

            if (offeredProperty == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.NotFound
                };
            }

            var capabilityRelationships = offeredCapabilityContainer.FindCapabilityRelationships();

            if (capabilityRelationships == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundAndNotConstrained
                };
            }

            var constraint = FindConstraintForProperty(capabilityRelationships, offeredProperty, env);

            if (constraint == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundAndNotConstrained
                };
            }

            var constraintResult = CheckConstraint(constraint, requiredProperty);

            if(constraintResult)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundAndSatisfied
                };
            } else
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundButNotSatisfied
                };
            }
        }

        protected static bool CheckConstraint(ISubmodelElement constraint, IProperty property)
        {
            var propertyValue = property.Value;

            if (constraint is IRange rangeConstraint)
            {
                var propertyValueAsDouble = ParseDouble(propertyValue);
                return (rangeConstraint.Min == null || ParseDouble(rangeConstraint.Min) <= propertyValueAsDouble) && 
                    (rangeConstraint.Max == null || ParseDouble(rangeConstraint.Max) >= propertyValueAsDouble);
            }

            throw new ApplicationException($"Unsupported type of submodel element encountered as constraint: {constraint.GetType().Name}");
        }

        protected static Double ParseDouble(string value)
        {
            // hacky solution as xs:doubles should (!) always be represented with a '.' separator
            return Double.Parse(value.Replace(".", ","));
        }

        protected RessourceDependencyTree FindRequiredRessourcesRecursively(IAssetAdministrationShell ressourceAas)
        {
            var dependencyTree = new RessourceDependencyTree(ressourceAas);
        
            var requiredSlotSpecificAssetId = GetRequiredSlotAssetId(ressourceAas);

            if (requiredSlotSpecificAssetId == null)
            {
                return dependencyTree;
            }

            var slotName = requiredSlotSpecificAssetId.Value;
            var slotSubject = requiredSlotSpecificAssetId.ExternalSubjectId;

            dependencyTree.SlotDependencies[slotName] = new DependencyOptions();

            foreach (var aas in env.AssetAdministrationShells)
            {
                var offeredSlotSpecificAssetId = GetOfferedSlotAssetId(aas);

                if (offeredSlotSpecificAssetId == null || 
                    offeredSlotSpecificAssetId.Value != slotName || 
                    !offeredSlotSpecificAssetId.ExternalSubjectId.Matches(slotSubject))
                {
                    // aas/ressource does not provide a suitable slot
                    continue;
                }

                dependencyTree.SlotDependencies[slotName].Options.Add(FindRequiredRessourcesRecursively(aas));
            }

            return dependencyTree;

        }

        protected ISpecificAssetId? GetRequiredSlotAssetId(IAssetAdministrationShell aas)
        {
            return aas.AssetInformation.OverSpecificAssetIdsOrEmpty().FirstOrDefault(id => id.HasSemanticId(KeyTypes.GlobalReference, "requiredSlot"));
        }

        protected ISpecificAssetId? GetOfferedSlotAssetId(IAssetAdministrationShell aas)
        {
            return aas.AssetInformation.OverSpecificAssetIdsOrEmpty().FirstOrDefault(id => id.HasSemanticId(KeyTypes.GlobalReference, "offeredSlot"));
        }
    }

    public class CapabiltyCheckResult
    {
        public ISubmodelElementCollection RequiredCapabilityContainer { get; internal set; }
        public string RequiredCapabilitySemId => RequiredCapabilityContainer.GetCapabilitySemanticId();
        public bool Success => OfferedCapabilitySuccessResults.Any();
        public List<OfferedCapabilityResult> OfferedCapabilityResults { get; } = new List<OfferedCapabilityResult>();
        public IEnumerable<OfferedCapabilityResult> OfferedCapabilitySuccessResults => OfferedCapabilityResults.Where(r => r.Success);
    }

    public class OfferedCapabilityResult
    {
        public bool Success => PropertyMatchResults.All(r => r.Value.Success) && (DependencyTree?.CanBeFulfilled ?? false);
        public IAssetAdministrationShell RessourceAas { get; internal set; }
        public string RessourceAssetId => RessourceAas.AssetInformation?.GlobalAssetId;
        public ISubmodelElementCollection OfferedCapabilityContainer { get; internal set; }
        public IDictionary<IProperty, PropertyMatchResult> PropertyMatchResults { get; } = new Dictionary<IProperty, PropertyMatchResult>();
        public RessourceDependencyTree DependencyTree { get; internal set; }
    }

    public class PropertyMatchResult
    {
        public bool Success => DetailResult != PropertyMatchResultType.FoundButNotSatisfied;
        public PropertyMatchResultType DetailResult { get; internal set; }
    }

    public enum PropertyMatchResultType
    {
        FoundAndNotConstrained, FoundAndSatisfied, FoundButNotSatisfied, NotFound
    }

    public class RessourceDependencyTree
    {
        public IAssetAdministrationShell Ressource { get; set; }
        public Dictionary<string, DependencyOptions> SlotDependencies { get; } = new Dictionary<string, DependencyOptions>();
        public bool CanBeFulfilled => SlotDependencies.Values.All(o => o.CanBeFulfilled);

        public RessourceDependencyTree(IAssetAdministrationShell ressource)
        {
            Ressource = ressource;
        }
    }

    public class DependencyOptions
    {
        public List<RessourceDependencyTree> Options { get; } = new List<RessourceDependencyTree>();
        public bool CanBeFulfilled => Options.Any() && Options.Any(rdt => rdt.CanBeFulfilled);
    }
}
