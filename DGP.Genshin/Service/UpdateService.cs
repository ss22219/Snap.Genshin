﻿using DGP.Genshin.Core.Notification;
using DGP.Genshin.DataModel.Updating;
using DGP.Genshin.Helper;
using DGP.Genshin.Helper.Extension;
using DGP.Genshin.Message;
using DGP.Genshin.Service.Abstraction.Setting;
using DGP.Genshin.Service.Abstraction.Updating;
using Microsoft;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualStudio.Threading;
using Octokit;
using Snap.Core.DependencyInjection;
using Snap.Data.Json;
using Snap.Data.Utility;
using Snap.Net.Download;
using Snap.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace DGP.Genshin.Service
{
    [Service(typeof(IUpdateService), InjectAs.Singleton)]
    internal class UpdateService : IUpdateService
    {
        private const string UpdateNotificationTag = "snap_genshin_update";
        private const string UpdaterExecutable = "DGP.Genshin.Updater.exe";
        private const string UpdaterFolder = "Updater";

        private readonly IMessenger messenger;
        private NotificationUpdater? notificationUpdater;

        public Uri? PackageUri { get; private set; }
        public Version? NewVersion { get; private set; }
        public string? ReleaseNote { get; private set; }
        public Version CurrentVersion
        {
            get => App.Current.Version;
        }

        private Downloader? InnerDownloader { get; set; }

        public UpdateService(IMessenger messenger)
        {
            this.messenger = messenger;
        }

        public async Task<UpdateState> CheckUpdateStateAsync()
        {
            try
            {
                IUpdateChecker updateChecker = Setting2.UpdateAPI.Get() switch
                {
                    UpdateAPI.PatchAPI => new PatchUpdateChecker(),
                    UpdateAPI.GithubAPI => new GithubUpdateChecker(),
                    _ => throw Assumes.NotReachable()
                };

                UpdateInfomation? info = await updateChecker.GetUpdateInfomationAsync();
                if (info != null)
                {
                    this.ReleaseNote = info.ReleaseNote;
                    this.PackageUri = info.PackageUrl;
                    this.NewVersion = new Version(info.Version);
                }
                else
                {
                    return UpdateState.NotAvailable;
                }

                if (Debugger.IsAttached)
                {
                    return UpdateState.NeedUpdate;
                }
                else
                {
                    if (this.NewVersion > this.CurrentVersion)
                    {
                        return UpdateState.NeedUpdate;
                    }
                    else
                    {
                        if (this.NewVersion == this.CurrentVersion)
                        {
                            return UpdateState.IsNewestRelease;
                        }
                        else
                        {
                            return UpdateState.IsInsiderVersion;
                        }
                    }
                }
            }
            catch
            {
                return UpdateState.NotAvailable;
            }
        }

        private readonly TaskPreventer updateTaskPreventer = new();

        public async Task DownloadAndInstallPackageAsync()
        {
            if (this.updateTaskPreventer.ShouldExecute)
            {
                string destinationPath = PathContext.Locate("Package.zip");

                Requires.NotNull(this.PackageUri!, nameof(this.PackageUri));
                Requires.NotNull(this.NewVersion!, nameof(this.NewVersion));

                this.InnerDownloader = new(this.PackageUri, destinationPath);
                this.notificationUpdater = new(this.NewVersion.ToString(), this.messenger);
                IProgress<DownloadInfomation> progress = new Progress<DownloadInfomation>(this.notificationUpdater.OnProgressChanged);
                //toast can only be shown & updated by main thread
                this.notificationUpdater.ShowDownloadToastNotification();

                bool caught = false;
                try
                {
                    await this.InnerDownloader.DownloadAsync(progress);
                }
                catch
                {
                    caught = true;
                }
                finally
                {
                    this.messenger.Send(UpdateProgressedMessage.Default);
                }

                if (caught)
                {
                    new ToastContentBuilder()
                    .AddText("下载更新时遇到问题")
                    .AddText("点击检查更新再次尝试")
                    .SafeShow();
                }
                else
                {
                    this.StartInstallUpdate();
                }
                this.updateTaskPreventer.Release();
            }
        }

        /// <summary>
        /// 开始安装更新
        /// </summary>
        private void StartInstallUpdate()
        {
            Directory.CreateDirectory(PathContext.Locate(UpdaterFolder));
            PathContext.MoveToFolderOrIgnore(UpdaterExecutable, UpdaterFolder);
            string oldUpdaterPath = PathContext.Locate(UpdaterFolder, UpdaterExecutable);

            if (File.Exists(oldUpdaterPath))
            {
                try
                {
                    //Updater自带工作路径纠正
                    Process.Start(new ProcessStartInfo()
                    {
                        //fix auth exception
                        Verb = "runas",
                        UseShellExecute = true,
                        FileName = oldUpdaterPath,
                        Arguments = "UpdateInstall"
                    });
                    App.Current.Dispatcher.Invoke(App.Current.Shutdown);
                }
                catch (Win32Exception)
                {
                    new ToastContentBuilder()
                    .AddText("已经取消更新")
                    .AddText("下次更新需要重新下载安装包")
                    .SafeShow();
                }
            }
            else
            {
                new ToastContentBuilder()
                .AddText("在默认路径上未找到更新器")
                .AddText("请尝试手动解压安装包更新")
                .SafeShow();
            }
        }

        /// <summary>
        /// Github API 更新检查器实现
        /// </summary>
        internal class GithubUpdateChecker : IUpdateChecker
        {
            /// <summary>
            /// because repo cant cantain original token string
            /// so we store base64 encoded value here
            /// https://github.com/settings/tokens
            /// </summary>
            internal class GithubToken : Base64Converter
            {
                private GithubToken() { }
                public static string GetToken()
                {
                    return Base64Decode(Encoding.UTF8, "Z2hwX3lDRWdVTVNaNnRRV2JpNjZMUWYyTUprbWFQVFI3bTEwYkVnTw==");
                }
            }
            public async Task<UpdateInfomation?> GetUpdateInfomationAsync()
            {
                try
                {
                    GitHubClient client = new(new ProductHeaderValue("SnapGenshin"))
                    {
                        Credentials = new Credentials(GithubToken.GetToken()),
                    };
                    Release? release = await client.Repository.Release.GetLatest("DGP-Studio", "Snap.Genshin");
                    return new()
                    {
                        ReleaseNote = release.Body,
                        PackageUrl = new Uri(release.Assets[0].BrowserDownloadUrl),
                        Version = release.TagName
                    };
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Patch API 更新检查器实现
        /// </summary>
        internal class PatchUpdateChecker : IUpdateChecker
        {
            public async Task<UpdateInfomation?> GetUpdateInfomationAsync()
            {
                return await Json.FromWebsiteAsync<UpdateInfomation>("https://patch.snapgenshin.com/getPatch");
            }
        }

        /// <summary>
        /// 通知更新器
        /// </summary>
        internal class NotificationUpdater
        {
            private NotificationUpdateResult lastNotificationUpdateResult = NotificationUpdateResult.Succeeded;
            private readonly string progressTitle;
            private readonly IMessenger messenger;

            public NotificationUpdater(string progressTitle, IMessenger messenger)
            {
                this.progressTitle = progressTitle;
                this.messenger = messenger;
            }

            /// <summary>
            /// 显示下载进度通知
            /// </summary>
            internal void ShowDownloadToastNotification()
            {
                this.lastNotificationUpdateResult = NotificationUpdateResult.Succeeded;
                new ToastContentBuilder()
                    .AddText("下载更新中...")
                    .AddVisualChild(new AdaptiveProgressBar()
                    {
                        Title = progressTitle,
                        Value = new BindableProgressBarValue("progressValue"),
                        ValueStringOverride = new BindableString("progressValueString"),
                        Status = new BindableString("progressStatus")
                    })
                    .SafeShow(toast =>
                    {
                        toast.Tag = UpdateNotificationTag;
                        toast.Data = new(new Dictionary<string, string>()
                        {
                        {"progressValue", "0" },
                        {"progressValueString", "0% - 0MB / 0MB" },
                        {"progressStatus", "下载中..." }
                        }, 0);
                    });
            }

            /// <summary>
            /// 进度更新
            /// </summary>
            /// <param name="totalBytesToReceive">总大小</param>
            /// <param name="bytesReceived">下载的大小</param>
            /// <param name="percent">进度</param>
            internal void OnProgressChanged(DownloadInfomation downloadInfomation)
            {
                //message will be sent anyway.
                string valueString = downloadInfomation.ToString();
                this.messenger.Send(new UpdateProgressedMessage(downloadInfomation));
                //if user has dismissed the notification, we don't update it anymore
                if (this.lastNotificationUpdateResult is NotificationUpdateResult.Succeeded)
                {
                    //notification could only be updated by main thread.
                    this.ExecuteOnUI(() => this.UpdateNotificationValue(downloadInfomation));
                }
            }

            /// <summary>
            /// 更新下载进度通知上的进度条
            /// </summary>
            /// <param name="totalBytesToReceive">总大小</param>
            /// <param name="bytesReceived">下载的大小</param>
            /// <param name="percent">进度</param>
            private void UpdateNotificationValue(DownloadInfomation downloadInfomation)
            {
                NotificationData data = new() { SequenceNumber = 0 };

                data.Values["progressValue"] = $"{downloadInfomation.Percent}";
                data.Values["progressValueString"] = downloadInfomation.ToString();
                if (!downloadInfomation.IsDownloading)
                {
                    data.Values["progressStatus"] = "下载完成";
                }
                // Update the existing notification's data
                this.lastNotificationUpdateResult = ToastNotificationManagerCompat
                    .CreateToastNotifier()
                    .Update(data, UpdateNotificationTag);
            }
        }
    }
}
