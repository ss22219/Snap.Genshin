﻿using DGP.Genshin.Factory.Abstraction;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Mvvm.Input;
using Snap.Core.DependencyInjection;
using Snap.Core.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DGP.Genshin.Factory
{
    [Factory(typeof(IAsyncRelayCommandFactory), InjectAs.Transient)]
    internal class AsyncRelayCommandFactory : IAsyncRelayCommandFactory
    {
        public AsyncRelayCommand<T> Create<T>(Func<T?, Task> execute)
        {
            return this.Register(new AsyncRelayCommand<T>(execute));
        }
        public AsyncRelayCommand<T> Create<T>(Func<T?, CancellationToken, Task> cancelableExecute)
        {
            return this.Register(new AsyncRelayCommand<T>(cancelableExecute));
        }
        public AsyncRelayCommand<T> Create<T>(Func<T?, Task> execute, Predicate<T?> canExecute)
        {
            return this.Register(new AsyncRelayCommand<T>(execute, canExecute));
        }
        public AsyncRelayCommand<T> Create<T>(Func<T?, CancellationToken, Task> cancelableExecute, Predicate<T?> canExecute)
        {
            return this.Register(new AsyncRelayCommand<T>(cancelableExecute, canExecute));
        }
        public AsyncRelayCommand Create(Func<Task> execute)
        {
            return this.Register(new AsyncRelayCommand(execute));
        }
        public AsyncRelayCommand Create(Func<CancellationToken, Task> cancelableExecute)
        {
            return this.Register(new AsyncRelayCommand(cancelableExecute));
        }
        public AsyncRelayCommand Create(Func<Task> execute, Func<bool> canExecute)
        {
            return this.Register(new AsyncRelayCommand(execute, canExecute));
        }
        public AsyncRelayCommand Create(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute)
        {
            return this.Register(new AsyncRelayCommand(cancelableExecute, canExecute));
        }

        private AsyncRelayCommand Register(AsyncRelayCommand command)
        {
            this.ReportException(command);
            return command;
        }
        private AsyncRelayCommand<T> Register<T>(AsyncRelayCommand<T> command)
        {
            this.ReportException(command);
            return command;
        }
        private void ReportException(IAsyncRelayCommand command)
        {
            command.PropertyChanged += (sender, args) =>
            {
                if (sender is IAsyncRelayCommand asyncRelayCommand)
                {
                    if (args.PropertyName == nameof(AsyncRelayCommand.ExecutionTask))
                    {
                        if (asyncRelayCommand.ExecutionTask?.Exception is AggregateException exception)
                        {
                            Crashes.TrackError(exception);
                            this.Log(exception);
                        }
                    }
                }
            };
        }
    }
}
