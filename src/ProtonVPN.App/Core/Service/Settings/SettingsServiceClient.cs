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

using ProtonVPN.Service.Contract.Settings;

namespace ProtonVPN.Core.Service.Settings
{
    public class SettingsServiceClient
    {
        private const string Endpoint = "protonvpn-service/settings";
        private readonly ServiceChannelFactory _channelFactory;

        public SettingsServiceClient(ServiceChannelFactory channelFactory)
        {
            _channelFactory = channelFactory;
        }

        public void Apply(SettingsContract settings)
        {
            using ServiceChannel<ISettingsContract> channel = _channelFactory.Create<ISettingsContract>(Endpoint);
            channel.Proxy.Apply(settings);
        }
    }
}