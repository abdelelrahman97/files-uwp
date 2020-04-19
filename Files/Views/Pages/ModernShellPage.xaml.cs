﻿using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;


namespace Files.Views.Pages
{
    public sealed partial class ModernShellPage : Page, IShellPage
    {
        public event EventHandler RefreshRequestedEvent;
        public event EventHandler CancelLoadRequestedEvent;
        public event EventHandler NavigateToParentRequestedEvent;

        public ModernShellPage()
        {
            this.InitializeComponent();
            if (App.AppSettings.DrivesManager.ShowUserConsentOnInit)
            {
                App.AppSettings.DrivesManager.ShowUserConsentOnInit = false;
                DisplayFilesystemConsentDialog();
            }

            App.CurrentInstance = this as IShellPage;
            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = "New tab";
            App.CurrentInstance.NavigationToolbar.CanGoBack = false;
            App.CurrentInstance.NavigationToolbar.CanGoForward = false;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
        }

        Type IShellPage.CurrentPageType => ItemDisplayFrame.SourcePageType;
        INavigationToolbar IShellPage.NavigationToolbar => NavToolbar;
        INavigationControlItem IShellPage.SidebarSelectedItem { get => SidebarControl.SelectedSidebarItem; set => SidebarControl.SelectedSidebarItem = value; }
        Frame IShellPage.ContentFrame => ItemDisplayFrame;
        object IShellPage.OperationsControl => null;

        private BaseLayout GetContentOrNull()
        {
            if ((ItemDisplayFrame.Content as BaseLayout) != null)
            {
                return ItemDisplayFrame.Content as BaseLayout;
            }
            else
            {
                return null;
            }
        }

        private async void DisplayFilesystemConsentDialog()
        {
            await App.ConsentDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
        }

        string NavParams = null;

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParams = eventArgs.Parameter.ToString();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            switch (NavParams)
            {
                case "Start":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems[0];
                    break;
                case "New tab":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems[0];
                    break;
                case "Desktop":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DesktopPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Downloads":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DownloadsPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Documents":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DocumentsPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Pictures":
                    ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.AppSettings.PicturesPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Music":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.MusicPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Videos":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.VideosPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    if (NavParams[0] >= 'A' && NavParams[0] <= 'Z' && NavParams[1] == ':')
                    {
                        ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), NavParams, new SuppressNavigationTransitionInfo());
                        SidebarControl.SelectedSidebarItem = App.AppSettings.DrivesManager.Drives.First(x => x.Tag.ToString().Equals($"{NavParams[0]}:\\", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        SidebarControl.SelectedSidebarItem = null;
                    }
                    break;
            }

            this.Loaded -= Page_Loaded;
        }

        private void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                App.InteractionViewModel.IsPageTypeNotHome = true;
                // Reset DataGrid Rows that may be in "cut" command mode
                IEnumerable items = (ItemDisplayFrame.Content as GenericFileBrowser).AllView.ItemsSource;
                if (items == null)
                    return;
                foreach (ListedItem listedItem in items)
                {
                    FrameworkElement element = (ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(listedItem);
                    if (element != null)
                        element.Opacity = 1;
                }
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
            {
                App.InteractionViewModel.IsPageTypeNotHome = true;
                // Reset Photo Grid items that may be in "cut" command mode
                foreach (ListedItem listedItem in (ItemDisplayFrame.Content as PhotoAlbum).FileList.Items)
                {
                    List<Grid> itemContentGrids = new List<Grid>();
                    GridViewItem gridViewItem = (ItemDisplayFrame.Content as PhotoAlbum).FileList.ContainerFromItem(listedItem) as GridViewItem;
                    if (gridViewItem == null)
                        return;
                    FindHelpers.FindChildren<Grid>(itemContentGrids, gridViewItem);
                    var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                    imageOfItem.Opacity = 1;
                }
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(YourHome))
            {
                App.InteractionViewModel.IsPageTypeNotHome = false;
            }
        }

