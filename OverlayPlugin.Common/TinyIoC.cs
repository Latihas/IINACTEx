//===============================================================================
// TinyIoC
//
// An easy to use, hassle free, Inversion of Control Container for small projects
// and beginners alike.
//
// https://github.com/grumpydev/TinyIoC
//===============================================================================
// Copyright © Steven Robbins.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

#region Preprocessor Directives

#define EXPRESSIONS // Platform supports System.Linq.Expressions
#define COMPILED_EXPRESSIONS // Platform supports compiling expressions
#define APPDOMAIN_GETASSEMBLIES // Platform supports getting all assemblies from the AppDomain object
#define UNBOUND_GENERICS_GETCONSTRUCTORS // Platform supports GetConstructors on unbound generic types
#define GETPARAMETERS_OPEN_GENERICS // Platform supports GetParameters on open generics
#define RESOLVE_OPEN_GENERICS // Platform supports resolving open generics
#define READER_WRITER_LOCK_SLIM // Platform supports ReaderWriterLockSlim

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

// ReSharper disable All
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8714 // 类型不能用作泛型类型或方法中的类型参数。类型参数的为 Null 性与 "notnull" 约束不匹配。

#pragma warning disable CS8619 #pragma warning disable CS8603

#endregion

namespace RainbowMage.OverlayPlugin;

#region SafeDictionary

#if READER_WRITER_LOCK_SLIM
public
	class SafeDictionary<TKey, TValue> : IDisposable {
	private readonly ReaderWriterLockSlim _padlock = new();
	private readonly Dictionary<TKey, TValue> _Dictionary = new();

	public TValue this[TKey key] {
		set {
			_padlock.EnterWriteLock();

			try {
				if (_Dictionary.TryGetValue(key, out var current)) {
					if (current is IDisposable disposable)
						disposable.Dispose();
				}

				_Dictionary[key] = value;
			} finally {
				_padlock.ExitWriteLock();
			}
		}
	}

	public bool TryGetValue(TKey key, out TValue value) {
		_padlock.EnterReadLock();
		try {
			return _Dictionary.TryGetValue(key, out value);
		} finally {
			_padlock.ExitReadLock();
		}
	}

	public bool Remove(TKey key) {
		_padlock.EnterWriteLock();
		try {
			return _Dictionary.Remove(key);
		} finally {
			_padlock.ExitWriteLock();
		}
	}

	public void Clear() {
		_padlock.EnterWriteLock();
		try {
			_Dictionary.Clear();
		} finally {
			_padlock.ExitWriteLock();
		}
	}

	public IEnumerable<TKey> Keys {
		get {
			_padlock.EnterReadLock();
			try {
				return new List<TKey>(_Dictionary.Keys);
			} finally {
				_padlock.ExitReadLock();
			}
		}
	}

	#region IDisposable Members

	public void Dispose() {
		_padlock.EnterWriteLock();

		try {
			var disposableItems = from item in _Dictionary.Values
				where item is IDisposable
				select item as IDisposable;

			foreach (var item in disposableItems) {
				item.Dispose();
			}
		} finally {
			_padlock.ExitWriteLock();
		}

		GC.SuppressFinalize(this);
	}

	#endregion
}
#endif

#endregion

#region Extensions

public static class AssemblyExtensions {
	public static Type[] SafeGetTypes(this Assembly assembly) {
		Type[] assemblies;

		try {
			assemblies = assembly.GetTypes();
		} catch (FileNotFoundException) {
			assemblies = [];
		} catch (NotSupportedException) {
			assemblies = [];
		}
#if !NETFX_CORE
		catch (ReflectionTypeLoadException e) {
			assemblies = e.Types.Where(t => t != null).ToArray();
		}
#endif
		return assemblies;
	}
}

public static class TypeExtensions {
	private static readonly SafeDictionary<GenericMethodCacheKey, MethodInfo> _genericMethodCache;

	static TypeExtensions() {
		_genericMethodCache = new SafeDictionary<GenericMethodCacheKey, MethodInfo>();
	}


	public static MethodInfo GetGenericMethod(
		this Type sourceType, BindingFlags bindingFlags, string methodName, Type[] genericTypes,
		Type[] parameterTypes) {
		var cacheKey = new GenericMethodCacheKey(sourceType, methodName, genericTypes, parameterTypes);

		if (!_genericMethodCache.TryGetValue(cacheKey, out var method)) {
			method = GetMethod(sourceType, bindingFlags, methodName, genericTypes, parameterTypes);
			_genericMethodCache[cacheKey] = method;
		}

		return method;
	}

#if NETFX_CORE
        private static MethodInfo GetMethod(Type sourceType, BindingFlags flags, string methodName, Type[] genericTypes, Type[] parameterTypes)
        {
            var methods =
                sourceType.GetMethods(flags).Where(
                    mi => string.Equals(methodName, mi.Name, StringComparison.Ordinal)).Where(
                        mi => mi.ContainsGenericParameters).Where(mi => mi.GetGenericArguments().Length == genericTypes.Length).
                    Where(mi => mi.GetParameters().Length == parameterTypes.Length).Select(
                        mi => mi.MakeGenericMethod(genericTypes)).Where(
                            mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(parameterTypes)).ToList();

            if (methods.Count > 1)
            {
                throw new AmbiguousMatchException();
            }

            return methods.FirstOrDefault();
        }
#else
	private static MethodInfo GetMethod(
		Type sourceType, BindingFlags bindingFlags, string methodName, Type[] genericTypes, Type[] parameterTypes) {
#if GETPARAMETERS_OPEN_GENERICS
		var methods =
			sourceType.GetMethods(bindingFlags).Where(mi => string.Equals(methodName, mi.Name, StringComparison.Ordinal)).Where(mi => mi.ContainsGenericParameters)
				.Where(mi => mi.GetGenericArguments().Length == genericTypes.Length)
				.Where(mi => mi.GetParameters().Length == parameterTypes.Length).Select(mi => mi.MakeGenericMethod(genericTypes)).Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(parameterTypes))
				.ToList();
#else
            var validMethods = from method in sourceType.GetMethods(bindingFlags)
                                where method.Name == methodName
                                where method.IsGenericMethod
                                where method.GetGenericArguments().Length == genericTypes.Length
                                let genericMethod = method.MakeGenericMethod(genericTypes)
                                where genericMethod.GetParameters().Count() == parameterTypes.Length
                                where genericMethod.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(parameterTypes)
                                select genericMethod;

            var methods = validMethods.ToList();
#endif
		if (methods.Count > 1) {
			throw new AmbiguousMatchException();
		}

		return methods.FirstOrDefault();
	}
#endif

	private sealed class GenericMethodCacheKey {
		private readonly Type _sourceType;

		private readonly string _methodName;

		private readonly Type[] _genericTypes;

		private readonly Type[] _parameterTypes;

		private readonly int _hashCode;

		public GenericMethodCacheKey(Type sourceType, string methodName, Type[] genericTypes, Type[] parameterTypes) {
			_sourceType = sourceType;
			_methodName = methodName;
			_genericTypes = genericTypes;
			_parameterTypes = parameterTypes;
			_hashCode = GenerateHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is not GenericMethodCacheKey cacheKey)
				return false;

			if (_sourceType != cacheKey._sourceType)
				return false;

			if (!string.Equals(_methodName, cacheKey._methodName, StringComparison.Ordinal))
				return false;

			if (_genericTypes.Length != cacheKey._genericTypes.Length)
				return false;

			if (_parameterTypes.Length != cacheKey._parameterTypes.Length)
				return false;

			for (var i = 0; i < _genericTypes.Length; ++i) {
				if (_genericTypes[i] != cacheKey._genericTypes[i])
					return false;
			}

			for (var i = 0; i < _parameterTypes.Length; ++i) {
				if (_parameterTypes[i] != cacheKey._parameterTypes[i])
					return false;
			}

			return true;
		}

		public override int GetHashCode() => _hashCode;

		private int GenerateHashCode() {
			unchecked {
				var result = _sourceType.GetHashCode();

				result = result * 397 ^ _methodName.GetHashCode();

				for (var i = 0; i < _genericTypes.Length; ++i) {
					result = result * 397 ^ _genericTypes[i].GetHashCode();
				}

				for (var i = 0; i < _parameterTypes.Length; ++i) {
					result = result * 397 ^ _parameterTypes[i].GetHashCode();
				}

				return result;
			}
		}
	}
}

#if NETFX_CORE
	internal static class ListExtender
	{
		internal static void ForEach<T>(this List<T> list, Action<T> callback)
		{
			foreach (T obj in list)
				callback(obj);
		}
	}
#endif

#endregion

#region TinyIoC Exception Types

