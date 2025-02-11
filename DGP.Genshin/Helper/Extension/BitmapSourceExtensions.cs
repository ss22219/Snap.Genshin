﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DGP.Genshin.Helper.Extension
{
    public static class BitmapSourceExtensions
    {
        public static Pixel[,] GetPixels(this BitmapSource source)
        {
            try
            {
                PixelFormat format = source.Format;

                if (format != PixelFormats.Bgra32)
                {
                    source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                }
                Pixel[,] pixels = new Pixel[source.PixelWidth, source.PixelHeight];
                GCHandle pinnedPixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                source.CopyPixels(
                    new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
                    pinnedPixels.AddrOfPinnedObject(),
                    pixels.GetLength(0) * pixels.GetLength(1) * 4,
                    source.PixelWidth * ((source.Format.BitsPerPixel + 7) / 8));
                pinnedPixels.Free();
                return pixels;
            }
            catch (Exception ex)
            {
                Snap.Core.Logging.Logger.LogStatic(ex);
                throw;
            }

        }
    }
}
