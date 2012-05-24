﻿using System;
using GalaSoft.MvvmLight.Test.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GalaSoft.MvvmLight.Test
{
    [TestClass]
    public class ObservableObjectPropertyChangedTest
    {
        [TestMethod]
        public void TestPropertyChangedNoBroadcast()
        {
            var receivedDateTimeLocal = DateTime.MinValue;

            var vm = new TestClassWithObservableObject();
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == TestClassWithObservableObject.LastChangedPropertyName)
                {
                    receivedDateTimeLocal = vm.LastChanged;
                }
            };

            var now = DateTime.Now;
            vm.LastChanged = now;
            Assert.AreEqual(now, vm.LastChanged);
            Assert.AreEqual(now, receivedDateTimeLocal);
        }

        [TestMethod]
        public void TestPropertyChangedNoMagicString()
        {
            var receivedDateTimeLocal = DateTime.MinValue;

            var vm = new TestClassWithObservableObject();
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "LastChangedNoMagicString")
                {
                    receivedDateTimeLocal = vm.LastChangedNoMagicString;
                }
            };

            var now = DateTime.Now;
            vm.LastChangedNoMagicString = now;
            Assert.AreEqual(now, vm.LastChangedNoMagicString);
            Assert.AreEqual(now, receivedDateTimeLocal);
        }

        [TestMethod]
#if !NET40
#if DEBUG
        [ExpectedException(typeof(ArgumentException))]
#endif
#else
        // For some reason the VS test adapter throws exception even in Release mode
        [ExpectedException(typeof(ArgumentException))]
#endif
        public void TestRaisePropertyChangedValidInvalidPropertyName()
        {
            var vm = new TestClassWithObservableObject();

            var receivedPropertyChanged = false;
            var invalidPropertyNameReceived = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == TestClassWithObservableObject.LastChangedPropertyName)
                {
                    receivedPropertyChanged = true;
                }
                else
                {
                    invalidPropertyNameReceived = true;
                }
            };

            vm.RaisePropertyChangedPublic(TestClassWithObservableObject.LastChangedPropertyName);

            Assert.IsTrue(receivedPropertyChanged);
            Assert.IsFalse(invalidPropertyNameReceived);

            vm.RaisePropertyChangedPublic(TestClassWithObservableObject.LastChangedPropertyName + "1");

            Assert.IsTrue(invalidPropertyNameReceived);
        }

        [TestMethod]
        public void TestSet()
        {
            var vm = new TestClassWithObservableObject();
            const int expectedValue = 1234;
            var receivedValue = 0;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == TestClassWithObservableObject.PropertyWithSetPropertyName)
                {
                    receivedValue = expectedValue;
                }
            };

            vm.PropertyWithSet = expectedValue;
            Assert.AreEqual(expectedValue, receivedValue);
        }

        [TestMethod]
        public void TestSetWithString()
        {
            var vm = new TestClassWithObservableObject();
            const int expectedValue = 1234;
            var receivedValue = 0;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == TestClassWithObservableObject.PropertyWithStringSetPropertyName)
                {
                    receivedValue = expectedValue;
                }
            };

            vm.PropertyWithStringSet = expectedValue;
            Assert.AreEqual(expectedValue, receivedValue);
        }

        [TestMethod]
        public void TestReturnValueWithSet()
        {
            var vm = new TestClassWithObservableObject();
            const int firstValue = 1234;
            var receivedValue = 0;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == TestClassWithObservableObject.PropertyWithSetPropertyName)
                {
                    receivedValue = vm.PropertyWithSet;
                }
            };

            vm.PropertyWithSet = firstValue;
            Assert.AreEqual(firstValue, receivedValue);
            Assert.IsTrue(vm.SetRaisedPropertyChangedEvent);

            vm.PropertyWithSet = firstValue;
            Assert.AreEqual(firstValue, receivedValue);
            Assert.IsFalse(vm.SetRaisedPropertyChangedEvent);

            vm.PropertyWithSet = firstValue + 1;
            Assert.AreEqual(firstValue + 1, receivedValue);
            Assert.IsTrue(vm.SetRaisedPropertyChangedEvent);
        }

        [TestMethod]
        public void TestReturnValueWithSetWithString()
        {
            var vm = new TestClassWithObservableObject();
            const int firstValue = 1234;
            var receivedValue = 0;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == TestClassWithObservableObject.PropertyWithStringSetPropertyName)
                {
                    receivedValue = vm.PropertyWithStringSet;
                }
            };

            vm.PropertyWithStringSet = firstValue;
            Assert.AreEqual(firstValue, receivedValue);
            Assert.IsTrue(vm.SetRaisedPropertyChangedEvent);

            vm.PropertyWithStringSet = firstValue;
            Assert.AreEqual(firstValue, receivedValue);
            Assert.IsFalse(vm.SetRaisedPropertyChangedEvent);

            vm.PropertyWithStringSet = firstValue + 1;
            Assert.AreEqual(firstValue + 1, receivedValue);
            Assert.IsTrue(vm.SetRaisedPropertyChangedEvent);
        }
    }
}