public class TinyIoCResolutionException : Exception {
	private const string ERROR_TEXT = "Unable to resolve type: {0}";

	public TinyIoCResolutionException(Type type)
		: base(string.Format(ERROR_TEXT, type.FullName)) {
	}

	public TinyIoCResolutionException(Type type, Exception innerException)
		: base(string.Format(ERROR_TEXT, type.FullName), innerException) {
	}
}

public class TinyIoCRegistrationTypeException : Exception {
	private const string REGISTER_ERROR_TEXT =
		"Cannot register type {0} - abstract classes or interfaces are not valid implementation types for {1}.";

	public TinyIoCRegistrationTypeException(Type type, string factory)
		: base(string.Format(REGISTER_ERROR_TEXT, type.FullName, factory)) {
	}

	public TinyIoCRegistrationTypeException(Type type, string factory, Exception innerException)
		: base(string.Format(REGISTER_ERROR_TEXT, type.FullName, factory), innerException) {
	}
}

public class TinyIoCRegistrationException : Exception {
	private const string CONVERT_ERROR_TEXT = "Cannot convert current registration of {0} to {1}";
	private const string GENERIC_CONSTRAINT_ERROR_TEXT = "Type {1} is not valid for a registration of type {0}";

	public TinyIoCRegistrationException(Type type, string method)
		: base(string.Format(CONVERT_ERROR_TEXT, type.FullName, method)) {
	}

	public TinyIoCRegistrationException(Type type, string method, Exception innerException)
		: base(string.Format(CONVERT_ERROR_TEXT, type.FullName, method), innerException) {
	}

	public TinyIoCRegistrationException(Type registerType, Type implementationType)
		: base(string.Format(GENERIC_CONSTRAINT_ERROR_TEXT, registerType.FullName, implementationType.FullName)) {
	}

	public TinyIoCRegistrationException(Type registerType, Type implementationType, Exception innerException)
		: base(string.Format(GENERIC_CONSTRAINT_ERROR_TEXT, registerType.FullName, implementationType.FullName),
			innerException) {
	}
}

public class TinyIoCWeakReferenceException : Exception {
	private const string ERROR_TEXT = "Unable to instantiate {0} - referenced object has been reclaimed";

	public TinyIoCWeakReferenceException(Type type)
		: base(string.Format(ERROR_TEXT, type.FullName)) {
	}

	public TinyIoCWeakReferenceException(Type type, Exception innerException)
		: base(string.Format(ERROR_TEXT, type.FullName), innerException) {
	}
}

public class TinyIoCConstructorResolutionException : Exception {
	private const string ERROR_TEXT = "Unable to resolve constructor for {0} using provided Expression.";

	public TinyIoCConstructorResolutionException(Type type)
		: base(string.Format(ERROR_TEXT, type.FullName)) {
	}

	public TinyIoCConstructorResolutionException(Type type, Exception innerException)
		: base(string.Format(ERROR_TEXT, type.FullName), innerException) {
	}

	public TinyIoCConstructorResolutionException(string message, Exception innerException)
		: base(message, innerException) {
	}

	public TinyIoCConstructorResolutionException(string message)
		: base(message) {
	}
}

public class TinyIoCAutoRegistrationException : Exception {
	private const string ERROR_TEXT = "Duplicate implementation of type {0} found ({1}).";

	public TinyIoCAutoRegistrationException(Type registerType, IEnumerable<Type> types)
		: base(string.Format(ERROR_TEXT, registerType, GetTypesString(types))) {
	}

	public TinyIoCAutoRegistrationException(Type registerType, IEnumerable<Type> types, Exception innerException)
		: base(string.Format(ERROR_TEXT, registerType, GetTypesString(types)), innerException) {
	}

	private static string GetTypesString(IEnumerable<Type> types) {
		var typeNames = from type in types
			select type.FullName;

		return string.Join(",", typeNames.ToArray());
	}
}

#endregion

#region Public Setup / Settings Classes

public sealed class NamedParameterOverloads : Dictionary<string, object> {
	public static NamedParameterOverloads FromIDictionary(IDictionary<string, object> data) => data as NamedParameterOverloads ?? new NamedParameterOverloads(data);

	public NamedParameterOverloads() {
	}

	public NamedParameterOverloads(IDictionary<string, object> data)
		: base(data) {
	}

	public static NamedParameterOverloads Default { get; } = new();
}

public enum UnregisteredResolutionActions {
	AttemptResolve,

	Fail,

	GenericsOnly
}

public enum NamedResolutionFailureActions {
	AttemptUnnamedResolution,
	Fail
}

public enum DuplicateImplementationActions {
	RegisterSingle,
	RegisterMultiple,
	Fail
}

public sealed class ResolveOptions {
	private static readonly ResolveOptions _Default = new();
	private static readonly ResolveOptions _FailUnregisteredAndNameNotFound = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.Fail,
		UnregisteredResolutionAction = UnregisteredResolutionActions.Fail
	};
	private static readonly ResolveOptions _FailUnregisteredOnly = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.AttemptUnnamedResolution,
		UnregisteredResolutionAction = UnregisteredResolutionActions.Fail
	};
	private static readonly ResolveOptions _FailNameNotFoundOnly = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.Fail,
		UnregisteredResolutionAction = UnregisteredResolutionActions.AttemptResolve
	};

	public UnregisteredResolutionActions UnregisteredResolutionAction { get; set; } =
		UnregisteredResolutionActions.AttemptResolve;

	public NamedResolutionFailureActions NamedResolutionFailureAction { get; set; } =
		NamedResolutionFailureActions.Fail;

	public static ResolveOptions Default { get; } = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.Fail,
		UnregisteredResolutionAction = UnregisteredResolutionActions.Fail
	};

	public static ResolveOptions FailNameNotFoundOnly { get; } = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.Fail,
		UnregisteredResolutionAction = UnregisteredResolutionActions.AttemptResolve
	};

	public static ResolveOptions FailUnregisteredAndNameNotFound => Default;

	public static ResolveOptions FailUnregisteredOnly { get; } = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.AttemptUnnamedResolution,
		UnregisteredResolutionAction = UnregisteredResolutionActions.Fail
	};
}

#endregion

public sealed class TinyIoCContainer : IDisposable {
	#region Fake NETFX_CORE Classes

#if NETFX_CORE
        private sealed class MethodAccessException : Exception
        {
        }

        private sealed class AppDomain
        {
            public static AppDomain CurrentDomain { get; private set; }

            static AppDomain()
            {
                CurrentDomain = new AppDomain();
            }

			            public async Task<List<Assembly>> GetAssembliesAsync()
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

                List<Assembly> assemblies = new List<Assembly>();

				var files = await folder.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    if (file.FileType == ".dll" || file.FileType == ".exe")
                    {
                        AssemblyName name = new AssemblyName() { Name =
 System.IO.Path.GetFileNameWithoutExtension(file.Name) };
						try
						{
							var asm = Assembly.Load(name);
							assemblies.Add(asm);
						}
						catch
						{
													}
                    }
                }

				return assemblies;
            }
        }
