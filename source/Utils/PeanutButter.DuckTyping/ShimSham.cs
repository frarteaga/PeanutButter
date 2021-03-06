﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PeanutButter.DuckTyping.AutoConversion;
using PeanutButter.DuckTyping.Exceptions;
using PeanutButter.DuckTyping.Extensions;

namespace PeanutButter.DuckTyping
{
    /// <summary>
    /// Shim to wrap objects for ducking
    /// </summary>
    public class ShimSham : ShimShamBase, IShimSham
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once InconsistentNaming
        private bool _isFuzzy { get; }
        private readonly object _wrapped;
        private readonly IPropertyInfoFetcher _propertyInfoFetcher;
        private readonly Type _wrappedType;
        private readonly bool _wrappingADuck;

        private static readonly Dictionary<Type, PropertyInfoContainer> _propertyInfos = new Dictionary<Type, PropertyInfoContainer>();
        private static readonly Dictionary<Type, MethodInfoContainer> _methodInfos = new Dictionary<Type, MethodInfoContainer>();
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldInfos = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        private Dictionary<string, FieldInfo> _localFieldInfos;
        private Dictionary<string, MethodInfo> _localMethodInfos;
        private Dictionary<string, PropertyInfo> _localPropertyInfos;
        private readonly Dictionary<string, object> _shimmedProperties = new Dictionary<string, object>();
        private readonly HashSet<string> _unshimmableProperties = new HashSet<string>();
        private PropertyInfoContainer _mimickedPropInfos;
        private Dictionary<string, PropertyInfo> _localMimickPropertyInfos;

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Constructs a new instance of the ShimSham with a DefaultPropertyInfoFetcher
        /// </summary>
        /// <param name="toWrap">Object to wrap</param>
        /// <param name="interfaceToMimick">Interface type to mimick</param>
        /// <param name="isFuzzy">Flag allowing or preventing approximation</param>
        public ShimSham(object toWrap, Type interfaceToMimick, bool isFuzzy)
            : this(toWrap, interfaceToMimick, isFuzzy, new DefaultPropertyInfoFetcher())
        {
        }

        /// <summary>
        /// Constructs a new instance of the ShimSham with the provided property info fetcher
        /// </summary>
        /// <param name="toWrap">Object to wrap</param>
        /// <param name="interfaceToMimick">Interface type to mimick</param>
        /// <param name="isFuzzy">Flag allowing or preventing approximation</param>
        /// <param name="propertyInfoFetcher">Utility to fetch property information from the provided object and interface type</param>
        /// <exception cref="ArgumentNullException">Thrown if the mimick interface or property info fetch are null</exception>
        // ReSharper disable once MemberCanBePrivate.Global
        public ShimSham(
            object toWrap,
            Type interfaceToMimick,
            bool isFuzzy,
            IPropertyInfoFetcher propertyInfoFetcher)
        {
            if (interfaceToMimick == null) throw new ArgumentNullException(nameof(interfaceToMimick));
            if (propertyInfoFetcher == null) throw new ArgumentNullException(nameof(propertyInfoFetcher));
            _isFuzzy = isFuzzy;
            _wrapped = toWrap;
            _propertyInfoFetcher = propertyInfoFetcher;
            _wrappedType = toWrap.GetType();
            _wrappingADuck = IsObjectADuck();
            StaticallyCachePropertyInfosFor(toWrap, _wrappingADuck);
            StaticallyCachePropertInfosFor(interfaceToMimick);
            StaticallyCacheMethodInfosFor(_wrappedType);
            LocallyCachePropertyInfos();
            LocallyCacheMethodInfos();
            LocallyCacheMimickedPropertyInfos();
        }

