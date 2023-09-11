using AasCore.Aas3_0;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginAml.Utils;

public static class AmlSMUtils
{
    public const string SEM_ID_AML_SM = "https://automationml.org/aas/1/0/AmlFile";
    public const string AML_FRAGMENT_REF_PREFIX = "AML/";

    public const string IDSHORT_AMLATTRIBUTES = "AmlAttributes";

    public static File? GetAmlFile(this ISubmodel amlSubmodel)
    {
        return amlSubmodel.OverSubmodelElementsOrEmpty().FirstOrDefault(f => IsAmlFile(f as File)) as File;
    }

    /// <summary>
    /// Check whether the given 'IFile' element represents/points to an AutomationML file.
    /// Currently, this is determined based on its (primary/supplementary) semantic ID.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static bool IsAmlFile(this IFile file)
    {
        return file?.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_AML_SM) ?? false;
    }

    /// <summary>
    /// Create a fragment string pointing to the given 'targetObject'. This string can be used as value for an AAS FragmentReference.
    /// </summary>
    /// <param name="targetObject"></param>
    /// <returns></returns>
    public static string CreateAmlFragmentString(this CAEXObject targetObject)
    {
        return AML_FRAGMENT_REF_PREFIX + targetObject.CAEXPath();
            
    }

    /// <summary>
    /// Finds the existing SML holding all 'published' attributes (see 'PublishAmlAttribute'). If no such SML exists,
    /// this will create a new (empty) one.
    /// </summary>
    /// <param name="submodel"></param>
    /// <returns></returns>
    public static SubmodelElementList GetOrCreateAmlAttributesSml(this ISubmodel submodel)
    {
        var amlAttributesSml = submodel.OverSubmodelElementsOrEmpty().FirstOrDefault(c => c.IdShort == IDSHORT_AMLATTRIBUTES) as SubmodelElementList;

        if (amlAttributesSml != null)
        {
            return amlAttributesSml;
        }

        amlAttributesSml = new SubmodelElementList(AasSubmodelElements.SubmodelElementCollection)
        {
            IdShort = IDSHORT_AMLATTRIBUTES
        };

        submodel.AddChild(amlAttributesSml);

        return amlAttributesSml;
    }

    /// <summary>
    /// This 'publishes' an attribute from an AML file as AAS property.
    /// 
    /// Therefore, this creates a new entry in the SML identified by 'GetOrCreateAmlAttributesSml(...)'. Th entry will contain 
    /// (1) a property the name, value and type of which are derived from the given 'attributeToPublish' and
    /// (2) a RelationshipElement that links the created property to the 'attributeToPublish' via a fragment reference.
    /// </summary>
    /// <param name="attributeToPublish"></param>
    /// <param name="targetSubmodel"></param>
    /// <returns></returns>
    public static SubmodelElementCollection PublishAmlAttribute(AttributeType attributeToPublish, Submodel targetSubmodel)
    {
        string attributeName = attributeToPublish.Name;
        string attributeValue = attributeToPublish.Value;
        string parentName = attributeToPublish.CAEXParent.Name();

        // the list containting all published properties
        var amlAttributesSml = targetSubmodel.GetOrCreateAmlAttributesSml();

        // the SMC for the property to publish
        var attributeSmc = new SubmodelElementCollection()
        {
            IdShort = $"{parentName}.{attributeName}"
        };
        amlAttributesSml.AddChild(attributeSmc);

        var property = new Property(DataTypeDefXsd.String, idShort: attributeName, value: attributeValue);
        attributeSmc.AddChild(property);
        targetSubmodel.SetAllParents();

        var first = property.GetReference();
        var second = targetSubmodel.GetAmlFile().GetReference();
        second.Keys.Add(new Key(KeyTypes.FragmentReference, attributeToPublish.CreateAmlFragmentString()));
        var relationship = new RelationshipElement(first, second, idShort: $"SameAs_{attributeName}");

        attributeSmc.AddChild(relationship);

        return attributeSmc;
    }
}
