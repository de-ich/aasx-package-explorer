/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


// ReSharper disable MergeIntoPattern

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static AdminShellNS.AdminShellV20;

namespace AasxIntegrationBase
{
    internal partial class DeriveSubassemblyDialog : Window
    {
        public string SubassemblyAASName { get; set; } = string.Empty;
        public string SubassemblyEntityName { get; set; } = string.Empty;
        public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();

        public DeriveSubassemblyDialog(Window owner, IEnumerable<Entity> entities)
        {
            this.Owner = owner;
            DataContext = this;
            SubassemblyAASName = string.Join("_", entities.Select(e => e.idShort));
            SubassemblyEntityName = "Subassembly_" + string.Join("_", entities.Select(e => e.idShort));
            InitializeComponent();

            foreach(var entity in entities)
            {
                AddString(entity.idShort, entity.idShort);
            }
        }

        private void AddString(string label, string value)
        {
            AddRow();
            CreateLabel(label);
            CreateTextBox(value);
        }

        private void AddRow() => SubAssemblyParts.RowDefinitions.Add(new RowDefinition());

        private void CreateLabel(string content)
        {
            var label = new Label { Content = content };
            AddElement(label, 0);
        }

        private TextBox CreateTextBox(string text)
        {
            this.PartNames[text] = text;
            var textBox = new TextBox { Text = text };
            textBox.TextChanged += (sender, arguments) =>
            {
                this.PartNames[text] = (sender as TextBox).Text;
            };
            AddElement(textBox, 2);
            return textBox;
        }

        private void AddElement(FrameworkElement element, int column)
        {
            Grid.SetRow(element, SubAssemblyParts.RowDefinitions.Count - 1);
            Grid.SetColumn(element, column);
            SubAssemblyParts.Children.Add(element);
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
