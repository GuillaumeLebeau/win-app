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
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using ProtonVPN.Common;
using ProtonVPN.Common.Extensions;
using ProtonVPN.Common.Helpers;
using ProtonVPN.Common.Logging;
using ProtonVPN.Common.Networking;
using ProtonVPN.Common.Threading;
using ProtonVPN.Common.Vpn;
using ProtonVPN.Service.Contract.Settings;
using ProtonVPN.Service.Contract.Vpn;
using ProtonVPN.Service.Settings;
using ProtonVPN.Service.Vpn;
using ProtonVPN.Vpn.Common;

namespace ProtonVPN.Service
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Single)]
    public class VpnConnectionHandler : IVpnConnectionContract, IServiceSettingsAware
    {
        private readonly object _callbackLock = new();
        private readonly List<IVpnEventsContract> _callbacks = new();

        private readonly KillSwitch.KillSwitch _killSwitch;
        private readonly IVpnConnection _vpnConnection;
        private readonly ILogger _logger;
        private readonly IServiceSettings _serviceSettings;
        private readonly ITaskQueue _taskQueue;
        private readonly NetworkSettings _networkSettings;

        private VpnState _state = new(VpnStatus.Disconnected, default);
        private VpnProtocol _vpnProtocol;

        public VpnConnectionHandler(
            KillSwitch.KillSwitch killSwitch,
            NetworkSettings networkSettings,
            IVpnConnection vpnConnection,
            ILogger logger,
            IServiceSettings serviceSettings,
            ITaskQueue taskQueue)
        {
            _killSwitch = killSwitch;
            _vpnConnection = vpnConnection;
            _logger = logger;
            _serviceSettings = serviceSettings;
            _taskQueue = taskQueue;
            _networkSettings = networkSettings;
            _vpnConnection.StateChanged += VpnConnection_StateChanged;
        }

        public Task Connect(VpnConnectionRequestContract connectionRequest)
        {
            Ensure.NotNull(connectionRequest, nameof(connectionRequest));

            _logger.Info("Connect requested");

            _serviceSettings.Apply(connectionRequest.Settings);

            IReadOnlyList<VpnHost> endpoints = Map(connectionRequest.Servers);
            VpnCredentials credentials = Map(connectionRequest.Credentials);
            VpnConfig config = Map(connectionRequest.VpnConfig);
            _vpnProtocol = config.VpnProtocol;

            if (config.VpnProtocol == VpnProtocol.WireGuard || _networkSettings.IsNetworkAdapterAvailable(config.VpnProtocol, config.OpenVpnAdapter))
            {
                _vpnConnection.Connect(endpoints, config, credentials);
            }
            else
            {
                HandleNoTapError();
            }

            return Task.CompletedTask;
        }

        public Task UpdateAuthCertificate(string certificate)
        {
            _vpnConnection.UpdateAuthCertificate(certificate);
            return Task.CompletedTask;
        }

        public Task Disconnect(SettingsContract settings, VpnErrorTypeContract vpnError)
        {
            _logger.Info("Disconnect requested");

            _serviceSettings.Apply(settings);

            _vpnConnection.Disconnect(Map(vpnError));

            return Task.CompletedTask;
        }

        public Task RepeatState()
        {
            _taskQueue.Enqueue(() =>
            {
                CallbackStateChanged(_state);
            });

            return Task.CompletedTask;
        }

        public Task<InOutBytesContract> Total()
        {
            return Map(_vpnConnection.Total).AsTask();
        }

        public Task RegisterCallback()
        {
            lock (_callbackLock)
            {
                _callbacks.Add(OperationContext.Current.GetCallbackChannel<IVpnEventsContract>());
            }

            return Task.CompletedTask;
        }

        public Task UnRegisterCallback()
        {
            _logger.Info("Unregister callback requested");

            lock (_callbackLock)
            {
                _callbacks.Remove(OperationContext.Current.GetCallbackChannel<IVpnEventsContract>());
            }

            return Task.CompletedTask;
        }

        public void OnServiceSettingsChanged(SettingsContract settings)
        {
            if (_state.Status == VpnStatus.Disconnected)
            {
                _logger.Info($"Callbacking VPN service settings change. Current state: {_state.Status} (Error: {_state.Error})");
                Callback(callback => callback.OnServiceSettingsStateChanged(CreateServiceSettingsState()));
            }
            else if (_state.Status == VpnStatus.Connected)
            {
                _vpnConnection.SetFeatures(new VpnFeatures(settings.NetShieldMode, settings.SplitTcp));
            }
        }

        private ServiceSettingsStateContract CreateServiceSettingsState()
        {
            return new(Map(_state));
        }

        private void HandleNoTapError()
        {
            if (_state.Status == VpnStatus.Disconnected)
            {
                CallbackStateChanged(new VpnState(VpnStatus.Disconnected, VpnError.NoTapAdaptersError, _vpnProtocol));
            }
            else
            {
                _vpnConnection.Disconnect(VpnError.NoTapAdaptersError);
            }
        }

        private void VpnConnection_StateChanged(object sender, EventArgs<VpnState> e)
        {
            _state = e.Data;
            CallbackStateChanged(_state);
        }

        private void CallbackStateChanged(VpnState state)
        {
            _logger.Info($"Callbacking VPN state {state.Status} (Error: {state.Error})");
            Callback(callback => callback.OnStateChanged(Map(state)));
        }

        private void Callback(Action<IVpnEventsContract> action)
        {
            lock (_callbackLock)
            {
                foreach (IVpnEventsContract callback in _callbacks.ToList())
                {
                    try
                    {
                        action(callback);
                    }
                    catch (Exception ex) when (ex.IsServiceCommunicationException())
                    {
                        _logger.Warn($"Callback failed: {ex.Message}");
                        _callbacks.Remove(callback);
                    }
                    catch (TimeoutException)
                    {
                        _logger.Warn("Callback timed out");
                    }
                }
            }
        }

        private VpnStateContract Map(VpnState state)
        {
            bool killSwitchEnabled = _killSwitch.ExpectedLeakProtectionStatus(state);
            if (!killSwitchEnabled)
            {
                _state = new VpnState(state.Status, state.Error, state.VpnProtocol);
            }

            return new VpnStateContract(
                Map(state.Status),
                Map(state.Error),
                state.RemoteIp,
                killSwitchEnabled,
                state.OpenVpnAdapter,
                Map(state.VpnProtocol),
                state.Label);
        }

        private static VpnStatusContract Map(VpnStatus vpnStatus)
        {
            return (VpnStatusContract)vpnStatus;
        }

        private static VpnCredentials Map(VpnCredentialsContract credentials)
        {
            if (credentials.ClientCertPem.IsNullOrEmpty() || credentials.ClientKeyPair == null)
            {
                return new(credentials.Username, credentials.Password);
            }
            return new(credentials.Username, credentials.Password, credentials.ClientCertPem, credentials.ClientKeyPair.ConvertBack());
        }

        private static IReadOnlyList<VpnHost> Map(IEnumerable<VpnHostContract> servers)
        {
            return servers.Select(Map).ToList();
        }

        private static VpnHost Map(VpnHostContract server)
        {
            return new(server.Name, server.Ip, server.Label, server.X25519PublicKey?.ConvertBack());
        }

        private VpnConfig Map(VpnConfigContract config)
        {
            Dictionary<VpnProtocol, IReadOnlyCollection<int>> portConfig =
                config.Ports.ToDictionary(p => Map(p.Key), p => (IReadOnlyCollection<int>)p.Value.ToList());
            return new VpnConfig(
                new VpnConfigParameters
                {
                    Ports = portConfig,
                    CustomDns = config.CustomDns,
                    SplitTunnelMode = config.SplitTunnelMode,
                    SplitTunnelIPs = config.SplitTunnelIPs,
                    OpenVpnAdapter = _serviceSettings.OpenVpnAdapter,
                    VpnProtocol = Map(config.VpnProtocol),
                    NetShieldMode = config.NetShieldMode,
                    SplitTcp =  config.SplitTcp,
                });
        }

        private VpnProtocol Map(VpnProtocolContract contract)
        {
            return contract switch
            {
                VpnProtocolContract.OpenVpnTcp => VpnProtocol.OpenVpnTcp,
                VpnProtocolContract.OpenVpnUdp => VpnProtocol.OpenVpnUdp,
                VpnProtocolContract.WireGuard => VpnProtocol.WireGuard,
                _ => VpnProtocol.Smart,
            };
        }

        private VpnProtocolContract Map(VpnProtocol protocol)
        {
            return protocol switch
            {
                VpnProtocol.OpenVpnTcp => VpnProtocolContract.OpenVpnTcp,
                VpnProtocol.OpenVpnUdp => VpnProtocolContract.OpenVpnUdp,
                VpnProtocol.WireGuard => VpnProtocolContract.WireGuard,
                VpnProtocol.Smart => VpnProtocolContract.Smart,
            };
        }

        private static InOutBytesContract Map(InOutBytes bytes)
        {
            return new(bytes.BytesIn, bytes.BytesOut);
        }

        private static VpnError Map(VpnErrorTypeContract errorType)
        {
            return (VpnError)errorType;
        }

        private static VpnErrorTypeContract Map(VpnError errorType)
        {
            return (VpnErrorTypeContract)errorType;
        }
    }
}