#endif

	#endregion

	#region "Fluent" API

	public sealed class RegisterOptions {
		private readonly TinyIoCContainer _Container;
		private readonly TypeRegistration _Registration;

		public RegisterOptions(TinyIoCContainer container, TypeRegistration registration) {
			_Container = container;
			_Registration = registration;
		}

		public RegisterOptions AsSingleton() {
			var currentFactory = _Container.GetCurrentFactory(_Registration);

			if (currentFactory == null)
				throw new TinyIoCRegistrationException(_Registration.Type, "singleton");

			return _Container.AddUpdateRegistration(_Registration, currentFactory.SingletonVariant);
		}

		public RegisterOptions AsMultiInstance() {
			var currentFactory = _Container.GetCurrentFactory(_Registration);

			if (currentFactory == null)
				throw new TinyIoCRegistrationException(_Registration.Type, "multi-instance");

			return _Container.AddUpdateRegistration(_Registration, currentFactory.MultiInstanceVariant);
		}

		public RegisterOptions WithWeakReference() {
			var currentFactory = _Container.GetCurrentFactory(_Registration);

			if (currentFactory == null)
				throw new TinyIoCRegistrationException(_Registration.Type, "weak reference");

			return _Container.AddUpdateRegistration(_Registration, currentFactory.WeakReferenceVariant);
		}

		public RegisterOptions WithStrongReference() {
			var currentFactory = _Container.GetCurrentFactory(_Registration);

			if (currentFactory == null)
				throw new TinyIoCRegistrationException(_Registration.Type, "strong reference");

			return _Container.AddUpdateRegistration(_Registration, currentFactory.StrongReferenceVariant);
		}

#if EXPRESSIONS
		public RegisterOptions UsingConstructor<RegisterType>(Expression<Func<RegisterType>> constructor) {
			if (constructor is not LambdaExpression lambda)
				throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

			if (lambda.Body is not NewExpression newExpression)
				throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

			var constructorInfo = newExpression.Constructor;
			if (constructorInfo == null)
				throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

			var currentFactory = _Container.GetCurrentFactory(_Registration);
			if (currentFactory == null)
				throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

			currentFactory.SetConstructor(constructorInfo);

			return this;
		}
#endif
		public static RegisterOptions ToCustomLifetimeManager(
			RegisterOptions instance, ITinyIoCObjectLifetimeProvider lifetimeProvider, string errorString) {
			if (instance == null)
				throw new ArgumentNullException(nameof(instance), "instance is null.");

			if (lifetimeProvider == null)
				throw new ArgumentNullException(nameof(lifetimeProvider), "lifetimeProvider is null.");

			if (string.IsNullOrEmpty(errorString))
				throw new ArgumentException("errorString is null or empty.", nameof(errorString));

			var currentFactory = instance._Container.GetCurrentFactory(instance._Registration);

			if (currentFactory == null)
				throw new TinyIoCRegistrationException(instance._Registration.Type, errorString);

			return instance._Container.AddUpdateRegistration(instance._Registration,
				currentFactory.GetCustomObjectLifetimeVariant(
					lifetimeProvider, errorString));
		}
	}

	public sealed class MultiRegisterOptions {
		private IEnumerable<RegisterOptions> _RegisterOptions;

		public MultiRegisterOptions(IEnumerable<RegisterOptions> registerOptions) {
			_RegisterOptions = registerOptions;
		}

		public MultiRegisterOptions AsSingleton() {
			_RegisterOptions = ExecuteOnAllRegisterOptions(ro => ro.AsSingleton());
			return this;
		}

		public MultiRegisterOptions AsMultiInstance() {
			_RegisterOptions = ExecuteOnAllRegisterOptions(ro => ro.AsMultiInstance());
			return this;
		}

		public static MultiRegisterOptions ToCustomLifetimeManager(
			MultiRegisterOptions instance,
			ITinyIoCObjectLifetimeProvider lifetimeProvider,
			string errorString) {
			if (instance == null)
				throw new ArgumentNullException(nameof(instance), "instance is null.");

			if (lifetimeProvider == null)
				throw new ArgumentNullException(nameof(lifetimeProvider), "lifetimeProvider is null.");

			if (string.IsNullOrEmpty(errorString))
				throw new ArgumentException("errorString is null or empty.", nameof(errorString));

			instance._RegisterOptions =
				instance.ExecuteOnAllRegisterOptions(ro => RegisterOptions.ToCustomLifetimeManager(ro, lifetimeProvider, errorString));

			return instance;
		}

		private List<RegisterOptions> ExecuteOnAllRegisterOptions(
			Func<RegisterOptions, RegisterOptions> action) {
			var newRegisterOptions = new List<RegisterOptions>();

			foreach (var registerOption in _RegisterOptions) {
				newRegisterOptions.Add(action(registerOption));
			}

			return newRegisterOptions;
		}
	}

	#endregion

	#region Public API

	#region Child Containers

	public TinyIoCContainer GetChildContainer() => new(this);

	#endregion

	#region Registration

	public void AutoRegister() {
#if APPDOMAIN_GETASSEMBLIES
		AutoRegisterInternal(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsIgnoredAssembly(a)),
			DuplicateImplementationActions.RegisterSingle, null);
#else
            AutoRegisterInternal(new Assembly[] {this.GetType().Assembly()}, true, null);
#endif
	}

	public void AutoRegister(Func<Type, bool> registrationPredicate) {
#if APPDOMAIN_GETASSEMBLIES
		AutoRegisterInternal(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsIgnoredAssembly(a)),
			DuplicateImplementationActions.RegisterSingle, registrationPredicate);
#else
            AutoRegisterInternal(new Assembly[] { this.GetType().Assembly()}, true, registrationPredicate);
#endif
	}

	public void AutoRegister(DuplicateImplementationActions duplicateAction) {
#if APPDOMAIN_GETASSEMBLIES
		AutoRegisterInternal(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsIgnoredAssembly(a)),
			duplicateAction, null);
#else
            AutoRegisterInternal(new Assembly[] { this.GetType().Assembly() }, ignoreDuplicateImplementations, null);
#endif
	}

	public void AutoRegister(DuplicateImplementationActions duplicateAction, Func<Type, bool> registrationPredicate) {
#if APPDOMAIN_GETASSEMBLIES
		AutoRegisterInternal(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsIgnoredAssembly(a)),
			duplicateAction, registrationPredicate);
#else
            AutoRegisterInternal(new Assembly[] { this.GetType().Assembly() }, ignoreDuplicateImplementations, registrationPredicate);
