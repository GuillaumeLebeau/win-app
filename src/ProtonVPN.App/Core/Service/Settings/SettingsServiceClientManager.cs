﻿/*
 * Copyright (c) 2021 Proton Technologies AG
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

using System;
using System.ComponentModel;
using System.ServiceModel;
using System.Threading.Tasks;
using ProtonVPN.Common.Extensions;
using ProtonVPN.Common.KillSwitch;
using ProtonVPN.Common.Logging;
using ProtonVPN.Core.Settings;
using ProtonVPN.Service.Contract.Settings;

namespace ProtonVPN.Core.Service.Settings
{
    public class SettingsServiceClientManager : ISettingsServiceClientManager, ISettingsAware
    {
        private readonly SettingsServiceClient _client;
        private readonly ILogger _logger;
        private readonly SettingsContractProvider _settingsContractProvider;

        public SettingsServiceClientManager(
            SettingsServiceClient client,
            ILogger logger,
            SettingsContractProvider settingsContractProvider)
        {
            _settingsContractProvider = settingsContractProvider;
            _client = client;
            _logger = logger;
        }

        public async Task UpdateServiceSettings()
        {
            await UpdateServiceSettingsInternal(_settingsContractProvider.GetSettingsContract());
        }

        public async Task DisableKillSwitch()
        {
            SettingsContract settingsContract = _settingsContractProvider.GetSettingsContract();
            settingsContract.KillSwitchMode = KillSwitchMode.Off;
            await UpdateServiceSettingsInternal(settingsContract);
        }

        public async Task EnableHardKillSwitch()
        {
            SettingsContract settingsContract = _settingsContractProvider.GetSettingsContract();
            settingsContract.KillSwitchMode = KillSwitchMode.Hard;
            await UpdateServiceSettingsInternal(settingsContract);
        }

        private async Task UpdateServiceSettingsInternal(SettingsContract settingsContract)
        {
            try
            {
                await Task.Run(() => _client.Apply(settingsContract));
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is TaskCanceledException)
            {
                _logger.Error(ex.CombinedMessage());
            }
        }

        public async void OnAppSettingsChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAppSettings.KillSwitchMode) ||
                e.PropertyName == nameof(IAppSettings.VpnAcceleratorEnabled) ||
                e.PropertyName == nameof(IAppSettings.OvpnProtocol) ||
                e.PropertyName == nameof(IAppSettings.NetworkAdapterType) ||
                e.PropertyName == nameof(IAppSettings.NetShieldMode) ||
                e.PropertyName == nameof(IAppSettings.NetShieldEnabled) ||
                e.PropertyName == nameof(IAppSettings.Ipv6LeakProtection))
            {
                _logger.Info($"Setting \"{e.PropertyName}\" changed");
                await UpdateServiceSettings();
            }
        }
    }
}