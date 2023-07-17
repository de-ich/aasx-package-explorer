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
    internal partial class AssociateSubassembliesWithModuleDialog : Window
    {
        protected AasCore.Aas3_0.Environment env;
        protected AssetAdministrationShell aas;
        protected IEnumerable<Entity> selectedEntities;
        public List<string> ModulesToSelect { get; } = new List<string>();
        protected Dictionary<string, Entity> modulesByName = new Dictionary<string, Entity>();
        public Entity SelectedModule { get; set; }

        public AssociateSubassembliesWithModuleDialog(Window owner, IEnumerable<Entity> entities, AssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            this.env = env;
            this.aas = aas;
            this.selectedEntities = entities;
            this.Owner = owner;
            DataContext = this;

            var moduleBomSubmodels = FindBomSubmodels(aas, env).Where(sm => FindEntryNode(sm)?.EnumerateChildren().Where(c => c is Entity).All(c => (c as Entity).EntityType == EntityType.CoManagedEntity) ?? false);
            var moduleEntitiesToSelect = moduleBomSubmodels.Select(sm => FindEntryNode(sm)).SelectMany(e => e.EnumerateChildren()).Where(c => c is Entity).Select(c => c as Entity);
            foreach(var moduleEntityToSelect in moduleEntitiesToSelect)
            {
                modulesByName[moduleEntityToSelect.IdShort] = moduleEntityToSelect;
                ModulesToSelect.Add(moduleEntityToSelect.IdShort);
            }
            InitializeComponent();
        }

        protected void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var selectedModuleName = (sender as ComboBox)?.SelectedItem.ToString();
            this.SelectedModule = modulesByName[selectedModuleName];
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