#endif
	}

	public void AutoRegister(IEnumerable<Assembly> assemblies) {
		AutoRegisterInternal(assemblies, DuplicateImplementationActions.RegisterSingle, null);
	}

	public void AutoRegister(IEnumerable<Assembly> assemblies, Func<Type, bool> registrationPredicate) {
		AutoRegisterInternal(assemblies, DuplicateImplementationActions.RegisterSingle, registrationPredicate);
	}

	public void AutoRegister(IEnumerable<Assembly> assemblies, DuplicateImplementationActions duplicateAction) {
		AutoRegisterInternal(assemblies, duplicateAction, null);
	}

	public void AutoRegister(
		IEnumerable<Assembly> assemblies, DuplicateImplementationActions duplicateAction,
		Func<Type, bool> registrationPredicate) {
		AutoRegisterInternal(assemblies, duplicateAction, registrationPredicate);
	}

	public RegisterOptions Register(Type registerType) => RegisterInternal(registerType, string.Empty, GetDefaultObjectFactory(registerType, registerType));

	public RegisterOptions Register(Type registerType, string name) => RegisterInternal(registerType, name, GetDefaultObjectFactory(registerType, registerType));

	public RegisterOptions Register(Type registerType, Type registerImplementation) =>
		RegisterInternal(registerType, string.Empty,
			GetDefaultObjectFactory(registerType, registerImplementation));

	public RegisterOptions Register(Type registerType, Type registerImplementation, string name) =>
		RegisterInternal(registerType, name,
			GetDefaultObjectFactory(registerType, registerImplementation));

	public RegisterOptions Register(Type registerType, object instance) =>
		RegisterInternal(registerType, string.Empty,
			new InstanceFactory(registerType, registerType, instance));

	public RegisterOptions Register(Type registerType, object instance, string name) => RegisterInternal(registerType, name, new InstanceFactory(registerType, registerType, instance));

	public RegisterOptions Register(Type registerType, Type registerImplementation, object instance) =>
		RegisterInternal(registerType, string.Empty,
			new InstanceFactory(registerType, registerImplementation, instance));

	public RegisterOptions Register(Type registerType, Type registerImplementation, object instance, string name) =>
		RegisterInternal(registerType, name,
			new InstanceFactory(registerType, registerImplementation, instance));

	public RegisterOptions Register(
		Type registerType, Func<TinyIoCContainer, NamedParameterOverloads, object> factory) =>
		RegisterInternal(registerType, string.Empty, new DelegateFactory(registerType, factory));

	public RegisterOptions Register(
		Type registerType, Func<TinyIoCContainer, NamedParameterOverloads, object> factory, string name) =>
		RegisterInternal(registerType, name, new DelegateFactory(registerType, factory));

	public RegisterOptions Register<RegisterType>()
		where RegisterType : class =>
		Register(typeof(RegisterType));

	public RegisterOptions Register<RegisterType>(string name)
		where RegisterType : class =>
		Register(typeof(RegisterType), name);

	public RegisterOptions Register<RegisterType, RegisterImplementation>()
		where RegisterType : class
		where RegisterImplementation : class, RegisterType =>
		Register(typeof(RegisterType), typeof(RegisterImplementation));

	public RegisterOptions Register<RegisterType, RegisterImplementation>(string name)
		where RegisterType : class
		where RegisterImplementation : class, RegisterType =>
		Register(typeof(RegisterType), typeof(RegisterImplementation), name);

	public RegisterOptions Register<RegisterType>(RegisterType instance)
		where RegisterType : class =>
		Register(typeof(RegisterType), instance);

	public RegisterOptions Register<RegisterType>(RegisterType instance, string name)
		where RegisterType : class =>
		Register(typeof(RegisterType), instance, name);

	public RegisterOptions Register<RegisterType, RegisterImplementation>(RegisterImplementation instance)
		where RegisterType : class
		where RegisterImplementation : class, RegisterType =>
		Register(typeof(RegisterType), typeof(RegisterImplementation), instance);

	public RegisterOptions Register<RegisterType, RegisterImplementation>(
		RegisterImplementation instance, string name)
		where RegisterType : class
		where RegisterImplementation : class, RegisterType =>
		Register(typeof(RegisterType), typeof(RegisterImplementation), instance, name);

	public RegisterOptions Register<RegisterType>(
		Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory)
		where RegisterType : class {
		ArgumentNullException.ThrowIfNull(factory);

		return Register(typeof(RegisterType), (c, o) => factory(c, o));
	}

	public RegisterOptions Register<RegisterType>(
		Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory, string name)
		where RegisterType : class {
		ArgumentNullException.ThrowIfNull(factory);

		return Register(typeof(RegisterType), (c, o) => factory(c, o), name);
	}

	public MultiRegisterOptions RegisterMultiple<RegisterType>(IEnumerable<Type> implementationTypes) => RegisterMultiple(typeof(RegisterType), implementationTypes);

	public MultiRegisterOptions RegisterMultiple(Type registrationType, IEnumerable<Type> implementationTypes) {
		if (implementationTypes == null)
			throw new ArgumentNullException("types", "types is null.");

		foreach (var type in implementationTypes)
			if (!registrationType.IsAssignableFrom(type))
				throw new ArgumentException(string.Format("types: The type {0} is not assignable from {1}",
					registrationType.FullName, type.FullName));

		if (implementationTypes.Count() != implementationTypes.Distinct().Count()) {
			var queryForDuplicatedTypes = from i in implementationTypes
				group i by i
				into j
				where j.Count() > 1
				select j.Key.FullName;

			var fullNamesOfDuplicatedTypes = string.Join(",\n", queryForDuplicatedTypes.ToArray());
			var multipleRegMessage =
				string.Format(
					"types: The same implementation type cannot be specified multiple times for {0}\n\n{1}",
					registrationType.FullName, fullNamesOfDuplicatedTypes);
			throw new ArgumentException(multipleRegMessage);
		}

		var registerOptions = new List<RegisterOptions>();

		foreach (var type in implementationTypes) {
			registerOptions.Add(Register(registrationType, type, type.FullName));
		}

		return new MultiRegisterOptions(registerOptions);
	}

	#endregion

	#region Resolution

	public object Resolve(Type resolveType) =>
		ResolveInternal(new TypeRegistration(resolveType), NamedParameterOverloads.Default,
			ResolveOptions.Default);

	public object Resolve(Type resolveType, ResolveOptions options) => ResolveInternal(new TypeRegistration(resolveType), NamedParameterOverloads.Default, options);

	public object Resolve(Type resolveType, string name) =>
		ResolveInternal(new TypeRegistration(resolveType, name), NamedParameterOverloads.Default,
			ResolveOptions.Default);

	public object Resolve(Type resolveType, string name, ResolveOptions options) => ResolveInternal(new TypeRegistration(resolveType, name), NamedParameterOverloads.Default, options);

	public object Resolve(Type resolveType, NamedParameterOverloads parameters) => ResolveInternal(new TypeRegistration(resolveType), parameters, ResolveOptions.Default);

	public object Resolve(Type resolveType, NamedParameterOverloads parameters, ResolveOptions options) => ResolveInternal(new TypeRegistration(resolveType), parameters, options);

	public object Resolve(Type resolveType, string name, NamedParameterOverloads parameters) => ResolveInternal(new TypeRegistration(resolveType, name), parameters, ResolveOptions.Default);

	public object Resolve(Type resolveType, string name, NamedParameterOverloads parameters, ResolveOptions options) => ResolveInternal(new TypeRegistration(resolveType, name), parameters, options);

	public ResolveType Resolve<ResolveType>()
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType));

	public ResolveType Resolve<ResolveType>(ResolveOptions options)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), options);

	public ResolveType Resolve<ResolveType>(string name)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), name);

	public ResolveType Resolve<ResolveType>(string name, ResolveOptions options)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), name, options);

	public ResolveType Resolve<ResolveType>(NamedParameterOverloads parameters)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), parameters);

	public ResolveType Resolve<ResolveType>(NamedParameterOverloads parameters, ResolveOptions options)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), parameters, options);

	public ResolveType Resolve<ResolveType>(string name, NamedParameterOverloads parameters)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), name, parameters);

	public ResolveType Resolve<ResolveType>(string name, NamedParameterOverloads parameters, ResolveOptions options)
		where ResolveType : class =>
		(ResolveType)Resolve(typeof(ResolveType), name, parameters, options);

	public bool CanResolve(Type resolveType) =>
		CanResolveInternal(new TypeRegistration(resolveType), NamedParameterOverloads.Default,
			ResolveOptions.Default);

	private bool CanResolve(Type resolveType, string name) =>
		CanResolveInternal(new TypeRegistration(resolveType, name), NamedParameterOverloads.Default,
			ResolveOptions.Default);

	public bool CanResolve(Type resolveType, ResolveOptions options) => CanResolveInternal(new TypeRegistration(resolveType), NamedParameterOverloads.Default, options);

	public bool CanResolve(Type resolveType, string name, ResolveOptions options) =>
		CanResolveInternal(new TypeRegistration(resolveType, name), NamedParameterOverloads.Default,
			options);

	public bool CanResolve(Type resolveType, NamedParameterOverloads parameters) => CanResolveInternal(new TypeRegistration(resolveType), parameters, ResolveOptions.Default);

	public bool CanResolve(Type resolveType, string name, NamedParameterOverloads parameters) => CanResolveInternal(new TypeRegistration(resolveType, name), parameters, ResolveOptions.Default);

	public bool CanResolve(Type resolveType, NamedParameterOverloads parameters, ResolveOptions options) => CanResolveInternal(new TypeRegistration(resolveType), parameters, options);

	public bool CanResolve(
		Type resolveType, string name, NamedParameterOverloads parameters, ResolveOptions options) =>
		CanResolveInternal(new TypeRegistration(resolveType, name), parameters, options);

	public bool CanResolve<ResolveType>()
		where ResolveType : class =>
		CanResolve(typeof(ResolveType));

	public bool CanResolve<ResolveType>(string name)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), name);

	public bool CanResolve<ResolveType>(ResolveOptions options)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), options);

	public bool CanResolve<ResolveType>(string name, ResolveOptions options)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), name, options);

	public bool CanResolve<ResolveType>(NamedParameterOverloads parameters)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), parameters);

	public bool CanResolve<ResolveType>(string name, NamedParameterOverloads parameters)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), name, parameters);

	public bool CanResolve<ResolveType>(NamedParameterOverloads parameters, ResolveOptions options)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), parameters, options);

	public bool CanResolve<ResolveType>(string name, NamedParameterOverloads parameters, ResolveOptions options)
		where ResolveType : class =>
		CanResolve(typeof(ResolveType), name, parameters, options);

	public bool TryResolve(Type resolveType, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, ResolveOptions options, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, string name, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, string name, ResolveOptions options, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, NamedParameterOverloads parameters, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, parameters);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(
		Type resolveType, string name, NamedParameterOverloads parameters, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name, parameters);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(
		Type resolveType, NamedParameterOverloads parameters, ResolveOptions options, out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, parameters, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(
		Type resolveType, string name, NamedParameterOverloads parameters, ResolveOptions options,
		out object resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name, parameters, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>();
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(string name, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(name);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(string name, ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(name, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(NamedParameterOverloads parameters, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(parameters);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(
		string name, NamedParameterOverloads parameters, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(name, parameters);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(
		NamedParameterOverloads parameters, ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(parameters, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(
		string name, NamedParameterOverloads parameters, ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		try {
			resolvedType = Resolve<ResolveType>(name, parameters, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = default;
			return false;
		}
	}

	public IEnumerable<object> ResolveAll(Type resolveType, bool includeUnnamed) => ResolveAllInternal(resolveType, includeUnnamed);

	public IEnumerable<object> ResolveAll(Type resolveType) => ResolveAll(resolveType, false);

	public IEnumerable<ResolveType> ResolveAll<ResolveType>(bool includeUnnamed)
		where ResolveType : class =>
		ResolveAll(typeof(ResolveType), includeUnnamed).Cast<ResolveType>();

	public IEnumerable<ResolveType> ResolveAll<ResolveType>()
		where ResolveType : class =>
		ResolveAll<ResolveType>(true);

	public void BuildUp(object input) {
		BuildUpInternal(input, ResolveOptions.Default);
	}

	public void BuildUp(object input, ResolveOptions resolveOptions) {
		BuildUpInternal(input, resolveOptions);
	}

	#endregion

	#endregion

	#region Object Factories

	public interface ITinyIoCObjectLifetimeProvider {
		object GetObject();

		void SetObject(object value);

		void ReleaseObject();
	}

	private abstract class ObjectFactoryBase {
		public virtual bool AssumeConstruction => false;

		public abstract Type CreatesType { get; }

		public ConstructorInfo Constructor { get; protected set; }

		public abstract object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options);

		public virtual ObjectFactoryBase SingletonVariant =>
			throw new TinyIoCRegistrationException(GetType(), "singleton");

		public virtual ObjectFactoryBase MultiInstanceVariant =>
			throw new TinyIoCRegistrationException(GetType(), "multi-instance");

		public virtual ObjectFactoryBase StrongReferenceVariant =>
			throw new TinyIoCRegistrationException(GetType(), "strong reference");

		public virtual ObjectFactoryBase WeakReferenceVariant =>
			throw new TinyIoCRegistrationException(GetType(), "weak reference");

		public virtual ObjectFactoryBase GetCustomObjectLifetimeVariant(
			ITinyIoCObjectLifetimeProvider lifetimeProvider, string errorString) =>
			throw new TinyIoCRegistrationException(GetType(), errorString);

		public virtual void SetConstructor(ConstructorInfo constructor) {
			Constructor = constructor;
		}

		public virtual ObjectFactoryBase GetFactoryForChildContainer(
			Type type, TinyIoCContainer parent, TinyIoCContainer child) =>
			this;
	}

	private class MultiInstanceFactory : ObjectFactoryBase {
		private readonly Type registerType;
		private readonly Type registerImplementation;
		public override Type CreatesType => registerImplementation;

		public MultiInstanceFactory(Type registerType, Type registerImplementation) {
			if (registerImplementation.IsAbstract() || registerImplementation.IsInterface())
				throw new TinyIoCRegistrationTypeException(registerImplementation, "MultiInstanceFactory");
			if (!IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "MultiInstanceFactory");

			this.registerType = registerType;
			this.registerImplementation = registerImplementation;
		}

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			try {
				return container.ConstructType(requestedType, registerImplementation, Constructor, parameters,
					options);
			} catch (TinyIoCResolutionException ex) {
				throw new TinyIoCResolutionException(registerType, ex);
			}
		}

		public override ObjectFactoryBase SingletonVariant =>
			new SingletonFactory(registerType, registerImplementation);

		public override ObjectFactoryBase GetCustomObjectLifetimeVariant(
			ITinyIoCObjectLifetimeProvider lifetimeProvider, string errorString) =>
			new CustomObjectLifetimeFactory(registerType, registerImplementation, lifetimeProvider,
				errorString);

		public override ObjectFactoryBase MultiInstanceVariant => this;
	}

	private class DelegateFactory : ObjectFactoryBase {
		private readonly Type registerType;

		private readonly Func<TinyIoCContainer, NamedParameterOverloads, object> _factory;

		public override bool AssumeConstruction => true;

		public override Type CreatesType => registerType;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			try {
				return _factory.Invoke(container, parameters);
			} catch (Exception ex) {
				throw new TinyIoCResolutionException(registerType, ex);
			}
		}

		public DelegateFactory(Type registerType, Func<TinyIoCContainer, NamedParameterOverloads, object> factory) {
			ArgumentNullException.ThrowIfNull(factory);

			_factory = factory;

			this.registerType = registerType;
		}

		public override ObjectFactoryBase WeakReferenceVariant =>
			new WeakDelegateFactory(registerType, _factory);

		public override ObjectFactoryBase StrongReferenceVariant => this;

		public override void SetConstructor(ConstructorInfo constructor) {
			throw new TinyIoCConstructorResolutionException(
				"Constructor selection is not possible for delegate factory registrations");
		}
	}

	private class WeakDelegateFactory : ObjectFactoryBase {
		private readonly Type registerType;

		private readonly WeakReference _factory;

		public override bool AssumeConstruction => true;

		public override Type CreatesType => registerType;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			if (_factory.Target is not Func<TinyIoCContainer, NamedParameterOverloads, object> factory)
				throw new TinyIoCWeakReferenceException(registerType);

			try {
				return factory.Invoke(container, parameters);
			} catch (Exception ex) {
				throw new TinyIoCResolutionException(registerType, ex);
			}
		}

		public WeakDelegateFactory(
			Type registerType, Func<TinyIoCContainer, NamedParameterOverloads, object> factory) {
			ArgumentNullException.ThrowIfNull(factory);

			_factory = new WeakReference(factory);

			this.registerType = registerType;
		}

		public override ObjectFactoryBase StrongReferenceVariant {
			get {
				if (_factory.Target is not Func<TinyIoCContainer, NamedParameterOverloads, object> factory)
					throw new TinyIoCWeakReferenceException(registerType);

				return new DelegateFactory(registerType, factory);
			}
		}

		public override ObjectFactoryBase WeakReferenceVariant => this;

		public override void SetConstructor(ConstructorInfo constructor) {
			throw new TinyIoCConstructorResolutionException(
				"Constructor selection is not possible for delegate factory registrations");
		}
	}

	private class InstanceFactory : ObjectFactoryBase, IDisposable {
		private readonly Type registerType;
		private readonly Type registerImplementation;
		private readonly object _instance;

		public override bool AssumeConstruction => true;

		public InstanceFactory(Type registerType, Type registerImplementation, object instance) {
			if (!IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "InstanceFactory");

			this.registerType = registerType;
			this.registerImplementation = registerImplementation;
			_instance = instance;
		}

		public override Type CreatesType => registerImplementation;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) =>
			_instance;

		public override ObjectFactoryBase MultiInstanceVariant =>
			new MultiInstanceFactory(registerType, registerImplementation);

		public override ObjectFactoryBase WeakReferenceVariant =>
			new WeakInstanceFactory(registerType, registerImplementation, _instance);

		public override ObjectFactoryBase StrongReferenceVariant => this;

		public override void SetConstructor(ConstructorInfo constructor) {
			throw new TinyIoCConstructorResolutionException(
				"Constructor selection is not possible for instance factory registrations");
		}

		public void Dispose() {
			if (_instance is IDisposable disposable)
				disposable.Dispose();
		}
	}

	private class WeakInstanceFactory : ObjectFactoryBase, IDisposable {
		private readonly Type registerType;
		private readonly Type registerImplementation;
		private readonly WeakReference _instance;

		public WeakInstanceFactory(Type registerType, Type registerImplementation, object instance) {
			if (!IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "WeakInstanceFactory");

			this.registerType = registerType;
			this.registerImplementation = registerImplementation;
			_instance = new WeakReference(instance);
		}

		public override Type CreatesType => registerImplementation;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			var instance = _instance.Target;

			if (instance == null)
				throw new TinyIoCWeakReferenceException(registerType);

			return instance;
		}

		public override ObjectFactoryBase MultiInstanceVariant =>
			new MultiInstanceFactory(registerType, registerImplementation);

		public override ObjectFactoryBase WeakReferenceVariant => this;

		public override ObjectFactoryBase StrongReferenceVariant {
			get {
				var instance = _instance.Target;

				if (instance == null)
					throw new TinyIoCWeakReferenceException(registerType);

				return new InstanceFactory(registerType, registerImplementation, instance);
			}
		}

		public override void SetConstructor(ConstructorInfo constructor) {
			throw new TinyIoCConstructorResolutionException(
				"Constructor selection is not possible for instance factory registrations");
		}

		public void Dispose() {
			if (_instance.Target is IDisposable disposable)
				disposable.Dispose();
		}
	}

	private class SingletonFactory : ObjectFactoryBase, IDisposable {
		private readonly Type registerType;
		private readonly Type registerImplementation;
		private readonly object SingletonLock = new();
		private object _Current;

		public SingletonFactory(Type registerType, Type registerImplementation) {
			if (registerImplementation.IsAbstract() || registerImplementation.IsInterface())
				throw new TinyIoCRegistrationTypeException(registerImplementation, "SingletonFactory");

			if (!IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "SingletonFactory");

			this.registerType = registerType;
			this.registerImplementation = registerImplementation;
		}

		public override Type CreatesType => registerImplementation;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			if (parameters.Count != 0)
				throw new ArgumentException("Cannot specify parameters for singleton types");

			lock (SingletonLock)
				if (_Current == null)
					_Current = container.ConstructType(requestedType, registerImplementation, Constructor,
						options);

			return _Current;
		}

		public override ObjectFactoryBase SingletonVariant => this;

		public override ObjectFactoryBase GetCustomObjectLifetimeVariant(
			ITinyIoCObjectLifetimeProvider lifetimeProvider, string errorString) =>
			new CustomObjectLifetimeFactory(registerType, registerImplementation, lifetimeProvider,
				errorString);

		public override ObjectFactoryBase MultiInstanceVariant =>
			new MultiInstanceFactory(registerType, registerImplementation);

		public override ObjectFactoryBase GetFactoryForChildContainer(
			Type type, TinyIoCContainer parent, TinyIoCContainer child) {
			GetObject(type, parent, NamedParameterOverloads.Default, ResolveOptions.Default);
			return this;
		}

		public void Dispose() {
			if (_Current == null)
				return;

			if (_Current is IDisposable disposable)
				disposable.Dispose();
		}
	}

	private class CustomObjectLifetimeFactory : ObjectFactoryBase, IDisposable {
		private readonly Lock SingletonLock = new();
		private readonly Type registerType;
		private readonly Type registerImplementation;
		private readonly ITinyIoCObjectLifetimeProvider _LifetimeProvider;

		public CustomObjectLifetimeFactory(
			Type registerType, Type registerImplementation, ITinyIoCObjectLifetimeProvider lifetimeProvider,
			string errorMessage) {
			if (lifetimeProvider == null)
				throw new ArgumentNullException(nameof(lifetimeProvider), "lifetimeProvider is null.");

			if (!IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "SingletonFactory");

			if (registerImplementation.IsAbstract() || registerImplementation.IsInterface())
				throw new TinyIoCRegistrationTypeException(registerImplementation, errorMessage);

			this.registerType = registerType;
			this.registerImplementation = registerImplementation;
			_LifetimeProvider = lifetimeProvider;
		}

		public override Type CreatesType => registerImplementation;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			object current;

			lock (SingletonLock) {
				current = _LifetimeProvider.GetObject();
				if (current == null) {
					current = container.ConstructType(requestedType, registerImplementation, Constructor,
						options);
					_LifetimeProvider.SetObject(current);
				}
			}

			return current;
		}

		public override ObjectFactoryBase SingletonVariant {
			get {
				_LifetimeProvider.ReleaseObject();
				return new SingletonFactory(registerType, registerImplementation);
			}
		}

		public override ObjectFactoryBase MultiInstanceVariant {
			get {
				_LifetimeProvider.ReleaseObject();
				return new MultiInstanceFactory(registerType, registerImplementation);
			}
		}

		public override ObjectFactoryBase GetCustomObjectLifetimeVariant(
			ITinyIoCObjectLifetimeProvider lifetimeProvider, string errorString) {
			_LifetimeProvider.ReleaseObject();
			return new CustomObjectLifetimeFactory(registerType, registerImplementation, lifetimeProvider,
				errorString);
		}

		public override ObjectFactoryBase GetFactoryForChildContainer(
			Type type, TinyIoCContainer parent, TinyIoCContainer child) {
			GetObject(type, parent, NamedParameterOverloads.Default, ResolveOptions.Default);
			return this;
		}

		public void Dispose() {
			_LifetimeProvider.ReleaseObject();
		}
	}

	#endregion

	#region Singleton Container

	static TinyIoCContainer() {
	}

	public static TinyIoCContainer Current { get; } = new();

	#endregion

	#region Type Registrations

	public sealed class TypeRegistration {
		private readonly int _hashCode;

		public Type Type { get; private set; }
		public string Name { get; private set; }

		public TypeRegistration(Type type)
			: this(type, string.Empty) {
		}

		public TypeRegistration(Type type, string name) {
			Type = type;
			Name = name;

			_hashCode = string.Concat(Type.FullName, "|", Name).GetHashCode();
		}

		public override bool Equals(object obj) {
			if (obj is not TypeRegistration typeRegistration)
				return false;

			if (Type != typeRegistration.Type)
				return false;

			if (!string.Equals(Name, typeRegistration.Name, StringComparison.Ordinal))
				return false;

			return true;
		}

		public override int GetHashCode() => _hashCode;
	}

	private readonly SafeDictionary<TypeRegistration, ObjectFactoryBase> _RegisteredTypes;

	private delegate object ObjectConstructor(params object[] parameters);

	private static readonly SafeDictionary<ConstructorInfo, ObjectConstructor> _ObjectConstructorCache = new();

	#endregion

	#region Constructors

	public TinyIoCContainer() {
		_RegisteredTypes = new SafeDictionary<TypeRegistration, ObjectFactoryBase>();

		RegisterDefaultTypes();
	}

	private readonly TinyIoCContainer _Parent;

	private TinyIoCContainer(TinyIoCContainer parent)
		: this() {
		_Parent = parent;
	}

	#endregion

	#region Internal Methods

	private readonly Lock _AutoRegisterLock = new();

	private void AutoRegisterInternal(
		IEnumerable<Assembly> assemblies, DuplicateImplementationActions duplicateAction,
		Func<Type, bool> registrationPredicate) {
		lock (_AutoRegisterLock) {
			var types = assemblies.SelectMany(a => a.SafeGetTypes())
				.Where(t => !IsIgnoredType(t, registrationPredicate)).ToList();

			var concreteTypes = types
				.Where(type => type.IsClass() && !type.IsAbstract() &&
				               type != GetType() && type.DeclaringType != GetType() &&
				               !type.IsGenericTypeDefinition())
				.ToList();

			foreach (var type in concreteTypes) {
				try {
					RegisterInternal(type, string.Empty, GetDefaultObjectFactory(type, type));
				} catch (MethodAccessException) {
				}
			}

			var abstractInterfaceTypes = from type in types
				where (type.IsInterface() || type.IsAbstract()) &&
				      type.DeclaringType != GetType() &&
				      !type.IsGenericTypeDefinition()
				select type;

			foreach (var type in abstractInterfaceTypes) {
				var localType = type;
				var implementations = from implementationType in concreteTypes
					where localType.IsAssignableFrom(implementationType)
					select implementationType;

				if (implementations.Skip(1).Any()) {
					if (duplicateAction == DuplicateImplementationActions.Fail)
						throw new TinyIoCAutoRegistrationException(type, implementations);

					if (duplicateAction == DuplicateImplementationActions.RegisterMultiple) {
						RegisterMultiple(type, implementations);
					}
				}

				var firstImplementation = implementations.FirstOrDefault();
				if (firstImplementation != null) {
					try {
						RegisterInternal(type, string.Empty, GetDefaultObjectFactory(type, firstImplementation));
					} catch (MethodAccessException) {
					}
				}
			}
		}
	}

	private static bool IsIgnoredAssembly(Assembly assembly) {
		var ignoreChecks = new List<Func<Assembly, bool>> {
			asm => asm.FullName.StartsWith("Microsoft.", StringComparison.Ordinal),
			asm => asm.FullName.StartsWith("System.", StringComparison.Ordinal),
			asm => asm.FullName.StartsWith("System,", StringComparison.Ordinal),
			asm => asm.FullName.StartsWith("CR_ExtUnitTest", StringComparison.Ordinal),
			asm => asm.FullName.StartsWith("mscorlib,", StringComparison.Ordinal),
			asm => asm.FullName.StartsWith("CR_VSTest", StringComparison.Ordinal),
			asm => asm.FullName.StartsWith("DevExpress.CodeRush", StringComparison.Ordinal)
		};

		foreach (var check in ignoreChecks) {
			if (check(assembly))
				return true;
		}

		return false;
	}

	private static bool IsIgnoredType(Type type, Func<Type, bool> registrationPredicate) {
		var ignoreChecks = new List<Func<Type, bool>> {
			t => t.FullName.StartsWith("System.", StringComparison.Ordinal),
			t => t.FullName.StartsWith("Microsoft.", StringComparison.Ordinal),
			t => t.IsPrimitive(),
#if !UNBOUND_GENERICS_GETCONSTRUCTORS
                t => t.IsGenericTypeDefinition(),
#endif
			t => t.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length == 0 &&
			     !(t.IsInterface() || t.IsAbstract())
		};

		if (registrationPredicate != null) {
			ignoreChecks.Add(t => !registrationPredicate(t));
		}

		foreach (var check in ignoreChecks) {
			if (check(type))
				return true;
		}

		return false;
	}

	private void RegisterDefaultTypes() {
		Register(this);

#if TINYMESSENGER
                        if (_Parent == null)
                Register<TinyMessenger.ITinyMessengerHub, TinyMessenger.TinyMessengerHub>();
#endif
	}

	private ObjectFactoryBase GetCurrentFactory(TypeRegistration registration) {
		_RegisteredTypes.TryGetValue(registration, out var current);

		return current;
	}

	private RegisterOptions RegisterInternal(Type registerType, string name, ObjectFactoryBase factory) {
		var typeRegistration = new TypeRegistration(registerType, name);

		return AddUpdateRegistration(typeRegistration, factory);
	}

	private RegisterOptions AddUpdateRegistration(TypeRegistration typeRegistration, ObjectFactoryBase factory) {
		_RegisteredTypes[typeRegistration] = factory;

		return new RegisterOptions(this, typeRegistration);
	}

	private void RemoveRegistration(TypeRegistration typeRegistration) {
		_RegisteredTypes.Remove(typeRegistration);
	}

	private static ObjectFactoryBase GetDefaultObjectFactory(Type registerType, Type registerImplementation) {
		if (registerType.IsInterface() || registerType.IsAbstract())
			return new SingletonFactory(registerType, registerImplementation);

		return new MultiInstanceFactory(registerType, registerImplementation);
	}

	private bool CanResolveInternal(
		TypeRegistration registration, NamedParameterOverloads parameters, ResolveOptions options) {
		ArgumentNullException.ThrowIfNull(parameters);

		var checkType = registration.Type;
		var name = registration.Name;

		if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType, name), out ObjectFactoryBase factory)) {
			if (factory.AssumeConstruction)
				return true;

			if (factory.Constructor == null)
				return GetBestConstructor(factory.CreatesType, parameters, options) != null ? true : false;
			return CanConstruct(factory.Constructor, parameters, options);
		}

