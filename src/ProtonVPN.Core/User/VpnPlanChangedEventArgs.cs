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

using ProtonVPN.Common.Extensions;

namespace ProtonVPN.Core.User
{
    public class VpnPlanChangedEventArgs
    {
        public string OldVpnPlan { get; }
        public string NewVpnPlan { get; }
        public bool IsDowngrade { get; }

        public VpnPlanChangedEventArgs(string oldVpnPlan, string newVpnPlan)
        {
            OldVpnPlan = oldVpnPlan;
            NewVpnPlan = newVpnPlan;
            IsDowngrade = !oldVpnPlan.IsNullOrEmpty() &&
                          oldVpnPlan != newVpnPlan && (
                              (newVpnPlan == "free") ||
                              (newVpnPlan == "vpnbasic" && oldVpnPlan != "free"));
        }
    }
}
