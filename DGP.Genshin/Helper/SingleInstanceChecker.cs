﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DGP.Genshin.Helper
{
    /// <summary>
    /// 单例程序检查器
    /// </summary>
    internal class SingleInstanceChecker
    {
        private readonly string uniqueEventName;
        private EventWaitHandle? eventWaitHandle;

        /// <summary>
        /// 构造新的检测器实例
        /// </summary>
        /// <param name="uniqueEventName">App的唯一名称标识符</param>
        public SingleInstanceChecker(string uniqueEventName)
        {
            this.uniqueEventName = uniqueEventName;
        }

        /// <summary>
        /// 指示是否由于单例限制而退出
        /// </summary>
        public bool IsExitDueToSingleInstanceRestriction { get; set; }

        /// <summary>
        /// 指示是否在进行验证
        /// </summary>
        public bool IsEnsureingSingleInstance { get; set; }

        /// <summary>
        /// 确保应用程序是否为第一个打开
        /// </summary>
        /// <param name="app"></param>
        public void Ensure(Application app, Action multiInstancePresentAction)
        {
            // check if it is already open.
            try
            {
                this.IsEnsureingSingleInstance = true;
                // try to open it - if another instance is running, it will exist , if not it will throw
                this.eventWaitHandle = EventWaitHandle.OpenExisting(this.uniqueEventName);
                // Notify other instance so it could bring itself to foreground.
                this.eventWaitHandle.Set();
                // Terminate this instance.
                this.IsExitDueToSingleInstanceRestriction = true;
                app.Shutdown();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // listen to a new event (this app instance will be the new "master")
                this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, this.uniqueEventName);
            }
            finally
            {
                this.IsEnsureingSingleInstance = false;
            }
            new Task(() =>
            {
                // if this instance gets the signal
                while (this.eventWaitHandle.WaitOne())
                {
                    App.Current.Dispatcher.Invoke(multiInstancePresentAction);
                }
            }).Start();
        }
    }
}
