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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ProtonVPN.Common.Logging;
using ProtonVPN.Common.OS.Net.NetworkInterface;

namespace ProtonVPN.Common.Test.OS.Net.NetworkInterface
{
    [TestClass]
    [SuppressMessage("ReSharper", "AssignmentIsFullyDiscarded")]
    public class SafeSystemNetworkInterfacesTest
    {
        private ILogger _logger;
        private INetworkInterfaces _origin;

        [TestInitialize]
        public void TestInitialize()
        {
            _logger = Substitute.For<ILogger>();
            _origin = Substitute.For<INetworkInterfaces>();
        }

        [TestMethod]
        public void NetworkAddressChanged_ShouldRaise_WhenOrigin_NetworkAddressChanged_IsRaised()
        {
            // Arrange
            var wasRaised = false;
            var subject = new SafeSystemNetworkInterfaces(_logger, _origin);
            subject.NetworkAddressChanged += (s, e) => wasRaised = true;
            // Act
            _origin.NetworkAddressChanged += Raise.Event<EventHandler>();
            // Assert
            wasRaised.Should().BeTrue();
        }

        [TestMethod]
        public void Interfaces_ShouldBe_Origin_Interfaces()
        {
            // Arrange
            var expected = new INetworkInterface[] {new TestNetworkInterface("t1"), new TestNetworkInterface("t2")};
            _origin.GetInterfaces().Returns(expected);
            var subject = new SafeSystemNetworkInterfaces(_logger, _origin);
            // Act
            var result = subject.GetInterfaces();
            // Assert
            _origin.Received().GetInterfaces();
            result.Should().HaveCount(2);
        }

        [DataTestMethod]
        [DataRow(typeof(NetworkInformationException))]
        public void Interfaces_ShouldSuppress_ExpectedException(Type exceptionType)
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType);
            _origin.GetInterfaces().Throws(exception);
            var subject = new SafeSystemNetworkInterfaces(_logger, _origin);
            // Act
            var result = subject.GetInterfaces();
            // Assert
            result.Should().BeEmpty();
        }

        [DataTestMethod]
        public void Interfaces_ShouldPass_NotExpectedException()
        {
            // Arrange
            var exception = new Exception();
            _origin.GetInterfaces().Throws(exception);
            var subject = new SafeSystemNetworkInterfaces(_logger, _origin);
            // Act
            Action action = () => subject.GetInterfaces();
            // Assert
            action.Should().Throw<Exception>();
        }

        [DataTestMethod]
        public void Interfaces_ShouldLog_ExpectedException()
        {
            // Arrange
            var exception = new NetworkInformationException();
            _origin.GetInterfaces().Throws(exception);
            var subject = new SafeSystemNetworkInterfaces(_logger, _origin);
            // Act
            _ = subject.GetInterfaces();
            // Assert
            _logger.ReceivedWithAnyArgs().Error("");
        }

        #region Helpers

        private class TestNetworkInterface : INetworkInterface
        {
            public TestNetworkInterface(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public string Name => string.Empty;

            public string Description => string.Empty;

            public bool IsLoopback => false;

            public bool IsActive => false;

            public IPAddress DefaultGateway => IPAddress.None;

            public uint Index => 0;
        }

        #endregion
    }
}