#if RESOLVE_OPEN_GENERICS
		if (checkType is { IsInterface: true, IsGenericType: true }) {
			if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType.GetGenericTypeDefinition(), name),
				    out factory)) {
				if (factory.AssumeConstruction)
					return true;

				if (factory.Constructor == null)
					return GetBestConstructor(factory.CreatesType, parameters, options) != null ? true : false;
				return CanConstruct(factory.Constructor, parameters, options);
			}
		}
#endif

		if (!string.IsNullOrEmpty(name) &&
		    options.NamedResolutionFailureAction == NamedResolutionFailureActions.Fail)
			return _Parent != null ? _Parent.CanResolveInternal(registration, parameters, options) : false;

		if (!string.IsNullOrEmpty(name) && options.NamedResolutionFailureAction ==
		    NamedResolutionFailureActions.AttemptUnnamedResolution) {
			if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType), out factory)) {
				if (factory.AssumeConstruction)
					return true;

				return GetBestConstructor(factory.CreatesType, parameters, options) != null ? true : false;
			}
		}

		if (IsAutomaticLazyFactoryRequest(checkType))
			return true;

		if (IsIEnumerableRequest(registration.Type))
			return true;

		if (options.UnregisteredResolutionAction == UnregisteredResolutionActions.AttemptResolve ||
		    checkType.IsGenericType() &&
		    options.UnregisteredResolutionAction == UnregisteredResolutionActions.GenericsOnly)
			return GetBestConstructor(checkType, parameters, options) != null ? true :
				_Parent != null ? _Parent.CanResolveInternal(registration, parameters, options) : false;

		if (_Parent != null)
			return _Parent.CanResolveInternal(registration, parameters, options);

		return false;
	}

	private static bool IsIEnumerableRequest(Type type) {
		if (!type.IsGenericType())
			return false;

		var genericType = type.GetGenericTypeDefinition();

		if (genericType == typeof(IEnumerable<>))
			return true;

		return false;
	}

	private static bool IsAutomaticLazyFactoryRequest(Type type) {
		if (!type.IsGenericType())
			return false;

		var genericType = type.GetGenericTypeDefinition();

		if (genericType == typeof(Func<>))
			return true;

		if (genericType == typeof(Func<,>) && type.GetGenericArguments()[0] == typeof(string))
			return true;

		if (genericType == typeof(Func<,,>) && type.GetGenericArguments()[0] == typeof(string) &&
		    type.GetGenericArguments()[1] == typeof(IDictionary<string, object>))
			return true;

		return false;
	}

	private ObjectFactoryBase GetParentObjectFactory(TypeRegistration registration) {
		if (_Parent == null)
			return null;

		if (_Parent._RegisteredTypes.TryGetValue(registration, out var factory)) {
			return factory.GetFactoryForChildContainer(registration.Type, _Parent, this);
		}

		return _Parent.GetParentObjectFactory(registration);
	}

	private object ResolveInternal(
		TypeRegistration registration, NamedParameterOverloads parameters, ResolveOptions options) {
		if (_RegisteredTypes.TryGetValue(registration, out ObjectFactoryBase factory)) {
			try {
				return factory.GetObject(registration.Type, this, parameters, options);
			} catch (TinyIoCResolutionException) {
				throw;
			} catch (Exception ex) {
				throw new TinyIoCResolutionException(registration.Type, ex);
			}
		}

#if RESOLVE_OPEN_GENERICS
		if (registration.Type.IsGenericType()) {
			var openTypeRegistration = new TypeRegistration(registration.Type.GetGenericTypeDefinition(),
				registration.Name);

			if (_RegisteredTypes.TryGetValue(openTypeRegistration, out factory)) {
				try {
					return factory.GetObject(registration.Type, this, parameters, options);
				} catch (TinyIoCResolutionException) {
					throw;
				} catch (Exception ex) {
					throw new TinyIoCResolutionException(registration.Type, ex);
				}
			}
		}
#endif

		var bubbledObjectFactory = GetParentObjectFactory(registration);
		if (bubbledObjectFactory != null) {
			try {
				return bubbledObjectFactory.GetObject(registration.Type, this, parameters, options);
			} catch (TinyIoCResolutionException) {
				throw;
			} catch (Exception ex) {
				throw new TinyIoCResolutionException(registration.Type, ex);
			}
		}

		if (!string.IsNullOrEmpty(registration.Name) &&
		    options.NamedResolutionFailureAction == NamedResolutionFailureActions.Fail)
			throw new TinyIoCResolutionException(registration.Type);

		if (!string.IsNullOrEmpty(registration.Name) && options.NamedResolutionFailureAction ==
		    NamedResolutionFailureActions.AttemptUnnamedResolution) {
			if (_RegisteredTypes.TryGetValue(new TypeRegistration(registration.Type, string.Empty), out factory)) {
				try {
					return factory.GetObject(registration.Type, this, parameters, options);
				} catch (TinyIoCResolutionException) {
					throw;
				} catch (Exception ex) {
					throw new TinyIoCResolutionException(registration.Type, ex);
				}
			}
		}

#if EXPRESSIONS
		if (IsAutomaticLazyFactoryRequest(registration.Type))
			return GetLazyAutomaticFactoryRequest(registration.Type);
#endif
		if (IsIEnumerableRequest(registration.Type))
			return GetIEnumerableRequest(registration.Type);

		if (options.UnregisteredResolutionAction == UnregisteredResolutionActions.AttemptResolve ||
		    registration.Type.IsGenericType() &&
		    options.UnregisteredResolutionAction == UnregisteredResolutionActions.GenericsOnly) {
			if (!registration.Type.IsAbstract() && !registration.Type.IsInterface())
				return ConstructType(null, registration.Type, parameters, options);
		}

		throw new TinyIoCResolutionException(registration.Type);
	}