        private void StaticallyCachePropertInfosFor(Type interfaceToMimick)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public;
            _mimickedPropInfos = new PropertyInfoContainer(
                interfaceToMimick.GetAllImplementedInterfaces()
                    .Select(i => _propertyInfoFetcher
                        .GetProperties(i, bindingFlags))
                    .SelectMany(c => c)
                    .ToArray()
            );
        }

        private void StaticallyCacheMethodInfosFor(Type wrappedType)
        {
            lock (_methodInfos)
            {
                if (_methodInfos.ContainsKey(wrappedType))
                    return;
                _methodInfos[wrappedType] = new MethodInfoContainer(
                    wrappedType
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        // TODO: handle method overloads, which this won't
                        .Distinct(new MethodInfoComparer())
                        .ToArray()
                );
            }
        }

        /// <inheritdoc />
        public object GetPropertyValue(string propertyName)
        {
            if (_wrappingADuck)
            {
                return FieldValueFor(propertyName);
            }
            object shimmed;
            if (_shimmedProperties.TryGetValue(propertyName, out shimmed))
                return shimmed;
            var propInfo = FindPropertyInfoFor(propertyName);
            if (!propInfo.PropertyInfo.CanRead || propInfo.Getter == null)
            {
                throw new WriteOnlyPropertyException(_wrappedType, propertyName);
            }
            return DuckIfRequired(propInfo, propertyName);
        }

        private object DuckIfRequired(PropertyInfoCacheItem propertyInfoCacheItem, string propertyName)
        {
            object existingShim;
            if (_shimmedProperties.TryGetValue(propertyName, out existingShim))
                return existingShim;
            var getter = propertyInfoCacheItem.Getter;
            var propValue = getter();

            var correctType = _localMimickPropertyInfos[propertyName].PropertyType;
            if (propValue == null)
            {
                return GetDefaultValueFor(correctType);
            }
            var propValueType = propertyInfoCacheItem.PropertyInfo.PropertyType;
            if (correctType.IsAssignableFrom(propValueType))
                return propValue;
            var converter = ConverterLocator.GetConverter(propValueType, correctType);
            if (converter != null)
                return ConvertWith(converter, propValue, correctType);
            if (correctType.ShouldTreatAsPrimitive())
                return GetDefaultValueFor(correctType);
            if (CannotShim(propertyName, propValueType, correctType))
                return null;
            var duckType = MakeTypeToImplement(correctType, _isFuzzy);
            var instance = Activator.CreateInstance(duckType, propValue);
            _shimmedProperties[propertyName] = instance;
            return instance;
        }

        private bool CannotShim(string propertyName, Type srcType, Type targetType)
        {
            if (_unshimmableProperties.Contains(propertyName))
                return true;
            var result = !srcType.InternalCanDuckAs(targetType, _isFuzzy, false);
            if (result)
                _unshimmableProperties.Add(propertyName);
            return result;
        }

        /// <inheritdoc />
        public void SetPropertyValue(string propertyName, object newValue)
        {
            if (_wrappingADuck)
            {
                SetFieldValue(propertyName, newValue);
                return;
            }

            var propInfo = FindPropertyInfoFor(propertyName);
            if (!propInfo.PropertyInfo.CanWrite || propInfo.Setter == null)
            {
                throw new ReadOnlyPropertyException(_wrappedType, propertyName);
            }
            var mimickedPropInfo = _localMimickPropertyInfos[propertyName];
            var newValueType = newValue?.GetType();
            var mimickedType = mimickedPropInfo.PropertyType;
            if (newValueType == null)
            {
                var defaultValue = GetDefaultValueFor(mimickedType);
                TrySetValue(defaultValue, mimickedType, propInfo);
                return;
            }
            if (mimickedType.IsAssignableFrom(newValueType))
            {
                TrySetValue(newValue, newValueType, propInfo);
                return;
            }
            var duckType = MakeTypeToImplement(mimickedType, _isFuzzy);
            var instance = Activator.CreateInstance(duckType, newValue);
            _shimmedProperties[propertyName] = instance;
        }



        /// <inheritdoc />
        public void CallThroughVoid(string methodName, params object[] parameters)
        {
            CallThrough(methodName, parameters);
        }

        /// <inheritdoc />
        public object CallThrough(string methodName, object[] parameters)
        {
            if (_wrappingADuck)
            {
                throw new NotImplementedException("Cannot call-through when there is no wrapped object");
            }
            MethodInfo methodInfo;
            if (!_localMethodInfos.TryGetValue(methodName, out methodInfo))
                throw new MethodNotFoundException(_wrappedType, methodName);
            if (_isFuzzy)
                parameters = AttemptToOrderCorrectly(parameters, methodInfo);
            var result = methodInfo.Invoke(_wrapped, parameters);
            return result;
        }

        private object[] AttemptToOrderCorrectly(
            object[] parameters,
            MethodInfo methodInfo)
        {
            var methodParameters = methodInfo.GetParameters();
            if (parameters.Length != methodParameters.Length)
                throw new ParameterCountMismatchException(parameters.Length, methodInfo);
            var srcTypes = parameters.Select(o => o?.GetType()).ToArray();
            var dstTypes = methodParameters.Select(p => p.ParameterType).ToArray();
            if (AlreadyInCorrectOrderByType(srcTypes, dstTypes))
                return parameters; // no need to change anything here and we don't have to care about parameters with the same type
            if (dstTypes.Distinct().Count() != dstTypes.Length)
                throw new UnresolveableParameterOrderMismatchException(dstTypes, methodInfo);
            return Reorder(parameters, dstTypes);
        }

        private object[] Reorder(object[] parameters, Type[] dstTypes)
        {
            return dstTypes
                .Select(type => FindBestMatchFor(type, parameters))
                .ToArray();
        }

        private static object FindBestMatchFor(Type type, object[] parameters)
        {
            return parameters.FirstOrDefault(p => p.GetType() == type);
        }

        private static bool AlreadyInCorrectOrderByType(
            Type[] srcTypes,
            Type[] dstTypes
        )
        {
            for (var i = 0; i < srcTypes.Length; i++)
            {
                var src = srcTypes[i];
                var dst = dstTypes[i];
                if (src == null && dst.IsPrimitive)
                    return false;
                if (src != dst)
                    return false;
            }
            return true;
        }

        private void LocallyCachePropertyInfos()
        {
            _localPropertyInfos = _isFuzzy
                ? _propertyInfos[_wrappedType].FuzzyPropertyInfos
                : _propertyInfos[_wrappedType].PropertyInfos;
            if (_wrappingADuck)
                _localFieldInfos = _fieldInfos[_wrappedType];
        }

        private void LocallyCacheMimickedPropertyInfos()
        {
            _localMimickPropertyInfos = _isFuzzy
                ? _mimickedPropInfos.FuzzyPropertyInfos
                : _mimickedPropInfos.PropertyInfos;
        }

        private void LocallyCacheMethodInfos()
        {
            _localMethodInfos = _isFuzzy
                ? _methodInfos[_wrappedType].FuzzyMethodInfos
                : _methodInfos[_wrappedType].MethodInfos;
        }

        private void StaticallyCachePropertyInfosFor(object toCacheFor, bool cacheFieldInfosToo)
        {
            lock (_propertyInfos)
            {
                var type = toCacheFor.GetType();
                if (_propertyInfos.ContainsKey(type))
                    return;
                _propertyInfos[type] = new PropertyInfoContainer(
                    _propertyInfoFetcher
                        .GetPropertiesFor(toCacheFor,
                            BindingFlags.Instance | BindingFlags.Public));
                if (!cacheFieldInfosToo)
                    return;
                _fieldInfos[type] = type
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .ToDictionary(
                        fi => fi.Name,
                        fi => fi
                    );
            }
        }

        private bool IsObjectADuck()
        {
            return _wrappedType.GetCustomAttributes(true).OfType<IsADuckAttribute>().Any();
        }

        private readonly ListDictionary _propertyInfoLookupCache = new ListDictionary();
        private struct PropertyInfoCacheItem
        {
            public PropertyInfo PropertyInfo;
            public Action<object> Setter;
            public Func<object> Getter;
        }

        private PropertyInfoCacheItem FindPropertyInfoFor(string propertyName)
        {
            PropertyInfoCacheItem cacheItem;
            if (_propertyInfoLookupCache.Contains(propertyName))
                return (PropertyInfoCacheItem)_propertyInfoLookupCache[propertyName];
            object shimmed;
            if (_shimmedProperties.TryGetValue(propertyName, out shimmed))
            {
                cacheItem = CreateCacheItemFor(_localMimickPropertyInfos[propertyName]);
                _propertyInfoLookupCache[propertyName] = cacheItem;
                return cacheItem;
            }
            PropertyInfo pi;
            if (!_localPropertyInfos.TryGetValue(propertyName, out pi))
                throw new PropertyNotFoundException(_wrappedType, propertyName);
            cacheItem = CreateCacheItemFor(pi);
            _propertyInfoLookupCache[propertyName] = cacheItem;
            return cacheItem;
        }

        private PropertyInfoCacheItem CreateCacheItemFor(PropertyInfo propertyInfo)
        {
            return new PropertyInfoCacheItem
            {
                PropertyInfo = propertyInfo,
                Setter = CreateSetterExpressionFor(propertyInfo),
                Getter = CreateGetterExpressionFor(propertyInfo)
            };
        }

        private void SetFieldValue(string propertyName, object newValue)
        {
            var fieldInfo = FindPrivateBackingFieldFor(propertyName);
            fieldInfo.SetValue(_wrapped, newValue);
        }

        private object FieldValueFor(string propertyName)
        {
            var fieldInfo = FindPrivateBackingFieldFor(propertyName);
            return fieldInfo.GetValue(_wrapped);
        }

        private FieldInfo FindPrivateBackingFieldFor(string propertyName)
        {
            var seek = "_" + propertyName;
            FieldInfo fieldInfo;
            if (!_localFieldInfos.TryGetValue(seek, out fieldInfo))
                throw new BackingFieldForPropertyNotFoundException(_wrappedType, propertyName);
            return fieldInfo;
        }


        // borrowed from http://stackoverflow.com/questions/17660097/is-it-possible-to-speed-this-method-up/17669142#17669142
        //  -> use compiled lambdas for property get / set which provides a performance
        //      boost in that the runtime doesn't have to perform as much checking

        private Action<object> CreateSetterExpressionFor(PropertyInfo propertyInfo)
        {
            var methodInfo = propertyInfo.GetSetMethod();
            if (methodInfo == null)
                return null;
            var exValue = Expression.Parameter(typeof(object), "p");
            var exTarget = Expression.Constant(_wrapped);
            var exBody = Expression.Call(exTarget, methodInfo,
                Expression.Convert(exValue, propertyInfo.PropertyType));

            var lambda = Expression.Lambda<Action<object>>(exBody, exValue);
            var action = lambda.Compile();
            return action;
        }

        private void TrySetValue(object newValue, Type newValueType, PropertyInfoCacheItem propInfo)
        {
            var pType = propInfo.PropertyInfo.PropertyType;
            var setter = propInfo.Setter;
            if (pType.IsAssignableFrom(newValueType))
            {
                setter(newValue);
                return;
            }
            var converter = ConverterLocator.GetConverter(newValueType, pType);
            if (converter == null)
                throw new InvalidOperationException($"Unable to set property: no converter for {newValueType.Name} => {pType.Name}");
            var converted = ConvertWith(converter, newValue, pType);
            setter(converted);
        }

        private Func<object> CreateGetterExpressionFor(PropertyInfo propertyInfo)
        {
            var methodInfo = propertyInfo.GetGetMethod();
            if (methodInfo == null)
                return null;

            var exTarget = Expression.Constant(_wrapped);
            var exBody = Expression.Call(exTarget, methodInfo);
            var exBody2 = Expression.Convert(exBody, typeof(object));

            var lambda = Expression.Lambda<Func<object>>(exBody2);

            var action = lambda.Compile();
            return action;
        }

    }

}