﻿using DGP.Genshin.Message;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.Threading;
using Snap.Core.Logging;
using Snap.Data.Utility.Extension;
using Snap.Threading;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DGP.Genshin.Control.Infrastructure.CachedImage
{
    /// <summary>
    /// 实现了图片缓存，
    /// 用来在 <see cref="Border"/> 上设置异步设置 <see cref="Border.Background"/> 与相应的 <see cref="ImageBrush.Stretch"/>，
    /// 因为只有 <see cref="Border"/> 可以较为便捷的设置角落弧度 <see cref="Border.CornerRadius"/>
    /// </summary>
    public class ImageAsyncHelper
    {
        private static readonly DependencyProperty ImageUrlProperty = Property<ImageAsyncHelper>.Attach<string>("ImageUrl", HitImageCallback);
        private static readonly DependencyProperty StretchModeProperty = Property<ImageAsyncHelper>.Attach("StretchMode", Stretch.Uniform);

        // 发送消息细粒度锁
        private static readonly SemaphoreSlim SendMessageLocker = new(1, 1);

        private static int imageUrlHittingCount = 0;

        /// <summary>
        /// 获取图片拉伸
        /// </summary>
        /// <param name="border">待获取的 <see cref="Border"/></param>
        /// <returns>图片拉伸</returns>
        public static Stretch GetStretchMode(Border border)
        {
            return (Stretch)border.GetValue(StretchModeProperty);
        }

        /// <summary>
        /// 设置图片拉伸
        /// </summary>
        /// <param name="border">待设置的 <see cref="Border"/></param>
        /// <param name="value">新的图片拉伸</param>
        public static void SetStretchMode(Border border, Stretch value)
        {
            border.SetValue(StretchModeProperty, value);
        }

        /// <summary>
        /// 获取图片链接
        /// </summary>
        /// <param name="border">待获取的 <see cref="Border"/></param>
        /// <returns>图片链接</returns>
        public static string GetImageUrl(Border border)
        {
            return (string)border.GetValue(ImageUrlProperty);
        }

        /// <summary>
        /// 设置图片链接
        /// </summary>
        /// <param name="border">待设置的 <see cref="Border"/></param>
        /// <param name="value">新的图片链接</param>
        public static void SetImageUrl(Border border, Uri value)
        {
            border.SetValue(ImageUrlProperty, value);
        }

        private static void HitImageCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            HitImageAsync((Border)obj, (string)e.NewValue).Forget();
        }

        private static async Task HitImageAsync(Border border, string url)
        {
            MemoryStream? memoryStream;
            if (FileCache.Exists(url))
            {
                memoryStream = await FileCache.HitAsync(url);
            }
            else
            {
                ++imageUrlHittingCount;
                using (await SendMessageLocker.EnterAsync())
                {
                    // 不存在图片，所以需要下载额外的资源
                    if (imageUrlHittingCount == 1)
                    {
                        App.Messenger.Send(new ImageHitBeginMessage());
                    }

                    Logger.LogStatic(imageUrlHittingCount);
                }

                memoryStream = await FileCache.HitAsync(url);

                --imageUrlHittingCount;
                using (await SendMessageLocker.EnterAsync())
                {
                    if (imageUrlHittingCount == 0)
                    {
                        App.Messenger.Send(new ImageHitEndMessage());
                    }

                    Logger.LogStatic(imageUrlHittingCount);
                }
            }

            if (memoryStream != null)
            {
                BitmapImage bitmapImage = new();
                try
                {
                    using (bitmapImage.AsDisposableInit())
                    {
                        bitmapImage.StreamSource = memoryStream;
                    }
                }
                catch
                {
                }

                border.Background = new ImageBrush()
                {
                    ImageSource = bitmapImage,
                    Stretch = GetStretchMode(border),
                };
            }
        }
    }
}