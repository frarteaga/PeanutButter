﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PeanutButter.DuckTyping.Extensions;
using static PeanutButter.RandomGenerators.RandomValueGen;
// ReSharper disable PossibleNullReferenceException

namespace PeanutButter.DuckTyping.Tests.Extensions
{
    [TestFixture]
    public class TestDuckTypingHelperExtensions : AssertionHelper
    {
        [Test]
        public void IsCaseSensitive_OperatingOnCaseSensitiveDictionary_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var input = new Dictionary<string, object>();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.IsCaseSensitive();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        private static IEqualityComparer<string>[] _caseInsensitiveComparers = new[]
        {
            StringComparer.OrdinalIgnoreCase,
            StringComparer.CurrentCultureIgnoreCase,
            StringComparer.InvariantCultureIgnoreCase
        };

        [TestCaseSource(nameof(_caseInsensitiveComparers))]
        public void IsCaseSensitive_OperatingOnCaseInSensitiveDictionary_ShouldReturnFalse(
            IEqualityComparer<string> comparer
        )
        {
            //--------------- Arrange -------------------
            var input = new Dictionary<string, object>(comparer);

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.IsCaseSensitive();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }

        [Test]
        public void IsCaseSensitive_OperatingOnSomethingWithoutComparerProperty_ShouldResortToBruteForce_EmptyIsNotSensitive()
        {
            //--------------- Arrange -------------------
            var dict = new MyDictionary(true);

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = dict.IsCaseSensitive();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }

        [Test]
        public void IsCaseSensitive_OperatingOnSomethingWithoutComparerProperty_ShouldResortToBruteForce_CaseSensitive()
        {
            //--------------- Arrange -------------------
            var dict = new MyDictionary(true);
            dict.Add(new KeyValuePair<string, object>("Foo", "bar"));

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = dict.IsCaseSensitive();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        [Test]
        public void ToCaseInsensitiveDictionary_OperatingOnCaseInsensitiveDictionary_ShouldReturnOriginal()
        {
            //--------------- Arrange -------------------
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", 1 }
            };

            //--------------- Assume ----------------
            Expect(dict.IsCaseSensitive(), Is.False);

            //--------------- Act ----------------------
            var result = dict.ToCaseInsensitiveDictionary();

            //--------------- Assert -----------------------
            Expect(result, Is.EqualTo(dict));
        }

        [Test]
        public void ToCaseInsensitiveDictionary_OperatingOnCaseSensitiveFlatDictionary_ShouldReturnNewCaseInsensitiveVariant()
        {
            //--------------- Arrange -------------------
            var expected = GetRandomInt();
            var dict = new Dictionary<string, object>()
            {
                { "Id", expected }
            };

            //--------------- Assume ----------------
            Expect(dict.IsCaseSensitive(), Is.True);

            //--------------- Act ----------------------
            var result = dict.ToCaseInsensitiveDictionary();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result.IsCaseSensitive(), Is.False);
            Expect(() => result["id"], Throws.Nothing);
        }

        [Test]
        public void ToCaseInsensitiveDictionary_ShouldGoCaseInsensitiveAllTheWayDown()
        {
            //--------------- Arrange -------------------
            var expected = GetRandomInt();
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Inner", new Dictionary<string, object>() { {  "Id", expected } } }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = dict.ToCaseInsensitiveDictionary();

            //--------------- Assert -----------------------
            Expect((result["Inner"] as Dictionary<string, object>)["id"], Is.EqualTo(expected));
        }

        [Test]
        public void ToCaseInsensitiveDictionary_ShouldNotBeConfusedBySelfReferencingDictionaries()
        {
            //--------------- Arrange -------------------
            var outer = new Dictionary<string, object>();
            var inner = new Dictionary<string, object>();
            outer["Inner"] = inner;
            inner["Outer"] = outer;

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            Expect(() =>
                outer.ToCaseInsensitiveDictionary(),
                Throws.Nothing);

            //--------------- Assert -----------------------
        }


        [Test]
        public void FailingWildDuck1()
        {
            //--------------- Arrange -------------------
            var json = "{\"flowId\":\"Travel Request\",\"activityId\":\"Capture Travel Request Details\",\"payload\":{\"taskId\":\"4e53c85b-ca72-4c12-b185-50342ed0fc30\",\"payload\":{\"Initiated\":\"\",\"DepartingFrom\":\"123\",\"TravellingTo\":\"123\",\"Departing\":\"\",\"PreferredDepartureTime\":\"\",\"Returning\":\"\",\"PreferredReturnTime\":\"\",\"ReasonForTravel\":\"123\",\"CarRequired\":\"\",\"AccomodationRequired\":\"\",\"AccommodationNotes\":\"213\"}}}";
            var jobject = JsonConvert.DeserializeObject<JObject>(json);
            var dict = jobject.ToDictionary();
            (dict["payload"] as Dictionary<string, object>)["actorId"] = Guid.Empty.ToString();
            var payload = dict["payload"];

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = payload.FuzzyDuckAs<ITravelRequestCaptureDetailsActivityParameters>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
        }

