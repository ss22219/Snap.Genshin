﻿using DGP.Genshin.Helper.Notification;
using DGP.Genshin.Message;
using DGP.Genshin.MiHoYoAPI.GameRole;
using DGP.Genshin.MiHoYoAPI.Sign;
using DGP.Genshin.Service.Abstraction;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualStudio.Threading;
using Snap.Core.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DGP.Genshin.Service
{
    [Service(typeof(ISignInService),InjectAs.Singleton)]
    internal class SignInService : IRecipient<DayChangedMessage>, ISignInService
    {
        private readonly ICookieService cookieService;
        private readonly IMessenger messenger;

        public SignInService(ICookieService cookieService, IMessenger messenger)
        {
            this.messenger = messenger;
            this.cookieService = cookieService;

            messenger.RegisterAll(this);
        }
        ~SignInService()
        {
            messenger.UnregisterAll(this);
        }

        public async Task TrySignAllAccountsRolesInAsync()
        {
            using (await cookieService.CookiesLock.ReadLockAsync())
            {
                foreach (string cookie in cookieService.Cookies)
                {
                    List<UserGameRole> roles = await new UserGameRoleProvider(cookie).GetUserGameRolesAsync();
                    foreach (UserGameRole role in roles)
                    {
                        SignInResult? result = await new SignInProvider(cookie).SignInAsync(role);

                        Setting2.LastAutoSignInTime.Set(DateTime.UtcNow);
                        bool isSignInSilently = Setting2.SignInSilently.Get();
                        SecureToastNotificationContext.TryCatch(() =>
                        new ToastContentBuilder()
                            .AddHeader("SIGNIN", "米游社每日签到", "SIGNIN")
                            .AddText(result is null ? "签到失败" : "签到成功")
                            .AddAttributionText(role.ToString())
                            .Show(toast => { toast.SuppressPopup = isSignInSilently; }));
                    }
                }
            }
        }

        public void Receive(DayChangedMessage message)
        {
            TrySignAllAccountsRolesInAsync().Forget();
        }
    }
}
