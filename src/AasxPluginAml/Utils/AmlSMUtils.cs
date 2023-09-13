using AasCore.Aas3_0;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Engine.AmlObjects;
using Aml.Engine.AmlObjects.Extensions;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AasxPluginAml.Utils.BasicAmlUtils;
using AdminShellNS;
using System.IO;

namespace AasxPluginAml.Utils;

public static class AmlSMUtils
{
    public const string SEM_ID_AML_SM = "https://automationml.org/aas/1/0/AmlFile";
    public const string AML_FRAGMENT_REF_PREFIX = "AML/";

    public const string IDSHORT_AMLFILE = "AutomationMLFile";
    public const string IDSHORT_AMLVERSION = "AutomationMLVersion";
    public const string IDSHORT_AMLATTRIBUTES = "AutomationMLAttributes";
    public const string IDSHORT_AMLELEMENTS = "AutomationMLElements";
    public const string IDSHORT_AMLSTRUCTURE = "AutomationMLStructure";

    /// <summary>
    /// Import the AML file/AMLX package with the given 'amlFilePath'. This will lead to,
    /// (1) importing the file into the AASX package,
    /// (2) create a 'File' element pointing to the imported file and
    /// (3) creating a property 'AutomationMLVersion' that specifies the AML version used in the given AML file.
    /// </summary>
    /// <param name="submodel"></param>
    /// <param name="amlFilePath"></param>
    /// <param name="packageEnv"></param>
    /// <returns></returns>
    public static IFile? ImportAmlFile(ISubmodel submodel, string amlFilePath, AdminShellPackageEnv packageEnv)
    {
        CAEXDocument amlDocument = LoadAmlFile(amlFilePath);

        if (amlDocument == null)
        {
            return null;
        }

        var amlFileName = MakeValidFileName(Path.GetFileName(amlFilePath));
        var internalAmlFilePath = packageEnv.AddSupplementaryFileToStore(amlFilePath, "/aasx/files", amlFileName, false);

        var amlVersion = amlDocument.AutomationMLVersion();

        var amlFile = new AasCore.Aas3_0.File("text/xml", idShort: IDSHORT_AMLFILE, value: internalAmlFilePath);
        amlFile.SemanticId = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() { new Key(KeyTypes.GlobalReference, SEM_ID_AML_SM) });
        submodel.AddChild(amlFile);

        var amlVersionProperty = new Property(DataTypeDefXsd.String, idShort: IDSHORT_AMLVERSION, value: amlVersion);
        submodel.AddChild(amlVersionProperty);

