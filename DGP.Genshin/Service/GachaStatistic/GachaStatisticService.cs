﻿using DGP.Genshin.DataModel.GachaStatistic;
using DGP.Genshin.MiHoYoAPI.Gacha;
using DGP.Genshin.Service.Abstraction.GachaStatistic;
using DGP.Genshin.ViewModel;
using ModernWpf.Controls;
using Snap.Core.DependencyInjection;
using Snap.Data.Primitive;
using System;
using System.Threading.Tasks;

namespace DGP.Genshin.Service.GachaStatistic
{
    /// <summary>
    /// 抽卡记录服务
    /// </summary>
    [Service(typeof(IGachaStatisticService), InjectAs.Transient)]
    internal class GachaStatisticService : IGachaStatisticService
    {
        private readonly LocalGachaLogWorker localGachaLogWorker;
        private readonly MetadataViewModel metadataViewModel;

        public GachaStatisticService(MetadataViewModel metadataViewModel)
        {
            this.metadataViewModel = metadataViewModel;
            this.localGachaLogWorker = new();
        }

        public async Task LoadLocalGachaDataAsync(GachaDataCollection gachaData)
        {
            await this.localGachaLogWorker.LoadAllAsync(gachaData);
        }

        public async Task<Result<bool, string>> RefreshAsync(GachaDataCollection gachaData, GachaLogUrlMode mode, IProgress<FetchProgress> progress, bool full = false)
        {
            (bool isOk, string? url) = await GachaLogUrlProvider.GetUrlAsync(mode);
            //获取成功
            if (isOk)
            {
                IGachaLogWorker worker = new GachaLogWorker(url, gachaData);
                (bool isSuccess, string uid) = await this.FetchGachaLogsAsync(gachaData, worker, progress, full);

                if (!isSuccess)
                {
                    await new ContentDialog()
                    {
                        Title = "获取祈愿配置信息失败",
                        Content = "可能是验证密钥已过期",
                        PrimaryButtonText = "确定",
                        DefaultButton = ContentDialogButton.Primary
                    }.ShowAsync();
                }
                return new(isSuccess, uid);
            }

            if (mode != GachaLogUrlMode.ManualInput)
            {
                await new ContentDialog()
                {
                    Title = "获取祈愿记录失败",
                    Content = this.GetUrlFailHintByMode(mode),
                    PrimaryButtonText = "确定",
                    DefaultButton = ContentDialogButton.Primary
                }.ShowAsync();
            }

            return new(false, null!);
        }

        private string GetUrlFailHintByMode(GachaLogUrlMode mode)
        {
            return mode switch
            {
                GachaLogUrlMode.GameLogFile => "请在游戏中打开祈愿历史记录页面后尝试刷新",
                GachaLogUrlMode.ManualInput => "请重新输入有效的Url",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 获取祈愿记录
        /// </summary>
        /// <param name="worker">工作器对象</param>
        /// <param name="full">是否增量获取</param>
        /// <returns>是否获取成功</returns>
        private async Task<Result<bool, string>> FetchGachaLogsAsync(GachaDataCollection gachaData, IGachaLogWorker worker, IProgress<FetchProgress> progress, bool full = false)
        {
            Config? gachaConfigTypes = await worker.GetCurrentGachaConfigAsync();
            if (gachaConfigTypes?.Types != null)
            {
                string? uid = null;
                foreach (ConfigType pool in gachaConfigTypes.Types)
                {
                    uid = full
                        ? await worker.FetchGachaLogAggressivelyAsync(pool, progress)
                        : await worker.FetchGachaLogIncreaselyAsync(pool, progress);

                    if (worker.IsFetchDelayEnabled)
                    {
                        await Task.Delay(worker.GetRandomDelay());
                    }
                }

                if (uid != null)
                {
                    this.localGachaLogWorker.SaveAll(gachaData);
                    return new(true, uid);
                }
            }
            return new(false, null!);
        }

        public Statistic GetStatistic(GachaDataCollection gachaData, string uid)
        {
            return new StatisticBuilder(this.metadataViewModel).ToStatistic(gachaData[uid]!, uid);
        }

        #region Im/Export
        public async Task ExportDataToExcelAsync(GachaDataCollection gachaData, string uid, string path)
        {
            await Task.Run(() => this.localGachaLogWorker!.ExportToUIGFW(uid, path, gachaData));
        }

        public async Task ExportDataToJsonAsync(GachaDataCollection gachaData, string uid, string path)
        {
            await Task.Run(() => this.localGachaLogWorker!.ExportToUIGFJ(uid, path, gachaData));
        }

        public async Task<Result<bool, string>> ImportFromUIGFWAsync(GachaDataCollection gachaData, string path)
        {
            return await this.localGachaLogWorker!.ImportFromUIGFWAsync(path, gachaData);
        }

        public async Task<Result<bool, string>> ImportFromUIGFJAsync(GachaDataCollection gachaData, string path)
        {
            return await this.localGachaLogWorker!.ImportFromUIGFJAsync(path, gachaData);
        }
        #endregion
    }
}