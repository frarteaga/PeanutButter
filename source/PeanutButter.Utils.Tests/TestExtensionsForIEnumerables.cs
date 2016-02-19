﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PeanutButter.RandomGenerators;

namespace PeanutButter.Utils.Tests
{
    [TestFixture]
    public class TestExtensionsForIEnumerables
    {
        [Test]
        public void ForEach_OperatingOnNullCollection_ShouldThrowArgumentNullException()
        {
            // This may seem pointless, but bear with me:
            //  I actually wanted ForEach to just do nothing when given a null collection,
            //  however System.Collections.Generic.List<T> has its own ForEach signature
            //  which doesn't even throw a valid ArgumentNullException -- it just barfs
            //  with a NullReferenceException. So if I made mine different, it would be
            //  inconsistent and surprising. And extension methods can't override actual
            //  methods. So, poo. This test then serves as documentation that I thought
            //  about it and chose consistency over function. You can, however, use
            //  the helper EmptyIfNull() to get the nicer behaviour:
            //  int[] foo = null;
            //  foo.EmptyIfNull().ForEach(i => {});
            //---------------Set up test pack-------------------
            int[] src = null;

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            Assert.Throws<NullReferenceException>(() => src.ForEach(i => { }));

            //---------------Test Result -----------------------
        }

        [Test]
        public void ForEach_OperatingOnCollection_ShouldRunActionOnEachElement()
        {
            //---------------Set up test pack-------------------
            var result = new List<int>();
            var src = new[]
            {
                RandomValueGen.GetRandomInt(),
                RandomValueGen.GetRandomInt(),
                RandomValueGen.GetRandomInt()
            };

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            src.ForEach(item => result.Add(item));

            //---------------Test Result -----------------------
            CollectionAssert.AreEqual(src, result);
        }

        [Test]
        public void IsSameAs_OperatingOnCollection_WhenBothAreNull_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            List<int> first = null;
            List<int> second = null;

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = first.IsSameAs(second);

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsSameAs_OperatingOnCollection_WhenOneIsNull_ShouldReturnFalse()
        {
            //---------------Set up test pack-------------------
            List<int> first = null;
            var second = new List<int>();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = first.IsSameAs(second);

            //---------------Test Result -----------------------
            Assert.IsFalse(result);
        }

        [Test]
        public void IsSameAs_OperatingOnCollection_WhenBothAreEmpty_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var first = new int[] {};
            var second = new List<int>();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = first.IsSameAs(second);

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsSameAs_OperatingOnCollection_WhenBothContainSameElements_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var first = new int[] { 1 };
            var second = new List<int>(new[] { 1 });

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = first.IsSameAs(second);

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsSameAs_OperatingOnCollection_WhenBothContainSameElementsInDifferentOrder_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var first = new int[] { 1, 2 };
            var second = new List<int>(new[] { 2, 1 });

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = first.IsSameAs(second);

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsSameAs_OperatingOnCollection_WhenBothContainDifferentElements_ShouldReturnFalse()
        {
            //---------------Set up test pack-------------------
            var first = new int[] { 1 };
            var second = new List<int>(new[] { 2, 1 });

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = first.IsSameAs(second);

            //---------------Test Result -----------------------
            Assert.IsFalse(result);
        }