        return amlFile;

    }

    // We need to sanitize the file name. Especially file names with spaces will lead to the package explorer not
    // being able to save the AASX package after adding the file.
    // see https://stackoverflow.com/a/847251
    private static string MakeValidFileName(string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()) + " ");
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }


    /// <summary>
    /// Find the 'IFile' element that represents/points to an AutomationML file.
    /// Currently, this is determined based on the files (primary/supplementary) semanticID.
    /// </summary>
    /// <param name="amlSubmodel"></param>
    /// <returns></returns>
    public static IFile? GetAmlFile(this ISubmodel amlSubmodel)
    {
        return amlSubmodel.OverSubmodelElementsOrEmpty().FirstOrDefault(f => IsAmlFile(f as AasCore.Aas3_0.File)) as AasCore.Aas3_0.File;
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
    /// Finds the existing SML holding all 'published' elements (see 'PublishAmlElement'). If no such SML exists,
    /// this will create a new (empty) one.
    /// </summary>
    /// <param name="submodel"></param>
    /// <returns></returns>
    public static SubmodelElementList GetOrCreateAmlElementsSml(this ISubmodel submodel)
    {
        var amlAttributesSml = submodel.OverSubmodelElementsOrEmpty().FirstOrDefault(c => c.IdShort == IDSHORT_AMLELEMENTS) as SubmodelElementList;

        if (amlAttributesSml != null)
        {
            return amlAttributesSml;
        }

        amlAttributesSml = new SubmodelElementList(AasSubmodelElements.ReferenceElement)
        {
            IdShort = IDSHORT_AMLELEMENTS
        };

        submodel.AddChild(amlAttributesSml);

        return amlAttributesSml;
    }

    /// <summary>
    /// Finds the existing SML holding all elements linked to an AAS entity (see 'PublishAmlStrucutre'). If no such SML exists,
    /// this will create a new (empty) one.
    /// </summary>
    /// <param name="submodel"></param>
    /// <returns></returns>
    public static SubmodelElementList GetOrCreateAmlStructureSml(this ISubmodel submodel)
    {
        var amlAttributesSml = submodel.OverSubmodelElementsOrEmpty().FirstOrDefault(c => c.IdShort == IDSHORT_AMLSTRUCTURE) as SubmodelElementList;

        if (amlAttributesSml != null)
        {
            return amlAttributesSml;
        }

        amlAttributesSml = new SubmodelElementList(AasSubmodelElements.ReferenceElement)
        {
            IdShort = IDSHORT_AMLSTRUCTURE
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

        // the list containting all published properties
        var amlAttributesSml = targetSubmodel.GetOrCreateAmlAttributesSml();

        // the SMC for the property to publish
        var attributeSmc = new SubmodelElementCollection()
        {
            IdShort = attributeToPublish.GetFullNodePath()
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

    /// <summary>
    /// This 'publishes' an element from an AML file as AAS reference.
    /// 
    /// Therefore, this creates a new entry in the SML identified by 'GetOrCreateAmlElementsSml(...)'.
    /// </summary>
    /// <param name="elementToPublish"></param>
    /// <param name="targetSubmodel"></param>
    /// <returns></returns>
    public static ReferenceElement PublishAmlElement(SystemUnitClassType elementToPublish, Submodel targetSubmodel)
    {
        string elementName = elementToPublish.GetFullNodePath();

        // the list containting all published elements
        var amlElementsSml = targetSubmodel.GetOrCreateAmlElementsSml();

        var reference = targetSubmodel.GetAmlFile().GetReference();
        reference.Keys.Add(new Key(KeyTypes.FragmentReference, elementToPublish.CreateAmlFragmentString()));

        // the reference for the element to publish
        var elementReference = new ReferenceElement()
        {
            IdShort = elementName,
            Value = reference
        };
        amlElementsSml.AddChild(elementReference);

        return elementReference;
    }

    /// <summary>
    /// This 'publishes' an element from an AML file by linking it to an existing AAS entity.
    /// 
    /// Therefore, this creates a new entry in the SML identified by 'GetOrCreateAmlStructureSml(...)'.
    /// </summary>
    /// <param name="elementToPublish"></param>
    /// <param name="targetSubmodel"></param>
    /// <returns></returns>
    public static RelationshipElement PublishAmlStructure(SystemUnitClassType elementToPublish, Submodel targetSubmodel, IEnumerable<IKey> entityReferenceKeys)
    {
        string elementName = elementToPublish.GetFullNodePath();

        // the list containting all published elements
        var amlElementsSml = targetSubmodel.GetOrCreateAmlStructureSml();

        var reference = targetSubmodel.GetAmlFile().GetReference();
        reference.Keys.Add(new Key(KeyTypes.FragmentReference, elementToPublish.CreateAmlFragmentString()));

        var first = new Reference(ReferenceTypes.ModelReference, new List<IKey>(entityReferenceKeys));

        var second = targetSubmodel.GetAmlFile().GetReference();
        second.Keys.Add(new Key(KeyTypes.FragmentReference, elementToPublish.CreateAmlFragmentString()));

        var relationship = new RelationshipElement(first, second, idShort: $"SameAs_{elementName}");
        amlElementsSml.AddChild(relationship);

        return relationship;
    }
}
