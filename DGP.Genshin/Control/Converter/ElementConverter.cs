﻿using DGP.Genshin.DataModel.Helper;
using Microsoft;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DGP.Genshin.Control.Converter
{
    /// <summary>
    /// 将 英文元素名称 转换到 图标Url
    /// </summary>
    public sealed class ElementConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ElementHelper.FromENGName((string)value);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw Assumes.NotReachable();
        }
    }
}