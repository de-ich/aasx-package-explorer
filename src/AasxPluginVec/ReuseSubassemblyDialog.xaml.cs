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
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVec.BomSMUtils;
using static AasxPluginVec.VecSMUtils;

namespace AasxIntegrationBase
{
    internal partial class ReuseSubassemblyNameDialog : Window
    {
        protected AasCore.Aas3_0.Environment env;
        protected IEnumerable<Entity> selectedEntities;
        protected IEnumerable<IAssetAdministrationShell> Shells;
        public string SubassemblyEntityName { get; set; } = string.Empty;
        public List<string> AdminShellsToSelect { get; } = new List<string>();
        public IAssetAdministrationShell AasToReuse { get; set; }
        public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();

        public ReuseSubassemblyNameDialog(Window owner, IEnumerable<Entity> entities, AasCore.Aas3_0.Environment env)
        {
            this.env = env;
            this.Shells = env.AssetAdministrationShells;
            this.selectedEntities = entities;
            this.Owner = owner;
            DataContext = this;
            SubassemblyEntityName = "Subassembly_" + string.Join("_", entities.Select(e => e.IdShort));
            AdminShellsToSelect.AddRange(this.Shells.Select(s => s.IdShort));
            InitializeComponent();

            /*foreach(var entity in entities)
            {
                AddString(entity.idShort, entity.idShort);
            }*/
        }

        protected void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var selectedAasName = (sender as ComboBox)?.SelectedItem.ToString();
            this.AasToReuse = this.Shells.First(a => a.IdShort == selectedAasName);

            SubAssemblyParts.RowDefinitions.Clear();

            if (this.AasToReuse != null)
            {
                var bomSubmodel = FindFirstBomSubmodel(this.AasToReuse, env);

                var atomicComponentEntities = GetLeafNodes(bomSubmodel);

                foreach (var entity in atomicComponentEntities)
                {
                    AddComponentToMap(entity);
                }
            }
        }

        private void AddComponentToMap(Entity entity)
        {
            AddRow();
            CreateLabel(entity.IdShort);
            CreateComboBox(entity.IdShort);
        }

        private void AddRow() => SubAssemblyParts.RowDefinitions.Add(new RowDefinition());

        private void CreateLabel(string content)
        {
            var label = new Label { Content = content };
            AddElement(label, 0);
        }

        private ComboBox CreateComboBox(string text)
        {
            var comboBox = new ComboBox { ItemsSource = this.selectedEntities.Select(e => e.IdShort) };
            comboBox.SelectionChanged += (sender, arguments) =>
            {
                this.PartNames[(sender as ComboBox)?.SelectedItem.ToString()] = text;
            };
            AddElement(comboBox, 2);
            return comboBox;
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
