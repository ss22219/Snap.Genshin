﻿using DGP.Genshin.Core.Background.Abstraction;
using DGP.Genshin.Helper;
using DGP.Genshin.Helper.Extension;
using DGP.Genshin.Message;
using DGP.Genshin.Service.Abstraction.Setting;
using Microsoft;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.Threading;
using ModernWpf;
using ModernWpf.Media.Animation;
using Snap.Core.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace DGP.Genshin.Core.Background
{
    /// <summary>
    /// 背景图片加载器
    /// </summary>
    internal class BackgroundLoader : IRecipient<BackgroundOpacityChangedMessage>, IRecipient<BackgroundChangeRequestMessage>
    {
        private const int AnimationDuration = 500;

        private readonly MainWindow mainWindow;
        private readonly IMessenger messenger;

        /// <summary>
        /// 构造一个新的背景图片加载器
        /// </summary>
        /// <param name="mainWindow">要操作的主窗体引用</param>
        /// <param name="messenger">消息器</param>
        public BackgroundLoader(MainWindow mainWindow, IMessenger messenger)
        {
            this.mainWindow = mainWindow;
            this.messenger = messenger;
            messenger.RegisterAll(this);
        }

        /// <summary>
        /// 释放消息器
        /// </summary>
        ~BackgroundLoader()
        {
            this.messenger.UnregisterAll(this);
        }

        private double Lightness { get; set; }

        /// <inheritdoc/>
        void IRecipient<BackgroundOpacityChangedMessage>.Receive(BackgroundOpacityChangedMessage message)
        {
            if (this.mainWindow.BackgroundGrid.Background is ImageBrush brush)
            {
                brush.Opacity = message.Value;
            }
        }

        /// <inheritdoc/>
        void IRecipient<BackgroundChangeRequestMessage>.Receive(BackgroundChangeRequestMessage message)
        {
            try
            {
                this.LoadNextWallpaperAsync().Forget();
            }
            catch (Exception ex)
            {
                this.Log(ex);
            }
        }

        /// <summary>
        /// 尝试加载下一张壁纸
        /// </summary>
        /// <returns>任务</returns>
        public async Task LoadNextWallpaperAsync()
        {
            IBackgroundProvider? backgroundProvider = App.Current.SwitchableImplementationManager
                .CurrentBackgroundProvider!.Factory.Value;
            BitmapImage? image = await backgroundProvider.GetNextBitmapImageAsync();
            if (image != null)
            {
                this.TrySetTargetAdaptiveBackgroundOpacityValue(image);

                // first pic
                if (this.mainWindow.BackgroundGrid.Background is null)
                {
                    // 直接设置背景
                    this.mainWindow.BackgroundGrid.Background = new ImageBrush
                    {
                        ImageSource = image,
                        Stretch = Stretch.UniformToFill,
                        Opacity = Setting2.BackgroundOpacity,
                    };
                }
                else
                {
                    Grid backgroundPresenter = this.mainWindow.BackgroundGrid;
                    DoubleAnimation fadeOutAnimation = AnimationHelper.CreateAnimation<CubicBezierEase>(0, AnimationDuration);

                    // Fade out old image
                    backgroundPresenter.Background.BeginAnimation(Brush.OpacityProperty, fadeOutAnimation);
                    await Task.Delay(AnimationDuration);
                    backgroundPresenter.Background.BeginAnimation(Brush.OpacityProperty, null);

                    backgroundPresenter.Background = new ImageBrush
                    {
                        ImageSource = image,
                        Stretch = Stretch.UniformToFill,
                        Opacity = 0,
                    };

                    DoubleAnimation fadeInAnimation = AnimationHelper.CreateAnimation<CubicBezierEase>(Setting2.BackgroundOpacity, AnimationDuration);

                    // Fade in new image
                    backgroundPresenter.Background.BeginAnimation(Brush.OpacityProperty, fadeInAnimation);
                    await Task.Delay(AnimationDuration);
                    backgroundPresenter.Background.BeginAnimation(Brush.OpacityProperty, null);

                    backgroundPresenter.Background.Opacity = Setting2.BackgroundOpacity;
                    this.messenger.Send(new AdaptiveBackgroundOpacityChangedMessage(Setting2.BackgroundOpacity));
                }
            }
        }

        /// <summary>
        /// 尝试设置自适应背景图片不透明度
        /// 当未开启选项时自动跳过
        /// TODO: remove heavy work that blocks UI thread.
        /// </summary>
        /// <param name="image">背景图片</param>
        private void TrySetTargetAdaptiveBackgroundOpacityValue(BitmapImage image)
        {
            if (Setting2.IsBackgroundOpacityAdaptive)
            {
                // this operation is really laggy
                this.Lightness = image.GetPixels()
                    .Cast<Pixel>()
                    .Select(p => ((p.Red * 0.299) + (p.Green * 0.587) + (p.Blue * 0.114)) * (p.Alpha / 255D) / 255)
                    .Average();

                this.Log($"Lightness: {this.Lightness}");

                bool isDarkMode = ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark;
                double targetOpacity = isDarkMode ? (1 - this.Lightness) * 0.4 : this.Lightness * 0.6;
                this.Log($"Adjust BackgroundOpacity to {targetOpacity}");
                Setting2.BackgroundOpacity.Set(targetOpacity);
            }
        }
    }
}