#if EXPRESSIONS
	private object GetLazyAutomaticFactoryRequest(Type type) {
		if (!type.IsGenericType())
			return null;

		var genericType = type.GetGenericTypeDefinition();
		var genericArguments = type.GetGenericArguments();

		if (genericType == typeof(Func<>)) {
			var returnType = genericArguments[0];

			var resolveMethod = typeof(TinyIoCContainer).GetMethod("Resolve", []);
			resolveMethod = resolveMethod.MakeGenericMethod(returnType);

			var resolveCall = Expression.Call(Expression.Constant(this), resolveMethod);

			var resolveLambda = Expression.Lambda(resolveCall).Compile();

			return resolveLambda;
		}

		if (genericType == typeof(Func<,>) && genericArguments[0] == typeof(string)) {
			var returnType = genericArguments[1];

			var resolveMethod = typeof(TinyIoCContainer).GetMethod("Resolve", [typeof(string)]);
			resolveMethod = resolveMethod.MakeGenericMethod(returnType);

			var resolveParameters = new[] {
				Expression.Parameter(typeof(string), "name")
			};
			var resolveCall = Expression.Call(Expression.Constant(this), resolveMethod, resolveParameters);

			var resolveLambda = Expression.Lambda(resolveCall, resolveParameters).Compile();

			return resolveLambda;
		}

		if (genericType == typeof(Func<,,>) && type.GetGenericArguments()[0] == typeof(string) &&
		    type.GetGenericArguments()[1] == typeof(IDictionary<string, object>)) {
			var returnType = genericArguments[2];

			var name = Expression.Parameter(typeof(string), "name");
			var parameters = Expression.Parameter(typeof(IDictionary<string, object>), "parameters");

			var resolveMethod =
				typeof(TinyIoCContainer).GetMethod(
					"Resolve", [typeof(string), typeof(NamedParameterOverloads)]);
			resolveMethod = resolveMethod.MakeGenericMethod(returnType);

			var resolveCall = Expression.Call(Expression.Constant(this), resolveMethod, name,
				Expression.Call(typeof(NamedParameterOverloads), "FromIDictionary",
					null, parameters));

			var resolveLambda = Expression.Lambda(resolveCall, name, parameters).Compile();

			return resolveLambda;
		}

		throw new TinyIoCResolutionException(type);
	}