        [Test]
        public void JoinWith_OperatingOnCollection_WhenCollectionIsEmpty_ShouldReturnEmptyString()
        {
            //---------------Set up test pack-------------------
            var src = new List<string>();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.JoinWith(",");

            //---------------Test Result -----------------------
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void JoinWith_OperatingOnCollection_WhenCollectionIsNotEmpty_ShouldReturnCollectionJoinedWithGivenDelimiter()
        {
            //---------------Set up test pack-------------------
            var src = new int[] {1, 2, 3};
            var delimiter = RandomValueGen.GetRandomString(2, 3);
            var expected = "1" + delimiter + "2" + delimiter + "3";

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.JoinWith(delimiter);

            //---------------Test Result -----------------------
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void IsEmpty_WhenCollectionIsEmpty_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var src = new Stack<int>();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.IsEmpty();

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsEmpty_WhenCollectionIsNotEmpty_ShouldReturnFalse()
        {
            //---------------Set up test pack-------------------
            var src = new[] {1};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.IsEmpty();

            //---------------Test Result -----------------------
            Assert.IsFalse(result);
        }

        [Test]
        public void EmptyIfNull()
        {
            //---------------Set up test pack-------------------
            List<int> src = null;
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.EmptyIfNull();

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<List<int>>(result);
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void And_OperatingOnArrayOfType_ShouldReturnNewArrayWithAddedItems()
        {
            //---------------Set up test pack-------------------
            var src = new[] {1, 2, 3};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.And(4, 5);

            //---------------Test Result -----------------------
            CollectionAssert.AreEqual(new[] {1, 2, 3, 4, 5}, result);
        }

        [Test]
        public void ButNot_OperatingOnArrayOfType_ShouldReturnNewArrayWithAddedItems()
        {
            //---------------Set up test pack-------------------
            var src = new[] {1, 2, 3};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = src.ButNot(2);

            //---------------Test Result -----------------------
            CollectionAssert.AreEqual(new[] {1, 3 }, result);
        }

        [Test]
        public void Flatten_GivenEmptyCollection_ShouldReturnEmptyCollection()
        {
            //---------------Set up test pack-------------------
            var input = new List<List<int>>();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = input.Flatten();

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void Flatten_GivenCollectionWithOneItemInSubCollection_ShouldReturnFlattened()
        {
            //---------------Set up test pack-------------------
            var input = new List<IEnumerable<int>>();
            var expected = RandomValueGen.GetRandomCollection<int>(1,1);
            input.Add(expected);

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = input.Flatten();

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            CollectionAssert.IsNotEmpty(result);
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Flatten_GivenCollectionWithMultipleItemsInMultipleSubCollections_ShouldReturnFlattened()
        {
            //---------------Set up test pack-------------------
            var input = new List<IEnumerable<int>>();
            var part1 = RandomValueGen.GetRandomCollection<int>();
            var part2 = RandomValueGen.GetRandomCollection<int>();
            var part3 = RandomValueGen.GetRandomCollection<int>();
            var expected = new List<int>();
            expected.AddRange(part1);
            expected.AddRange(part2);
            expected.AddRange(part3);
            input.AddRange(new[] { part1, part2, part3 }); 

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = input.Flatten();

            //---------------Test Result -----------------------
            CollectionAssert.AreEquivalent(expected, result);
        }

        private class ItemWithNullableId
        {
            public int? Id { get; set; }
            public static ItemWithNullableId For(int? value)
            {
                return new ItemWithNullableId() { Id = value };
            }
        }

        [Test]
        public void SelectNonNull_GivenCollectionOfObjectsWithNullableInts_ShouldReturnOnlyNonNullValues()
        {
            //---------------Set up test pack-------------------
            var id1 = RandomValueGen.GetRandomInt();
            var id2 = RandomValueGen.GetRandomInt();
            var expected = new[] { id1, id2 };
            var input = new[] 
            {
                ItemWithNullableId.For(id1),
                ItemWithNullableId.For(null),
                ItemWithNullableId.For(id2),
                ItemWithNullableId.For(null)
            };

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = input.SelectNonNull(o => o.Id);

            //---------------Test Result -----------------------
            CollectionAssert.AreEqual(expected, result);
        }


        public class Thing
        {
        }
        public class ItemWithNullableThing
        {
            public Thing Thing { get; set; }
            public static ItemWithNullableThing For(Thing thing)
            {
                return new ItemWithNullableThing() { Thing = thing };
            }
        }

        [Test]
        public void SelectNonNull_GivenCollectionOfObjectsWithNullableThings_ShouldReturnOnlyNonNullValues()
        {
            //---------------Set up test pack-------------------
            var id1 = RandomValueGen.GetRandomValue<Thing>();
            var id2 = RandomValueGen.GetRandomValue<Thing>();
            var expected = new[] { id1, id2 };
            var input = new[] 
            {
                ItemWithNullableThing.For(id1),
                ItemWithNullableThing.For(null),
                ItemWithNullableThing.For(id2),
                ItemWithNullableThing.For(null)
            };

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = input.SelectNonNull(o => o.Thing);

            //---------------Test Result -----------------------
            CollectionAssert.AreEqual(expected, result);
        }


    }
}