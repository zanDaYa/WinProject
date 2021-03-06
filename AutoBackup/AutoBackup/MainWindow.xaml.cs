﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AutoBackup.ViewModels;
using Telerik.Windows.DragDrop;
using Telerik.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using System.ComponentModel;

namespace AutoBackup
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    enum keydown
    {
        Delete
    }
    

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            // Set the items source of the controls:
            //allProductsView.ItemsSource = null;
            //allProductsView.ItemsSource = CategoryViewModel.Generate();

            //var Background = new BackgroundWorkerTest();
            IList wishlistSource = new ObservableCollection<ProductViewModel>();
            wishlistView.ItemsSource = wishlistSource;
            

            DragDropManager.AddDragOverHandler(allProductsView, OnItemDragOver);
            DragDropManager.AddDropHandler(allProductsView, OnDrop);
        }

        private void RadTreeView_LoadOnDemand(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            e.Handled = true;
            RadTreeViewItem expandedItem = e.OriginalSource as RadTreeViewItem ?? (e.OriginalSource as FrameworkElement).ParentOfType<RadTreeViewItem>();
            if (expandedItem == null)
                return;

            Drive drive = GetDrive(expandedItem);
            if (drive != null)
            {
                ServiceFacade.Instance.LoadChildren(drive);
                if (drive.Children.Count == 0)
                {
                    expandedItem.IsLoadOnDemandEnabled = false;
                }
                return;
            }

            Directory directory = GetDirectory(expandedItem);
            if (directory != null)
            {
                ServiceFacade.Instance.LoadChildren(directory);
                if (directory.Children.Count == 0)
                {
                    expandedItem.IsLoadOnDemandEnabled = false;
                }
            }
        }


        private static Drive GetDrive(RadTreeViewItem expandedItem)
        {
            return expandedItem.Item as Drive;
        }

        private static Directory GetDirectory(RadTreeViewItem expandedItem)
        {
            return expandedItem.Item as Directory;
        }

        private void RadTreeView_ItemPrepared(object sender, RadTreeViewItemPreparedEventArgs e)
        {
            if (e.PreparedItem.DataContext is File)
            {
                e.PreparedItem.IsLoadOnDemandEnabled = false;
            }
        }

        private void OnDrop(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
        {
            var data = (IList)DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedData");
            if (data == null) return;
            if (e.Effects != DragDropEffects.None)
            {
                var destinationItem = (e.OriginalSource as FrameworkElement).ParentOfType<RadTreeViewItem>();
                var dropDetails = DragDropPayloadManager.GetDataFromObject(e.Data, "DropDetails") as DropIndicationDetails;

                if (destinationItems != null)
                {
                    var backup = new Backup();
                    for (int i = 0; i < dropDetails.CurrentDraggedItem.Count; i++)
                    {
                        var source = (dropDetails.CurrentDraggedItem[i] as ProductViewModel);
                        var dest = (dropDetails.CurrentDraggedOverItem as ProductViewModel);
                        string sourcePath = System.IO.Path.GetDirectoryName(source.FullPath);
                        if (destinationItems.Count == 0)
                        {
                            RadTreeView_LoadOnDemand(null, e);
                        }
                        backup.CopyFile(sourcePath, dest.FullPath, source.Name);
                        int dropIndex = dropDetails.DropIndex >= destinationItems.Count ? destinationItems.Count : dropDetails.DropIndex < 0 ? 0 : dropDetails.DropIndex;
                        this.destinationItems.Insert(dropIndex, data[i]);
                    }
                }
            }
        }

        IList destinationItems = null;
        private void OnItemDragOver(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
        {
            var item = (e.OriginalSource as FrameworkElement).ParentOfType<RadTreeViewItem>();
            if (item == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            var position = GetPosition(item, e.GetPosition(item));
            if (item.Level == 0 && position != DropPosition.Inside)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            RadTreeView tree = sender as RadTreeView;
            var draggedData = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedData");
            var dropDetails = DragDropPayloadManager.GetDataFromObject(e.Data, "DropDetails") as DropIndicationDetails;

            if ((draggedData == null && dropDetails == null))
            {
                return;
            }
            if (position != DropPosition.Inside)
            {
                e.Effects = DragDropEffects.All;

                destinationItems = item.Level > 0 ? (IList)item.ParentItem.ItemsSource : (IList)tree.ItemsSource;
                int index = destinationItems.IndexOf(item.Item);
                dropDetails.DropIndex = position == DropPosition.Before ? index : index + 1;
            }
            else
            {
                destinationItems = (IList)item.ItemsSource;
                int index = 0;

                if (destinationItems == null)
                {
                    e.Effects = DragDropEffects.None;
                }
                else
                {
                    e.Effects = DragDropEffects.All;
                    dropDetails.DropIndex = index;
                }
            }

            dropDetails.CurrentDraggedOverItem = item.Item;
            dropDetails.CurrentDropPosition = position;

            e.Handled = true;
        }



        private DropPosition GetPosition(RadTreeViewItem item, Point point)
        {
            double treeViewItemHeight = 24;
            if (point.Y < treeViewItemHeight / 4)
            {
                return DropPosition.Before;
            }
            else if (point.Y > treeViewItemHeight * 3 / 4)
            {
                return DropPosition.After;
            }

            return DropPosition.Inside;
        }

        private void AllProductsView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void ListBoxKeyDown(object sender, KeyEventArgs e)
        {
            
            var collection = (sender as System.Windows.Controls.ListBox).ItemsSource as IList;
            var selections = (sender as System.Windows.Controls.ListBox).SelectedItems as IList;

            if (e.Key == System.Windows.Input.Key.Delete)
            {
                while (selections.Count!=0)
                    collection.Remove(selections[0]);
                
            }
        }

        
    }
}