        [Test]
        public void ForceFuzzyDuckAs_GivenEmptyDictionaryAndInterfaceToMimick_ShouldHandleIt()
        {
            //--------------- Arrange -------------------
            var dict = new Dictionary<string, object>();
            var expected = new TravelRequestDetails() {
                Initiated = GetRandomDate(),
                DepartingFrom = GetRandomString(),
                TravellingTo = GetRandomString(),
                Departing = GetRandomDate(),
                PreferredDepartureTime = GetRandomString(),
                Returning = GetRandomDate(),
                PreferredReturnTime = GetRandomString(),
                ReasonForTravel = GetRandomString(),
                CarRequired = GetRandomBoolean(),
                AccomodationRequired = GetRandomBoolean(),
                AccommodationNotes = GetRandomString()
            };
            var expectedDuck = expected.DuckAs<ITravelRequestDetails>();

            //--------------- Assume ----------------
            Expect(expectedDuck, Is.Not.Null);

            //--------------- Act ----------------------
            var result = dict.ForceFuzzyDuckAs<ITravelRequestDetails>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result, Is.InstanceOf<ITravelRequestDetails>());
            Expect(() =>
            {
                result.Initiated = expectedDuck.Initiated;
                result.DepartingFrom = expectedDuck.DepartingFrom;
                result.TravellingTo = expectedDuck.TravellingTo;
                result.Departing = expectedDuck.Departing;
                result.PreferredDepartureTime = expectedDuck.PreferredDepartureTime;
                result.Returning = expectedDuck.Returning;
                result.PreferredReturnTime = expectedDuck.PreferredReturnTime;
                result.ReasonForTravel = expectedDuck.ReasonForTravel;
                result.CarRequired = expectedDuck.CarRequired;
                result.AccomodationRequired = expectedDuck.AccomodationRequired;
                result.AccommodationNotes = expectedDuck.AccommodationNotes;
            }, Throws.Nothing);

