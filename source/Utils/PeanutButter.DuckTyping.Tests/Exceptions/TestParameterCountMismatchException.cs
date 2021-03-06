﻿using System;
using NUnit.Framework;
using PeanutButter.DuckTyping.Exceptions;
using PeanutButter.RandomGenerators;
using PeanutButter.TestUtils.Generic;

namespace PeanutButter.DuckTyping.Tests.Exceptions
{
    [TestFixture]
    public class TestParameterCountMismatchException: AssertionHelper
    {
        [Test]
        public void Type_ShouldInheritFrom_ArgumentException()
        {
            //--------------- Arrange -------------------
            var sut = typeof(ParameterCountMismatchException);

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            sut.ShouldInheritFrom<ArgumentException>();

            //--------------- Assert -----------------------
        }

        public void SomeMethod(int i1, string s1)
        {
            /* intentionally left blank */
        }

        [Test]
        public void Construct_ShouldSetMessage()
        {
            //--------------- Arrange -------------------
            var methodInfo = GetType().GetMethod("SomeMethod");
            var count = RandomValueGen.GetRandomInt();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = new ParameterCountMismatchException(
                count, methodInfo
            );

            //--------------- Assert -----------------------
            Expect(result.Message,
                Does.Contain($"{count} parameters were provided for method {methodInfo.DeclaringType.Name}.{methodInfo.Name}"));
            Expect(result.Message,
                Does.Contain($"but it requires 2 parameters"));
        }
    }
}