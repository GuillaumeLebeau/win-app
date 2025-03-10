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

using ProtonVPN.Common.KillSwitch;
using ProtonVPN.Core.Storage;

namespace ProtonVPN.Settings.Migrations.v1_20_0
{
    internal class AppSettingsMigration : BaseAppSettingsMigration
    {
        private const string KillSwitchKey = "KillSwitch";

        public AppSettingsMigration(ISettingsStorage appSettings) :
            base(appSettings, "1.20.0")
        {
        }

        protected override void Migrate()
        {
            bool isKillSwitchOn = Settings.Get<bool>(KillSwitchKey);
            if (isKillSwitchOn)
            {
                Settings.Set("KillSwitchMode", KillSwitchMode.Soft);
            }
        }
    }
}