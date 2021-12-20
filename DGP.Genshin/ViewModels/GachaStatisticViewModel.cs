﻿using DGP.Genshin.Common.Core.DependencyInjection;
using DGP.Genshin.Common.Extensions.System;
using DGP.Genshin.Common.Threading;
using DGP.Genshin.MiHoYoAPI.Gacha;
using DGP.Genshin.Services.Abstratcions;
using DGP.Genshin.Services.GachaStatistics;
using DGP.Genshin.Services.GachaStatistics.Statistics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using ModernWpf.Controls;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace DGP.Genshin.ViewModels
{
    [ViewModel(ViewModelType.Transient)]
    public class GachaStatisticViewModel : ObservableObject
    {
        private readonly IGachaStatisticService gachaStatisticService;

        private Statistic? statistic;
        private UidGachaData? selectedUserGachaData;
        private FetchProgress? fetchProgress;
        private SpecificBanner? selectedSpecificBanner;
        private GachaDataCollection userGachaDataCollection = new();
        private bool isFullFetch;
        private IRelayCommand openUICommand;
        private IAsyncRelayCommand gachaLogAutoFindCommand;
        private IAsyncRelayCommand gachaLogManualCommand;
        private IAsyncRelayCommand importFromUIGFJCommand;
        private IAsyncRelayCommand importFromUIGFWCommand;
        private IAsyncRelayCommand exportToUIGFWCommand;
        private IAsyncRelayCommand exportToUIGFJCommand;

        /// <summary>
        /// 当前的统计信息
        /// </summary>
        public Statistic? Statistic
        {
            get => statistic;
            set => SetProperty(ref statistic, value);
        }
        /// <summary>
        /// 当前选择的UID
        /// </summary>
        public UidGachaData? SelectedUserGachaData
        {
            get => selectedUserGachaData;
            set
            {
                SetProperty(ref selectedUserGachaData, value);
                SyncStatisticWithUid();
            }
        }
        public async void SyncStatisticWithUid()
        {
            if (SelectedUserGachaData is null)
            {
                return;
            }
            string? currentUid = Statistic?.Uid;
            string? targetUid = SelectedUserGachaData.Uid;
            if (currentUid != targetUid)
            {
                Statistic = await gachaStatisticService.GetStatisticAsync(UserGachaDataCollection, targetUid);
                SelectedSpecificBanner = Statistic.SpecificBanners?.FirstOrDefault();
            }
        }
        /// <summary>
        /// 所有UID
        /// </summary>
        public GachaDataCollection UserGachaDataCollection
        {
            get => userGachaDataCollection;
            set => SetProperty(ref userGachaDataCollection, value);
        }
        /// <summary>
        /// 当前的获取进度
        /// </summary>
        public FetchProgress? FetchProgress
        {
            get => fetchProgress;
            set => SetProperty(ref fetchProgress, value);
        }
        /// <summary>
        /// 选定的特定池
        /// </summary>
        public SpecificBanner? SelectedSpecificBanner
        {
            get => selectedSpecificBanner;
            set => SetProperty(ref selectedSpecificBanner, value);
        }
        public bool IsFullFetch
        {
            get => isFullFetch;
            set => SetProperty(ref isFullFetch, value);
        }
        public IRelayCommand OpenUICommand
        {
            get => openUICommand;
            [MemberNotNull(nameof(openUICommand))]
            set => SetProperty(ref openUICommand, value);
        }
        public IAsyncRelayCommand GachaLogAutoFindCommand
        {
            get => gachaLogAutoFindCommand;
            [MemberNotNull(nameof(gachaLogAutoFindCommand))]
            set => SetProperty(ref gachaLogAutoFindCommand, value);
        }
        public IAsyncRelayCommand GachaLogManualCommand
        {
            get => gachaLogManualCommand;
            [MemberNotNull(nameof(gachaLogManualCommand))]
            set => SetProperty(ref gachaLogManualCommand, value);
        }
        public IAsyncRelayCommand ImportFromUIGFJCommand
        {
            get => importFromUIGFJCommand;
            [MemberNotNull(nameof(importFromUIGFJCommand))]
            set => SetProperty(ref importFromUIGFJCommand, value);
        }
        public IAsyncRelayCommand ImportFromUIGFWCommand
        {
            get => importFromUIGFWCommand;
            [MemberNotNull(nameof(importFromUIGFWCommand))]
            set => SetProperty(ref importFromUIGFWCommand, value);
        }
        public IAsyncRelayCommand ExportToUIGFWCommand
        {
            get => exportToUIGFWCommand;
            [MemberNotNull(nameof(exportToUIGFWCommand))]
            set => SetProperty(ref exportToUIGFWCommand, value);
        }
        public IAsyncRelayCommand ExportToUIGFJCommand
        {
            get => exportToUIGFJCommand;
            [MemberNotNull(nameof(exportToUIGFJCommand))]
            set => SetProperty(ref exportToUIGFJCommand, value);
        }

        public GachaStatisticViewModel(IGachaStatisticService gachaStatisticService)
        {
            this.gachaStatisticService = gachaStatisticService;
            gachaStatisticService.LoadLocalGachaData(UserGachaDataCollection);

            OpenUICommand = new RelayCommand(() => { SelectedUserGachaData = UserGachaDataCollection.FirstOrDefault(); });
            GachaLogAutoFindCommand = new AsyncRelayCommand(RefreshByAutoFindModeAsync);
            GachaLogManualCommand = new AsyncRelayCommand(RefreshByManualAsync);
            ImportFromUIGFJCommand = new AsyncRelayCommand(ImportFromUIGFJAsync);
            ImportFromUIGFWCommand = new AsyncRelayCommand(ImportFromUIGFWAsync);
            ExportToUIGFWCommand = new AsyncRelayCommand(ExportToUIGFWAsync);
            ExportToUIGFJCommand = new AsyncRelayCommand(ExportToUIGFJAsync);
        }

        private readonly TaskPreventer taskPreventer = new();
        private async Task RefreshByAutoFindModeAsync()
        {
            if (taskPreventer.ShouldExecute)
            {
                (bool isOk, string? uid) = await gachaStatisticService.RefreshAsync(UserGachaDataCollection, GachaLogUrlMode.GameLogFile, OnFetchProgressed, IsFullFetch);
                FetchProgress = null;
                if (isOk)
                {
                    SelectedUserGachaData = UserGachaDataCollection.FirstOrDefault(u => u.Uid == uid);
                }
                taskPreventer.Release();
            }
        }
        private async Task RefreshByManualAsync()
        {
            if (taskPreventer.ShouldExecute)
            {
                (bool isOk, string? uid) = await gachaStatisticService.RefreshAsync(UserGachaDataCollection, GachaLogUrlMode.ManualInput, OnFetchProgressed, IsFullFetch);
                FetchProgress = null;
                if (isOk)
                {
                    SelectedUserGachaData = UserGachaDataCollection.FirstOrDefault(u => u.Uid == uid);
                }
                taskPreventer.Release();
            }
        }
        private void OnFetchProgressed(FetchProgress p)
        {
            FetchProgress = p;
        }
        private async Task ImportFromUIGFJAsync()
        {
            if (taskPreventer.ShouldExecute)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "JS对象简谱文件|*.json",
                    Title = "从 可交换统一格式祈愿记录 Json文件导入",
                    Multiselect = false,
                    CheckFileExists = true
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    await gachaStatisticService.ImportFromUIGFJAsync(UserGachaDataCollection, openFileDialog.FileName);
                }
                taskPreventer.Release();
            }
        }
        private async Task ImportFromUIGFWAsync()
        {
            if (taskPreventer.ShouldExecute)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Excel 工作簿|*.xlsx",
                    Title = "从 可交换统一格式祈愿记录工作簿 文件导入",
                    Multiselect = false,
                    CheckFileExists = true
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    await gachaStatisticService.ImportFromUIGFWAsync(UserGachaDataCollection, openFileDialog.FileName);
                }
                taskPreventer.Release();
            }
        }
        private async Task ExportToUIGFWAsync()
        {
            if (taskPreventer.ShouldExecute)
            {
                if (SelectedUserGachaData is null)
                {
                    return;
                }
                SaveFileDialog dialog = new()
                {
                    Filter = "Excel 工作簿|*.xlsx",
                    Title = "保存到表格",
                    ValidateNames = true,
                    CheckPathExists = true,
                    FileName = $"{SelectedUserGachaData.Uid}.xlsx"
                };
                if (dialog.ShowDialog() == true)
                {
                    this.Log("try to export to excel");
                    await gachaStatisticService.ExportDataToExcelAsync(UserGachaDataCollection, SelectedUserGachaData.Uid, dialog.FileName);
                    await new ContentDialog
                    {
                        Title = "导出祈愿记录完成",
                        Content = $"祈愿记录已导出至 {dialog.SafeFileName}",
                        PrimaryButtonText = "确定",
                        DefaultButton = ContentDialogButton.Primary
                    }.ShowAsync();
                }
                taskPreventer.Release();
            }
        }
        private async Task ExportToUIGFJAsync()
        {
            if (taskPreventer.ShouldExecute)
            {
                if (SelectedUserGachaData is null)
                {
                    return;
                }
                SaveFileDialog dialog = new()
                {
                    Filter = "JS对象简谱文件|*.json",
                    Title = "保存到文件",
                    ValidateNames = true,
                    CheckPathExists = true,
                    FileName = $"{SelectedUserGachaData.Uid}.json"
                };
                if (dialog.ShowDialog() == true)
                {
                    await gachaStatisticService.ExportDataToJsonAsync(UserGachaDataCollection, SelectedUserGachaData.Uid, dialog.FileName);
                    await new ContentDialog
                    {
                        Title = "导出祈愿记录完成",
                        Content = $"祈愿记录已导出至 {dialog.SafeFileName}",
                        PrimaryButtonText = "确定",
                        DefaultButton = ContentDialogButton.Primary
                    }.ShowAsync();
                }
                taskPreventer.Release();
            }
        }
    }
}