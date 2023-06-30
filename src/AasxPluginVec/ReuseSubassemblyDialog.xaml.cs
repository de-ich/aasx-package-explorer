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
using static AasxPluginVec.BomSMUtils;
using static AasxPluginVec.VecSMUtils;

namespace AasxIntegrationBase
{
    internal partial class ReuseSubassemblyNameDialog : Window
    {
        protected AdministrationShellEnv env;
        protected AdministrationShell aas;
        protected IEnumerable<Entity> selectedEntities;
        protected IEnumerable<AdministrationShell> Shells;
        public string SubassemblyEntityName { get; set; } = string.Empty;
        public List<string> AdminShellsToSelect { get; } = new List<string>();
        public AdministrationShell AasToReuse { get; set; }
        public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();

        public ReuseSubassemblyNameDialog(Window owner, IEnumerable<Entity> entities, AdministrationShellEnv env)
        {
            this.env = env;
            this.Shells = env.AdministrationShells;
            this.selectedEntities = entities;
            this.Owner = owner;
            DataContext = this;
            SubassemblyEntityName = "Subassembly_" + string.Join("_", entities.Select(e => e.idShort));
            AdminShellsToSelect.AddRange(this.Shells.Select(s => s.idShort));
            InitializeComponent();

            /*foreach(var entity in entities)
            {
                AddString(entity.idShort, entity.idShort);
            }*/
        }

        protected void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var selectedAasName = (sender as ComboBox)?.SelectedItem.ToString();
            this.AasToReuse = this.Shells.First(a => a.idShort == selectedAasName);

            SubAssemblyParts.RowDefinitions.Clear();

            if (this.AasToReuse != null)
            {
                var bomSubmodel = FindBomSubmodel(this.AasToReuse, env);

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
            CreateLabel(entity.idShort);
            CreateComboBox(entity.idShort);
        }

        private void AddRow() => SubAssemblyParts.RowDefinitions.Add(new RowDefinition());

        private void CreateLabel(string content)
        {
            var label = new Label { Content = content };
            AddElement(label, 0);
        }

        private ComboBox CreateComboBox(string text)
        {
            var comboBox = new ComboBox { ItemsSource = this.selectedEntities.Select(e => e.idShort) };
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
