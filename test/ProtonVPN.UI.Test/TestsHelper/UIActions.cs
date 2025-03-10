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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace ProtonVPN.UI.Test.TestsHelper
{
    public class UIActions : UITestSession
    {
        public static void InsertTextIntoFieldWithId(string objectId, string text)
        {
            WindowsElement field = Session.FindElementByAccessibilityId(objectId);
            field.SendKeys(text);
        }

        public static void ClearInput(string id)
        {
            Session.FindElementByAccessibilityId(id).Clear();
        }

        public static void MoveToElement(IWebElement element)
        {
            Actions actions = new Actions(Session);
            actions.MoveToElement(element).Build().Perform();
        }

        public static void ClickOnObjectWithId(string objectId)
        {
            WindowsElement button = Session.FindElementByAccessibilityId(objectId);
            button.Click();
        }

        public static void ClickOnObjectWithName(string objectName)
        {
            WindowsElement button = Session.FindElementByName(objectName);
            button.Click();
        }

        public static void ClickOnObjectWithXPath(string objectPath)
        {
            ReadOnlyCollection<WindowsElement> button = Session.FindElementsByXPath(objectPath);
            button[0].Click();
        }

        public static void ClickOnObjectWithClassName(string className)
        {
            WindowsElement button = Session.FindElementByClassName(className);
            button.Click();
        }

        public static void CheckIfObjectWithIdIsDisplayed(string objectId, string errorMessage)
        {
            bool isDisplayed = Session.FindElementByAccessibilityId(objectId).Displayed;
            Assert.IsTrue(isDisplayed, errorMessage);
        }

        public static void CheckIfObjectIsNotDisplayed(string objectId, string errorMessage)
        {
            bool isDisplayed = Session.FindElementByAccessibilityId(objectId).Displayed;
            Assert.IsFalse(isDisplayed, errorMessage);
        }

        public static void CheckIfObjectIsNotDisplayedByName(string objectName, string errorMessage)
        {
            bool isDisplayed = Session.FindElementByName(objectName).Displayed;
            Assert.IsFalse(isDisplayed, errorMessage);
        }

        public static bool CheckIfElementExistsByName(string elementName)
        {
            List<IWebElement> elementList = new List<IWebElement>();
            elementList.AddRange(Session.FindElementsByName(elementName));
            return CheckIfExists(elementList);
        }

        public static void WaitUntilElementIsNotVisible(By locator, int timeInSeconds)
        {
            WebDriverWait wait = new WebDriverWait(Session, TimeSpan.FromSeconds(timeInSeconds));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(locator));
        }

        public static void CheckIfObjectWithAutomationIdDoesNotExist(string id, string errorMessage)
        {
            bool exists = false;
            ExecuteWithTempWait(() =>
            {
                Session.FindElementByAccessibilityId(id);
                exists = true;
            }, 1);

            Assert.IsFalse(exists, errorMessage);
        }

        public static void CheckIfObjectWithNameIsDisplayed(string objectName, string errorMessage)
        {
            bool content = Session.FindElementByName(objectName).Displayed;
            Assert.IsTrue(content, errorMessage);
        }

        public static void WaitUntilTextMatches(IWebElement element, string text, int seconds)
        {
            WebDriverWait wait = new WebDriverWait(Session, TimeSpan.FromSeconds(seconds));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.TextToBePresentInElement(element, text));
        }

        public static void WaitUntilDisplayed(By selector, int seconds)
        {
            WebDriverWait wait = new WebDriverWait(Session, TimeSpan.FromSeconds(seconds));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(selector));
        }

        public static void CheckIfObjectWithClassNameIsDisplayed(string className, string errorMessage)
        {
            bool content = Session.FindElementByClassName(className).Displayed;
            Assert.IsTrue(content, errorMessage);
        }

        public static void CheckIfElementWithAutomationIdTextMatches(string automationId, string valueToMatch, string errorMessage)
        {
            WindowsElement element = Session.FindElementByAccessibilityId(automationId);
            Assert.IsTrue(element.Text.Equals(valueToMatch), errorMessage);
        }

        public static void WaitUntilElementExistsByAutomationId(string automationId,int timeoutInSeconds)
        {
            DefaultWait<WindowsDriver<WindowsElement>> wait = new DefaultWait<WindowsDriver<WindowsElement>>(Session)
            {
                Timeout = TimeSpan.FromSeconds(timeoutInSeconds),
                PollingInterval = TimeSpan.FromMilliseconds(100)
            };

            WindowsElement mainWindow = null;
            wait.IgnoreExceptionTypes(typeof(WebDriverException));
            wait.Until(driver =>
            {
                RefreshSession();
                mainWindow = Session.FindElementByAccessibilityId(automationId);
                return mainWindow != null;
            });
        }

        private static void ExecuteWithTempWait(Action action, double timeInSeconds)
        {
            try
            {
                SetImplicitWait(timeInSeconds);
                action();
                SetImplicitWait(ImplicitWaitTimeInSeconds);
            }
            catch (Exception)
            {
                SetImplicitWait(ImplicitWaitTimeInSeconds);
            }
        }

        private static bool CheckIfExists(List<IWebElement> elementList)
        {
            bool isExists = false;
            if (elementList.Count > 0)
            {
                isExists = true;
            }
            return isExists;
        }
    }
}
