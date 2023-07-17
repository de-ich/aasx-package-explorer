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

namespace AasxIntegrationBase
{
    internal partial class CreateOrderDialog : Window
    {
        public string OrderNumber { get; set; } = string.Empty;

        public CreateOrderDialog(Window owner)
        {
            this.Owner = owner;
            DataContext = this;
            
            InitializeComponent();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
