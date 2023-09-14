using AasCore.Aas3_0;
using Aml.Engine.CAEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AasxPluginAml.Views;

public class AmlViewerPanel: StackPanel
{
    public CAEXObject SelectedObject { get; private set; }
    public readonly Submodel AssociatedSubmodel;

    public AmlViewerPanel(CAEXDocument amlDocument, Submodel associatedSubmodel) : base()
    {
        AssociatedSubmodel = associatedSubmodel;

        Orientation = Orientation.Vertical;
        DockPanel.SetDock(this, Dock.Top);

        var treeView = new TreeView() { Name = "amlTree" };
        treeView.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

        amlDocument.CAEXFile.InstanceHierarchy.ToList().ForEach(ih => FillTreeviewRecursively(treeView, ih));
        amlDocument.CAEXFile.SystemUnitClassLib.ToList().ForEach(sucLib => FillTreeviewRecursively(treeView, sucLib));
        amlDocument.CAEXFile.RoleClassLib.ToList().ForEach(rcLib => FillTreeviewRecursively(treeView, rcLib));
        amlDocument.CAEXFile.InterfaceClassLib.ToList().ForEach(ifcLib => FillTreeviewRecursively(treeView, ifcLib));
        amlDocument.CAEXFile.AttributeTypeLib.ToList().ForEach(atLib => FillTreeviewRecursively(treeView, atLib));

        Children.Add(treeView);
    }

    private void FillTreeviewRecursively(object parent, CAEXObject caexObject)
    {
        if (caexObject == null)
        {
            return;
        }

        var elementName = GetElementName(caexObject);

        var childItem = new TreeViewItem() { Header = elementName };
        (parent as ItemsControl)?.Items.Add(childItem);

        childItem.Selected += (object sender, RoutedEventArgs e) =>
        {
            SelectedObject = caexObject;
            e.Handled = true;
        };

        if (caexObject is IInternalElementContainer ieContainer)
        {
            foreach (var ie in ieContainer.InternalElement)
            {
                FillTreeviewRecursively(childItem, ie);
            }
        }

        if (caexObject is SystemUnitClassLibType sucLibrary)
        {
            foreach (var childSuc in sucLibrary.SystemUnitClass)
            {
                FillTreeviewRecursively(childItem, childSuc);
            }
        }

        if (caexObject is RoleClassLibType rcLibrary)
        {
            foreach (var rc in rcLibrary.RoleClass)
            {
                FillTreeviewRecursively(childItem, rc);
            }
        }

        if (caexObject is InterfaceClassLibType ifcLibrary)
        {
            foreach (var ifc in ifcLibrary.InterfaceClass)
            {
                FillTreeviewRecursively(childItem, ifc);
            }
        }

        if (caexObject is IObjectWithAttributes attContainer)
        {
            foreach (var childAtt in attContainer.Attribute)
            {
                FillTreeviewRecursively(childItem, childAtt);
            }
        }

        if (caexObject is IObjectWithExternalInterface eiContainer)
        {
            foreach (var childEI in eiContainer.ExternalInterface)
            {
                FillTreeviewRecursively(childItem, childEI);
            }
        }

        if (caexObject is IAttributeTypeContainer attTypeContainer)
        {
            foreach (var childAttType in attTypeContainer.AttributeType)
            {
                FillTreeviewRecursively(childItem, childAttType);
            }
        }

        if (caexObject is SystemUnitClassType suc)
        {
            foreach (var supportedRoleClass in suc.SupportedRoleClass)
            {
                FillTreeviewRecursively(childItem, supportedRoleClass.RoleClass);
            }
            foreach (var roleReference in suc.RoleReferences)
            {
                FillTreeviewRecursively(childItem, roleReference.RoleClass);
            }
        }

        if (caexObject is RoleFamilyType rf)
        {
            foreach (var roleClass in rf.RoleClass)
            {
                FillTreeviewRecursively(childItem, roleClass);
            }
        }
    }

    private static string GetElementName(CAEXObject caexObject)
    {
        var elementName = $"<{caexObject.GetType().Name}> {caexObject.Name}";

        if (caexObject is AttributeType att)
        {
            elementName += $" = {att.Value}";
        }

        return elementName;
    }

}