            foreach (var prop in result.GetType().GetProperties())
            {
                Expect(dict[prop.Name], Is.EqualTo(prop.GetValue(result)));
            }
        }

        [Test]
        public void ForceDuckAs_GivenEmptyDictionaryAndInterfaceToMimick_ShouldHandleIt()
        {
            //--------------- Arrange -------------------
            var dict = new Dictionary<string, object>();
            var expected = new TravelRequestDetails() {
                Initiated = GetRandomDate(),
                DepartingFrom = GetRandomString(),
                TravellingTo = GetRandomString(),
                Departing = GetRandomDate(),
                PreferredDepartureTime = GetRandomString(),
                Returning = GetRandomDate(),
                PreferredReturnTime = GetRandomString(),
                ReasonForTravel = GetRandomString(),
                CarRequired = GetRandomBoolean(),
                AccomodationRequired = GetRandomBoolean(),
                AccommodationNotes = GetRandomString()
            };
            var expectedDuck = expected.DuckAs<ITravelRequestDetails>();

            //--------------- Assume ----------------
            Expect(expectedDuck, Is.Not.Null);

            //--------------- Act ----------------------
            var result = dict.ForceDuckAs<ITravelRequestDetails>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result, Is.InstanceOf<ITravelRequestDetails>());
            Expect(() =>
            {
                result.Initiated = expectedDuck.Initiated;
                result.DepartingFrom = expectedDuck.DepartingFrom;
                result.TravellingTo = expectedDuck.TravellingTo;
                result.Departing = expectedDuck.Departing;
                result.PreferredDepartureTime = expectedDuck.PreferredDepartureTime;
                result.Returning = expectedDuck.Returning;
                result.PreferredReturnTime = expectedDuck.PreferredReturnTime;
                result.ReasonForTravel = expectedDuck.ReasonForTravel;
                result.CarRequired = expectedDuck.CarRequired;
                result.AccomodationRequired = expectedDuck.AccomodationRequired;
                result.AccommodationNotes = expectedDuck.AccommodationNotes;
            }, Throws.Nothing);

            foreach (var prop in result.GetType().GetProperties())
            {
                Expect(dict[prop.Name], Is.EqualTo(prop.GetValue(result)));
            }
        }

        public class TravelRequestDetails: ITravelRequestDetails
        {
            public DateTime Initiated { get; set; }
            public string DepartingFrom { get; set; }
            public string TravellingTo { get; set; }
            public DateTime Departing { get; set; }
            public string PreferredDepartureTime { get; set; }
            public DateTime Returning { get; set; }
            public string PreferredReturnTime { get; set; }
            public string ReasonForTravel { get; set; }
            public bool CarRequired { get; set; }
            public bool AccomodationRequired { get; set; }
            public string AccommodationNotes { get; set; }
        }
        public interface ITravelRequestDetails
        {
            DateTime Initiated { get; set; }
            string DepartingFrom { get; set; }
            string TravellingTo { get; set; }
            DateTime Departing { get; set; }
            string PreferredDepartureTime { get; set; }
            DateTime Returning { get; set; }
            string PreferredReturnTime { get; set; }
            string ReasonForTravel { get; set; }
            bool CarRequired { get; set; }
            bool AccomodationRequired { get; set; }
            string AccommodationNotes { get; set; }
        }

        public interface ITravelRequestCaptureDetailsActivityParameters :
            IActivityParameters<ITravelRequestDetails>
        {
        }

        public interface IHasAnActorId
        {
            Guid ActorId { get; }
        }
        public interface IActivityParameters : IHasAnActorId
        {
            Guid TaskId { get; }
        }
        public interface IActivityParameters<T> : IActivityParameters
        {
            T Payload { get; }
        }


        public class MyDictionary : IDictionary<string, object>
        {
            public MyDictionary(bool isCaseSensitive)
            {
                _isCaseSensitive = isCaseSensitive;
            }
            private List<KeyValuePair<string, object>> _data = new List<KeyValuePair<string, object>>();
            private bool _isCaseSensitive;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(KeyValuePair<string, object> item)
            {
                _data.Add(item);
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public int Count { get; }
            public bool IsReadOnly { get; }
            public bool ContainsKey(string key)
            {
                return _data.Any(kvp => KeysMatch(kvp.Key, key));
            }

            private bool KeysMatch(string k1, string k2)
            {
                return _isCaseSensitive
                        ? k1 == k2
                        : k1?.ToLower() == k2?.ToLower();
            }


            public void Add(string key, object value)
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out object value)
            {
                if (!Keys.Select(k => _isCaseSensitive ? k : k.ToLower()).Contains(_isCaseSensitive ? key : key.ToLower()))
                {
                    value = null;
                    return false;
                }
                value = _data.First(kvp => kvp.Key == key).Value;
                return true;
            }

            public object this[string key]
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public ICollection<string> Keys => _data.Select(kvp => kvp.Key).ToList();
            public ICollection<object> Values => _data.Select(kvp => kvp.Value).ToList();
        }


    }
    public static class JObjectExtensions
    {
        // just because JsonCovert doesn't believe in using your provided type all the way down >_<
        public static Dictionary<string, object> ToDictionary(this JObject src)
        {
            var result = new Dictionary<string, object>();
            if (src == null)
                return result;
            foreach (var prop in src.Properties())
            {
                result[prop.Name] = _resolvers[prop.Type](prop.Value);
            }
            return result;
        }

        private static Dictionary<JTokenType, Func<JToken, object>> _resolvers = new Dictionary<JTokenType, Func<JToken, object>>()
        {
            { JTokenType.None, o => null },
            { JTokenType.Array, ConvertJTokenArray },
            { JTokenType.Property, ConvertJTokenProperty },
            { JTokenType.Integer, o => o.Value<int>() },
            { JTokenType.String, o => o.Value<string>() },
            { JTokenType.Boolean, o => o.Value<bool>() },
            { JTokenType.Null, o => null },
            { JTokenType.Undefined, o => null },
            { JTokenType.Date, o => o.Value<DateTime>() },
            { JTokenType.Bytes, o => o.Value<byte[]>() },
            { JTokenType.Guid, o => o.Value<Guid>() },
            { JTokenType.Uri, o => o.Value<Uri>() },
            { JTokenType.TimeSpan, o => o.Value<TimeSpan>() },
            { JTokenType.Object, TryConvertObject }

        };

        private static object TryConvertObject(JToken arg)
        {
            var asJObject = arg as JObject;
            if (asJObject != null)
                return asJObject.ToDictionary();
            return PassThrough(arg);
        }

        private static object PassThrough(JToken arg)
        {
            return arg;
        }

        private static object ConvertJTokenProperty(JToken arg)
        {
            Func<JToken, object> resolver;
            if (_resolvers.TryGetValue(arg.Type, out resolver))
                return resolver(arg);
            throw new InvalidOperationException($"Unable to handle JToken of type: {arg.Type}");
        }

        private static object ConvertJTokenArray(JToken arg)
        {
            throw new NotImplementedException();
        }
    }
}