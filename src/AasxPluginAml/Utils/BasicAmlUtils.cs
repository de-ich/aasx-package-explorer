using Aml.Engine.AmlObjects;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginAml.Utils;

public static class BasicAmlUtils
{
    public static CAEXDocument? LoadAmlFile(Stream amlFileStream)
    {
        try
        {
            // first try: try directly loading the aml file
            return CAEXDocument.LoadFromStream(amlFileStream);
        }
        catch (Exception e1)
        {
            // the amlFilePath might represent a container 
        }

        try
        {
            // second try: try loading the aml file as AMLX package
            var container = new AutomationMLContainer(amlFileStream);
            var amlDocumentStream = container.RootDocumentStream();
            return CAEXDocument.LoadFromStream(amlDocumentStream);
        }
        catch (Exception e2)
        {
            // the amlFilePath probably does not represent an aml file or does not exist
            return null;
        }
    }

    public static CAEXDocument? LoadAmlFile(string amlFilePath)
    {
        try
        {
            // first try: try directly loading the aml file
            return CAEXDocument.LoadFromFile(amlFilePath);
        }
        catch (Exception e1)
        {
            // the amlFilePath might represent a container 
        }

        try
        {
            // second try: try loading the aml file as AMLX package
            var container = new AutomationMLContainer(amlFilePath);
            var amlDocumentStream = container.RootDocumentStream();
            return CAEXDocument.LoadFromStream(amlDocumentStream);
        }
        catch (Exception e2)
        {
            // the amlFilePath probably does not represent an aml file or does not exist
            return null;
        }
    }

    public static T FindByPath<T>(this CAEXDocument amlDocument, string path) where T : CAEXObject
    {
        var element = amlDocument.FindByPath(path) ?? throw new Exception($"Unable to find element by path {path} in the given AML document!");

        var castElement = element as T ?? throw new Exception($"Element at path {path} in the given AML document is of an unexpected type (expected: {typeof(T).Name}, got: {element.GetType().Name}!");

        return castElement;
    }
}