#endif
	private object GetIEnumerableRequest(Type type) {
		var genericResolveAllMethod = GetType().GetGenericMethod(
			BindingFlags.Public | BindingFlags.Instance, "ResolveAll", type.GetGenericArguments(),
			[typeof(bool)]);

		return genericResolveAllMethod.Invoke(this, [false]);
	}

	private bool CanConstruct(ConstructorInfo ctor, NamedParameterOverloads parameters, ResolveOptions options) {
		ArgumentNullException.ThrowIfNull(parameters);

		foreach (var parameter in ctor.GetParameters()) {
			if (string.IsNullOrEmpty(parameter.Name))
				return false;

			var isParameterOverload = parameters.ContainsKey(parameter.Name);

			if (parameter.ParameterType.IsPrimitive() && !isParameterOverload)
				return false;

			if (!isParameterOverload && !CanResolveInternal(new TypeRegistration(parameter.ParameterType),
				    NamedParameterOverloads.Default, options))
				return false;
		}

		return true;
	}

	private ConstructorInfo GetBestConstructor(
		Type type, NamedParameterOverloads parameters, ResolveOptions options) {
		ArgumentNullException.ThrowIfNull(parameters);

		if (type.IsValueType())
			return null;

		var ctors = GetTypeConstructors(type);

		foreach (var ctor in ctors) {
			if (CanConstruct(ctor, parameters, options))
				return ctor;
		}

		return null;
	}

	private static IEnumerable<ConstructorInfo> GetTypeConstructors(Type type) {
		return type.GetConstructors().OrderByDescending(ctor => ctor.GetParameters().Length);
	}

	private object ConstructType(Type requestedType, Type implementationType, ResolveOptions options) => ConstructType(requestedType, implementationType, null, NamedParameterOverloads.Default, options);

	private object ConstructType(
		Type requestedType, Type implementationType, ConstructorInfo constructor, ResolveOptions options) =>
		ConstructType(requestedType, implementationType, constructor, NamedParameterOverloads.Default,
			options);

	private object ConstructType(
		Type requestedType, Type implementationType, NamedParameterOverloads parameters, ResolveOptions options) =>
		ConstructType(requestedType, implementationType, null, parameters, options);

	private object ConstructType(
		Type requestedType, Type implementationType, ConstructorInfo constructor,
		NamedParameterOverloads parameters, ResolveOptions options) {
		var typeToConstruct = implementationType;

#if RESOLVE_OPEN_GENERICS
		if (implementationType.IsGenericTypeDefinition()) {
			if (requestedType == null || !requestedType.IsGenericType() ||
			    requestedType.GetGenericArguments().Length == 0)
				throw new TinyIoCResolutionException(typeToConstruct);

			typeToConstruct = typeToConstruct.MakeGenericType(requestedType.GetGenericArguments());
		}
#endif
		if (constructor == null) {
			constructor = GetBestConstructor(typeToConstruct, parameters, options) ??
			              GetTypeConstructors(typeToConstruct).LastOrDefault();
		}

		if (constructor == null)
			throw new TinyIoCResolutionException(typeToConstruct);

		var ctorParams = constructor.GetParameters();
		var args = new object[ctorParams.Length];

		for (var parameterIndex = 0; parameterIndex < ctorParams.Length; parameterIndex++) {
			var currentParam = ctorParams[parameterIndex];

			try {
				args[parameterIndex] = parameters.TryGetValue(currentParam.Name, out var parameter)
					? parameter
					: ResolveInternal(
						new TypeRegistration(currentParam.ParameterType),
						NamedParameterOverloads.Default,
						options);
			} catch (TinyIoCResolutionException ex) {
				throw new TinyIoCResolutionException(typeToConstruct, ex);
			} catch (Exception ex) {
				throw new TinyIoCResolutionException(typeToConstruct, ex);
			}
		}

		try {
			var constructionDelegate = CreateObjectConstructionDelegateWithCache(constructor);
			return constructionDelegate.Invoke(args);
		} catch (Exception ex) {
			throw new TinyIoCResolutionException(typeToConstruct, ex);
		}
	}

	private static ObjectConstructor CreateObjectConstructionDelegateWithCache(ConstructorInfo constructor) {
		if (_ObjectConstructorCache.TryGetValue(constructor, out var objectConstructor))
			return objectConstructor;

		var constructorParams = constructor.GetParameters();
		var lambdaParams = Expression.Parameter(typeof(object[]), "parameters");
		var newParams = new Expression[constructorParams.Length];

		for (var i = 0; i < constructorParams.Length; i++) {
			var paramsParameter = Expression.ArrayIndex(lambdaParams, Expression.Constant(i));

			newParams[i] = Expression.Convert(paramsParameter, constructorParams[i].ParameterType);
		}

		var newExpression = Expression.New(constructor, newParams);

		var constructionLambda = Expression.Lambda(typeof(ObjectConstructor), newExpression, lambdaParams);

		objectConstructor = (ObjectConstructor)constructionLambda.Compile();

		_ObjectConstructorCache[constructor] = objectConstructor;
		return objectConstructor;
	}

	private void BuildUpInternal(object input, ResolveOptions resolveOptions) {
		var properties = from property in input.GetType().GetProperties()
			where property.GetGetMethod() != null && property.GetSetMethod() != null &&
			      !property.PropertyType.IsValueType()
			select property;

		foreach (var property in properties) {
			if (property.GetValue(input, null) == null) {
				try {
					property.SetValue(
						input,
						ResolveInternal(new TypeRegistration(property.PropertyType),
							NamedParameterOverloads.Default, resolveOptions), null);
				} catch (TinyIoCResolutionException) {
				}
			}
		}
	}

	private IEnumerable<TypeRegistration> GetParentRegistrationsForType(Type resolveType) {
		if (_Parent == null)
			return [];

		var registrations = _Parent._RegisteredTypes.Keys.Where(tr => tr.Type == resolveType);

		return registrations.Concat(_Parent.GetParentRegistrationsForType(resolveType));
	}

	private IEnumerable<object> ResolveAllInternal(Type resolveType, bool includeUnnamed) {
		var registrations = _RegisteredTypes.Keys.Where(tr => tr.Type == resolveType)
			.Concat(GetParentRegistrationsForType(resolveType));

		if (!includeUnnamed)
			registrations = registrations.Where(tr => tr.Name != string.Empty);

		return registrations.Select(registration =>
			ResolveInternal(registration, NamedParameterOverloads.Default,
				ResolveOptions.Default));
	}

	private static bool IsValidAssignment(Type registerType, Type registerImplementation) {
		if (!registerType.IsGenericTypeDefinition()) {
			if (!registerType.IsAssignableFrom(registerImplementation))
				return false;
		} else {
			if (registerType.IsInterface()) {
				if (registerImplementation.FindInterfaces((t, _) => t.Name == registerType.Name, null).Length == 0)
					return false;
			} else if (registerType.IsAbstract() && registerImplementation.BaseType() != registerType) {
				return false;
			}
		}

		return true;
	}

	#endregion

	#region IDisposable Members

	private bool disposed;

	public void Dispose() {
		if (!disposed) {
			disposed = true;

			_RegisteredTypes.Dispose();

			GC.SuppressFinalize(this);
		}
	}

	#endregion
}

public static class ReverseTypeExtender {
	public static bool IsClass(this Type type) => type.IsClass;

	public static bool IsAbstract(this Type type) => type.IsAbstract;

	public static bool IsInterface(this Type type) => type.IsInterface;

	public static bool IsPrimitive(this Type type) => type.IsPrimitive;

	public static bool IsValueType(this Type type) => type.IsValueType;

	public static bool IsGenericType(this Type type) => type.IsGenericType;

	public static bool IsGenericParameter(this Type type) => type.IsGenericParameter;

	public static bool IsGenericTypeDefinition(this Type type) => type.IsGenericTypeDefinition;

	public static Type BaseType(this Type type) => type.BaseType;

	public static Assembly Assembly(this Type type) => type.Assembly;
}