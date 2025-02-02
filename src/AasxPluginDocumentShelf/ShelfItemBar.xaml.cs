/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#if USE_WPF

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AasxPluginDocumentShelf
{
    /// <summary>
    /// Displays a bar-like representation of a ShelfEntity
    /// </summary>
    public partial class ShelfItemBar : UserControl
    {

        public ShelfItemBar()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // clear
            PanelCountries.Children.Clear();
            TextBlockOrga.Text = "";
            TextBlockTitle.Text = "";
            TextBlockFurther.Text = "";

            // acccess
            var data = this.DataContext as DocumentEntity;
            if (data == null)
                // uups!
                return;

            // set contries
            if (data.CountryCodes != null)
                foreach (var cc in data.CountryCodes)
                    if (cc != null && cc.Trim().Length > 0)
                    {
                        var codeStr = cc.Trim().ToUpper();
                        foreach (var ev in (CountryFlag.CountryCode[])Enum.GetValues(typeof(CountryFlag.CountryCode)))
                            if (Enum.GetName(typeof(CountryFlag.CountryCode), ev)?.Trim().ToUpper() == codeStr)
                            {
                                var cf = new CountryFlag.CountryFlag();
                                cf.Code = ev;
                                cf.Width = 20;
                                PanelCountries.Children.Add(cf);
                                break;
                            }
                    }

            // set orga
            TextBlockOrga.Text = "" + data.Organization;

            // set title
            TextBlockTitle.Text = "" + data.Title;

            // set further
            TextBlockFurther.Text = "" + data.FurtherInfo;

            // Image to be (later) shown
            if (data.ImgContainerWpf != null)
            {
                BorderPlaceholder.Background = Brushes.White;
                BorderPlaceholder.BorderThickness = new Thickness(1);
                BorderPlaceholder.BorderBrush = Brushes.DarkGray;
                BorderPlaceholder.Child = data.ImgContainerWpf;
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var data = this.DataContext as DocumentEntity;
            if (e.ClickCount == 2 && data != null)
                data.RaiseDoubleClick();

        }

        private Point dragStartPoint = new Point(0, 0);

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var dataEnt = this.DataContext as DocumentEntity;
            if (e.LeftButton == MouseButtonState.Pressed && dataEnt != null)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    dataEnt.RaiseDragStart();
                }
            }
        }

        private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var data = this.DataContext as DocumentEntity;
            if (data != null && menuItem != null && (menuItem.Header as string) != null)
                data.RaiseMenuClick(menuItem.Header as string, menuItem.Tag);
        }

        private void ItemButton_Click(object sender, RoutedEventArgs e)
        {
            // access
            ContextMenu cm = GridItem?.FindResource("ContextMenuItem") as ContextMenu;
            var data = this.DataContext as DocumentEntity;
            if (cm == null || data == null)
                return;

            // clear old items (very stupid)
            while (cm.Items.Count > 4)
                cm.Items.RemoveAt(4);

            // add new items
            if (data.Relations != null && data.Relations.Count > 0)
            {
                cm.Items.Add(new Separator());

                foreach (var reltup in data.Relations)
                {
                    var drt = reltup.Item1;
                    var re = reltup.Item2;
                    if (re == null || re.Count < 1)
                        continue;
                    var mi = new MenuItem();
                    mi.Header = "" + drt.ToString() + ": " + re.Last.value;
                    mi.Icon = " \x2794";
                    mi.Click += MenuItem_Click;
                    mi.Tag = reltup;

                    cm.Items.Add(mi);
                }
            }

            // show
            cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;
        }

    }
}

#endif