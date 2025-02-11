﻿using DGP.Genshin.Message;
using Microsoft.Toolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace DGP.Genshin.Control
{
    /// <summary>
    /// 等待下载视图
    /// 防止用户与UI交互
    /// </summary>
    public sealed partial class BlockingView : UserControl, IRecipient<ImageHitBeginMessage>, IRecipient<ImageHitEndMessage>
    {
        private static readonly DependencyProperty ShouldPresentProperty =
            DependencyProperty.Register(nameof(ShouldPresent), typeof(bool), typeof(BlockingView), new PropertyMetadata(false));

        /// <summary>
        /// 构造一个新的视图实例
        /// </summary>
        public BlockingView()
        {
            this.DataContext = this;
            this.InitializeComponent();

            App.Messenger.RegisterAll(this);
        }

        /// <summary>
        /// 释放消息器
        /// </summary>
        ~BlockingView()
        {
            App.Messenger.UnregisterAll(this);
        }

        /// <summary>
        /// 指示当前视图是否可见
        /// </summary>
        public bool ShouldPresent
        {
            get => (bool)this.GetValue(ShouldPresentProperty);

            set => this.SetValue(ShouldPresentProperty, value);
        }

        /// <summary>
        /// 接收图像开始下载消息
        /// </summary>
        /// <param name="message">图像下载消息</param>
        void IRecipient<ImageHitBeginMessage>.Receive(ImageHitBeginMessage message)
        {
            this.ShouldPresent = true;
        }

        /// <summary>
        /// 接收图像下载完成消息
        /// </summary>
        /// <param name="message">图像下载消息</param>
        void IRecipient<ImageHitEndMessage>.Receive(ImageHitEndMessage message)
        {
            this.ShouldPresent = false;
        }
    }
}