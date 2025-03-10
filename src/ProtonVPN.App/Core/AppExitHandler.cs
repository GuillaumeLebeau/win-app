﻿/*
 * Copyright (c) 2020 Proton Technologies AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Threading.Tasks;
using System.Windows;
using ProtonVPN.Common.Vpn;
using ProtonVPN.Core.Modals;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;
using ProtonVPN.Core.Vpn;
using ProtonVPN.Modals;

namespace ProtonVPN.Core
{
    public class AppExitHandler : IVpnStateAware, IServiceSettingsStateAware
    {
        private readonly IModals _modals;
        private readonly VpnService _vpnService;
        private VpnStatus _lastVpnStatus = VpnStatus.Disconnected;
        private bool _isNetworkBlocked;

        public AppExitHandler(IModals modals, VpnService vpnService)
        {
            _vpnService = vpnService;
            _modals = modals;
        }

        public bool PendingExit { get; private set; }

        public void RequestExit()
        {
            if (ShowModal())
            {
                bool? result = _modals.Show<ExitModalViewModel>();
                if (result == false)
                {
                    return;
                }
            }

            PendingExit = true;

            _vpnService.UnRegisterCallback();
            Application.Current.Shutdown();
        }

        public Task OnVpnStateChanged(VpnStateChangedEventArgs e)
        {
            _lastVpnStatus = e.State.Status;
            _isNetworkBlocked = e.NetworkBlocked;
            return Task.CompletedTask;
        }

        public void OnServiceSettingsStateChanged(ServiceSettingsStateChangedEventArgs e)
        {
            _isNetworkBlocked = e.IsNetworkBlocked;
        }

        private bool ShowModal()
        {
            return (_lastVpnStatus != VpnStatus.Disconnected &&
                    _lastVpnStatus != VpnStatus.Disconnecting) ||
                   _isNetworkBlocked;
        }
    }
}