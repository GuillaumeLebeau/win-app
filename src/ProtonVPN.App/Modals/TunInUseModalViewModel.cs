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

using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using ProtonVPN.Common.Networking;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Settings;

namespace ProtonVPN.Modals
{
    public class TunInUseModalViewModel : BaseModalViewModel
    {
        private readonly IAppSettings _appSettings;
        private readonly IVpnManager _vpnManager;

        public TunInUseModalViewModel(IAppSettings appSettings, IVpnManager vpnManager)
        {
            _vpnManager = vpnManager;
            _appSettings = appSettings;
            SwitchToTapCommand = new RelayCommand(SwitchToTapAction);
        }

        public ICommand SwitchToTapCommand { get; }

        private async void SwitchToTapAction()
        {
            TryClose();
            _appSettings.NetworkAdapterType = OpenVpnAdapter.Tap;
            await _vpnManager.ReconnectAsync();
        }
    }
}