        public void UpdateProgressFlyout(InteractionOperationType operationType, int amountComplete, int amountTotal)
        {
            this.FindName("ProgressFlyout");

            string operationText = null;
            switch (operationType)
            {
                case InteractionOperationType.PasteItems:
                    operationText = "Completing Paste";
                    break;
                case InteractionOperationType.DeleteItems:
                    operationText = "Deleting Items";
                    break;
            }
            ProgressFlyoutTextBlock.Text = operationText + " (" + amountComplete + "/" + amountTotal + ")" + "...";
            ProgressFlyoutProgressBar.Value = amountComplete;
            ProgressFlyoutProgressBar.Maximum = amountTotal;

            if (amountComplete == amountTotal)
            {
                UnloadObject(ProgressFlyout);
            }
        }

        private void ModernShellPage_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var tabInstance = App.CurrentInstance != null;

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: e.Key)
            {
                case (false, false, true, true, VirtualKey.Left): //alt + back arrow, backward
                    Back_Click();
                    break;
                case (false, false, true, true, VirtualKey.Right): //alt + right arrow, forward
                    Forward_Click();
                    break;
                case (true, false, false, true, VirtualKey.R): //ctrl + r, refresh
                    Refresh_Click();
                    break;
                    //case (true, false, false, true, VirtualKey.F): //ctrl + f, search box
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 0;
                    //    break;
                    //case (true, false, false, true, VirtualKey.E): //ctrl + e, search box
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 0;
                    //    break;
                    //case (false, false, true, true, VirtualKey.H):
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 1;
                    //    break;
                    //case (false, false, true, true, VirtualKey.S):
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 2;
                    //    break;
                    //case (false, false, true, true, VirtualKey.V):
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 3;
                    //    break;
            };
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                Back_Click();
            }
            else if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                Forward_Click();
            }
        }

        public void Refresh_Click()
        {
            RefreshRequestedEvent?.Invoke(this, EventArgs.Empty);
        }

        public void Back_Click()
        {
            App.CurrentInstance.NavigationToolbar.CanGoBack = false;
            if (ItemDisplayFrame.CanGoBack)
            {
                CancelLoadRequestedEvent?.Invoke(this, EventArgs.Empty);
                var previousSourcePageType = ItemDisplayFrame.BackStack[ItemDisplayFrame.BackStack.Count - 1].SourcePageType;

                if (previousSourcePageType == typeof(YourHome) && previousSourcePageType != null)
                {
                    App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.First(x => x.Path.Equals("Home"));
                    App.CurrentInstance.NavigationToolbar.PathControlDisplayText = "New tab";
                }
                ItemDisplayFrame.GoBack();
            }
        }

        public void Forward_Click()
        {
            App.CurrentInstance.NavigationToolbar.CanGoForward = false;
            if (ItemDisplayFrame.CanGoForward)
            {
                CancelLoadRequestedEvent?.Invoke(this, EventArgs.Empty);
                var incomingSourcePageType = ItemDisplayFrame.ForwardStack[ItemDisplayFrame.ForwardStack.Count - 1].SourcePageType;

                if (incomingSourcePageType == typeof(YourHome) && incomingSourcePageType != null)
                {
                    App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.First(x => x.Path.Equals("Home"));
                    App.CurrentInstance.NavigationToolbar.PathControlDisplayText = "New tab";
                }
                ItemDisplayFrame.GoForward();
            }
        }

        public void Up_Click()
        {
            App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            CancelLoadRequestedEvent?.Invoke(this, EventArgs.Empty);
            NavigateToParentRequestedEvent?.Invoke(this, EventArgs.Empty);
        }

        public void NavigateBack() => Back_Click();

        public void NavigateForward() => Forward_Click();

        public void NavigateUp() => Up_Click();
    }

    public enum InteractionOperationType
    {
        PasteItems = 0,
        DeleteItems = 1,
    }

    public class PathBoxItem
    {
        public string Title { get; set; }
        public string Path { get; set; }
    }
}
