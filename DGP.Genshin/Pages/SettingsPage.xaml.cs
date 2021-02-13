﻿using DGP.Genshin.Data;
using DGP.Genshin.Service;
using ModernWpf;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DGP.Genshin.Pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //unreleased character present
            this.IsUnreleasedDataPresent = SettingService.Instance.GetOrDefault(Setting.ShowUnreleasedData, false);
            //traveler present
            this.TravelerElement = SettingService.Instance.GetOrDefault(Setting.PresentTravelerElementType, Element.Anemo, Setting.EnumConverter<Element>);
            foreach (RadioButton radioButton in this.TravelerOptions.Children)
            {
                if (ElementHelper.GetElement(radioButton) == this.TravelerElement)
                {
                    radioButton.IsChecked = true;
                }
            }
            //version
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            this.VersionString = $"DGP.Genshin - version {v.Major}.{v.Minor}.{v.Build} Build {v.Revision}";
            //theme
            Func<object, ApplicationTheme?> converter = n => { if (n == null) { return null; } return (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), n.ToString()); };
            this.ThemeComboBox.SelectedIndex = (SettingService.Instance.GetOrDefault(Setting.AppTheme, null, converter)) switch
            {
                ApplicationTheme.Light => 0,
                ApplicationTheme.Dark => 1,
                _ => 2,
            };
        }

        #region propdp
        public bool IsUnreleasedDataPresent
        {
            get => (bool)this.GetValue(IsUnreleasedDataPresentProperty);
            set => this.SetValue(IsUnreleasedDataPresentProperty, value);
        }
        public static readonly DependencyProperty IsUnreleasedDataPresentProperty =
            DependencyProperty.Register("IsUnreleasedDataPresent", typeof(bool), typeof(SettingsPage), new PropertyMetadata(false));

        public Element TravelerElement { get; set; }

        public UpdateInfo UpdateInfo
        {
            get => (UpdateInfo)this.GetValue(UpdateInfoProperty);
            set => this.SetValue(UpdateInfoProperty, value);
        }
        public static readonly DependencyProperty UpdateInfoProperty =
            DependencyProperty.Register("UpdateInfo", typeof(UpdateInfo), typeof(SettingsPage), new PropertyMetadata(new UpdateInfo()));

        public string VersionString
        {
            get => (string)this.GetValue(VersionStringProperty);
            set => this.SetValue(VersionStringProperty, value);
        }
        public static readonly DependencyProperty VersionStringProperty =
            DependencyProperty.Register("VersionString", typeof(string), typeof(SettingsPage), new PropertyMetadata(""));

        public ApplicationTheme CurrentTheme
        {
            get => (ApplicationTheme)this.GetValue(CurrentThemeProperty);
            set => this.SetValue(CurrentThemeProperty, value);
        }
        public static readonly DependencyProperty CurrentThemeProperty =
            DependencyProperty.Register("CurrentTheme", typeof(ApplicationTheme), typeof(SettingsPage), new PropertyMetadata(null));

        #endregion

        private void UnreleasedCInfoToggled(object sender, RoutedEventArgs e) => SettingService.Instance[Setting.ShowUnreleasedData] = this.IsUnreleasedDataPresent;
        private void TravelerPresentSwitched(object sender, RoutedEventArgs e)
        {
            SettingService.Instance[Setting.PresentTravelerElementType] = ElementHelper.GetElement((RadioButton)sender);
            TravelerPresentService.Instance.SetPresentTraveler();
        }
        private async void UpdateRequested(object sender, RoutedEventArgs e)
        {
            UpdateService.Instance.UpdateInfo = this.UpdateInfo;
            UpdateState u;
            if (((Button)sender).Tag.ToString() == "Github")
            {
                u = UpdateService.Instance.CheckUpdateStateViaGithub();
            }
            else
            {
                u = UpdateService.Instance.CheckUpdateStateViaGitee();
            }

            switch (u)
            {
                case UpdateState.NeedUpdate:
                    UpdateService.Instance.DownloadAndInstallPackage();
                    await this.UpdateDialog.ShowAsync();
                    break;
                case UpdateState.IsNewestRelease:
                    ((Button)sender).Content = "已是最新版";
                    ((Button)sender).IsEnabled = false;
                    break;
                case UpdateState.IsInsiderVersion:
                    ((Button)sender).Content = "内部测试版";
                    ((Button)sender).IsEnabled = false;
                    break;
                case UpdateState.NotAvailable:
                    ((Button)sender).Content = "获取更新失败";
                    ((Button)sender).IsEnabled = false;
                    break;
            }
        }

        private void UpdateCancellationRequested(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args) => UpdateService.Instance.CancelUpdate();

        private void ThemeChangeRequested(object sender, SelectionChangedEventArgs e)
        {
            SettingService.Instance[Setting.AppTheme] = ((ComboBox)sender).SelectedIndex switch
            {
                0 => ApplicationTheme.Light,
                1 => ApplicationTheme.Dark,
                _ => null,
            };
            Func<object, ApplicationTheme?> converter = n => { if (n == null) { return null; } return (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), n.ToString()); };
            ThemeManager.Current.ApplicationTheme = SettingService.Instance.GetOrDefault(Setting.AppTheme, null, converter);
        }
    }
}
