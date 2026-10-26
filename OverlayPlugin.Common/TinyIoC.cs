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


#define EXPRESSIONS // Platform supports System.Linq.Expressions
#define COMPILED_EXPRESSIONS // Platform supports compiling expressions
#define APPDOMAIN_GETASSEMBLIES // Platform supports getting all assemblies from the AppDomain object
#define UNBOUND_GENERICS_GETCONSTRUCTORS // Platform supports GetConstructors on unbound generic types
#define GETPARAMETERS_OPEN_GENERICS // Platform supports GetParameters on open generics
#define RESOLVE_OPEN_GENERICS // Platform supports resolving open generics
#define READER_WRITER_LOCK_SLIM // Platform supports ReaderWriterLockSlim

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

#pragma warning disable CS8600
#pragma warning disable CS8604
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8714

#pragma warning disable CS8619


namespace RainbowMage.OverlayPlugin;

public class SafeDictionary<TKey, TValue> : IDisposable {
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
}

public static class TypeExtensions {
	private static readonly SafeDictionary<GenericMethodCacheKey, MethodInfo?> _genericMethodCache;

	static TypeExtensions() {
		_genericMethodCache = new SafeDictionary<GenericMethodCacheKey, MethodInfo?>();
	}


	public static MethodInfo? GetGenericMethod(
		this Type sourceType, BindingFlags bindingFlags, string methodName, Type[] genericTypes,
		Type[] parameterTypes) {
		var cacheKey = new GenericMethodCacheKey(sourceType, methodName, genericTypes, parameterTypes);

		if (!_genericMethodCache.TryGetValue(cacheKey, out var method)) {
			method = GetMethod(sourceType, bindingFlags, methodName, genericTypes, parameterTypes);
			_genericMethodCache[cacheKey] = method;
		}

		return method;
	}

	private static MethodInfo? GetMethod(
		Type sourceType, BindingFlags bindingFlags, string methodName, Type[] genericTypes, Type[] parameterTypes) {
		var methods =
			sourceType.GetMethods(bindingFlags).Where(mi => string.Equals(methodName, mi.Name, StringComparison.Ordinal)).Where(mi => mi.ContainsGenericParameters)
				.Where(mi => mi.GetGenericArguments().Length == genericTypes.Length)
				.Where(mi => mi.GetParameters().Length == parameterTypes.Length).Select(mi => mi.MakeGenericMethod(genericTypes)).Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(parameterTypes))
				.ToList();

		return methods.Count > 1 ? throw new AmbiguousMatchException() : methods.FirstOrDefault();
	}


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

		public override bool Equals(object? obj) {
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

			if (_genericTypes.Where((t, i) => t != cacheKey._genericTypes[i]).Any()) { return false; }

			return !_parameterTypes.Where((t, i) => t != cacheKey._parameterTypes[i]).Any();
		}

		public override int GetHashCode() => _hashCode;

		private int GenerateHashCode() {
			unchecked {
				var result = _sourceType.GetHashCode();

				result = result * 397 ^ _methodName.GetHashCode();

				result = _genericTypes.Aggregate(result, (current, t) => current * 397 ^ t.GetHashCode());

				return _parameterTypes.Aggregate(result, (current, t) => current * 397 ^ t.GetHashCode());
			}
		}
	}
}

public class TinyIoCResolutionException : Exception {
	private const string ERROR_TEXT = "Unable to resolve type: {0}";

	public TinyIoCResolutionException(Type type)
		: base(string.Format(ERROR_TEXT, type.FullName)) {
	}

	public TinyIoCResolutionException(Type type, Exception innerException)
		: base(string.Format(ERROR_TEXT, type.FullName), innerException) {
	}
}

public class TinyIoCRegistrationTypeException(Type type, string factory) : Exception(string.Format(REGISTER_ERROR_TEXT, type.FullName, factory)) {
	private const string REGISTER_ERROR_TEXT =
		"Cannot register type {0} - abstract classes or interfaces are not valid implementation types for {1}.";
}

public sealed class NamedParameterOverloads : Dictionary<string, object> {
	public static NamedParameterOverloads FromIDictionary(IDictionary<string, object> data) => data as NamedParameterOverloads ?? new NamedParameterOverloads(data);

	public NamedParameterOverloads() {
	}

	private NamedParameterOverloads(IDictionary<string, object> data) : base(data) {
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

public sealed class ResolveOptions {
	public UnregisteredResolutionActions UnregisteredResolutionAction { get; private init; } =
		UnregisteredResolutionActions.AttemptResolve;

	public NamedResolutionFailureActions NamedResolutionFailureAction { get;private init; } =
		NamedResolutionFailureActions.Fail;

	public static ResolveOptions Default { get; } = new() {
		NamedResolutionFailureAction = NamedResolutionFailureActions.Fail,
		UnregisteredResolutionAction = UnregisteredResolutionActions.Fail
	};
}

public sealed class TinyIoCContainer : IDisposable {
	public sealed class RegisterOptions;


	public TinyIoCContainer GetChildContainer() => new(this);


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

		return Register(typeof(RegisterType), factory);
	}

	public RegisterOptions Register<RegisterType>(
		Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory, string name)
		where RegisterType : class {
		ArgumentNullException.ThrowIfNull(factory);

		return Register(typeof(RegisterType), factory, name);
	}


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

	public bool TryResolve(Type resolveType, out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, ResolveOptions options, out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, string name, out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, string name, ResolveOptions options, out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(Type resolveType, NamedParameterOverloads parameters, out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType, parameters);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(
		Type resolveType, string name, NamedParameterOverloads parameters, out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name, parameters);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve(
		Type resolveType, NamedParameterOverloads parameters, ResolveOptions options, out object? resolvedType) {
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
		out object? resolvedType) {
		try {
			resolvedType = Resolve(resolveType, name, parameters, options);
			return true;
		} catch (TinyIoCResolutionException) {
			resolvedType = null;
			return false;
		}
	}

	public bool TryResolve<ResolveType>(out ResolveType resolvedType) where ResolveType : class {
			resolvedType = Resolve<ResolveType>();
			return true;
	}

	public bool TryResolve<ResolveType>(ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(options);
			return true;
	}

	public bool TryResolve<ResolveType>(string name, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(name);
			return true;
		
	}

	public bool TryResolve<ResolveType>(string name, ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(name, options);
			return true;
		
	}

	public bool TryResolve<ResolveType>(NamedParameterOverloads parameters, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(parameters);
			return true;
		
	}

	public bool TryResolve<ResolveType>(
		string name, NamedParameterOverloads parameters, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(name, parameters);
			return true;
		
	}

	public bool TryResolve<ResolveType>(
		NamedParameterOverloads parameters, ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(parameters, options);
			return true;
		
	}

	public bool TryResolve<ResolveType>(
		string name, NamedParameterOverloads parameters, ResolveOptions options, out ResolveType resolvedType)
		where ResolveType : class {
		
			resolvedType = Resolve<ResolveType>(name, parameters, options);
			return true;
		
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


	private abstract class ObjectFactoryBase {
		public virtual bool AssumeConstruction => false;

		public abstract Type CreatesType { get; }

		public ConstructorInfo? Constructor { get; protected set; }

		public abstract object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options);

		public virtual ObjectFactoryBase GetFactoryForChildContainer(
			Type type, TinyIoCContainer parent) =>
			this;
	}

	private class MultiInstanceFactory : ObjectFactoryBase {
		private readonly Type registerType;
		private readonly Type registerImplementation;
		public override Type CreatesType => registerImplementation;

		public MultiInstanceFactory(Type registerType, Type registerImplementation) {
			if (registerImplementation.IsAbstract() || registerImplementation.IsInterface() || !IsValidAssignment(registerType, registerImplementation))
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
	}

	private sealed class DelegateFactory : ObjectFactoryBase {
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
	}

	private class InstanceFactory : ObjectFactoryBase, IDisposable {
		private readonly object _instance;

		public override bool AssumeConstruction => true;

		public InstanceFactory(Type registerType, Type registerImplementation, object instance) {
			if (!IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "InstanceFactory");

			CreatesType = registerImplementation;
			_instance = instance;
		}

		public override Type CreatesType { get; }

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) =>
			_instance;

		public void Dispose() {
			if (_instance is IDisposable disposable)
				disposable.Dispose();
		}
	}

	private class SingletonFactory : ObjectFactoryBase, IDisposable {
		private readonly Type registerImplementation;
		private readonly Lock SingletonLock = new();
		private object? _Current;

		public SingletonFactory(Type registerType, Type registerImplementation) {
			if (registerImplementation.IsAbstract() || registerImplementation.IsInterface() || !IsValidAssignment(registerType, registerImplementation))
				throw new TinyIoCRegistrationTypeException(registerImplementation, "SingletonFactory");

			this.registerImplementation = registerImplementation;
		}

		public override Type CreatesType => registerImplementation;

		public override object GetObject(
			Type requestedType, TinyIoCContainer container, NamedParameterOverloads parameters,
			ResolveOptions options) {
			if (parameters.Count != 0)
				throw new ArgumentException("Cannot specify parameters for singleton types");

			lock (SingletonLock)
				_Current ??= container.ConstructType(requestedType, registerImplementation, Constructor,
					options);

			return _Current;
		}

		public override ObjectFactoryBase GetFactoryForChildContainer(
			Type type, TinyIoCContainer parent) {
			GetObject(type, parent, NamedParameterOverloads.Default, ResolveOptions.Default);
			return this;
		}

		public void Dispose() {
			if (_Current is IDisposable disposable)
				disposable.Dispose();
		}
	}

	public static TinyIoCContainer Current { get; } = new();


	private sealed class TypeRegistration {
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

		public override bool Equals(object? obj) {
			if (obj is not TypeRegistration typeRegistration)
				return false;

			return Type == typeRegistration.Type && string.Equals(Name, typeRegistration.Name, StringComparison.Ordinal);
		}

		public override int GetHashCode() => _hashCode;
	}

	private readonly SafeDictionary<TypeRegistration, ObjectFactoryBase> _RegisteredTypes;

	private delegate object ObjectConstructor(params object[] parameters);

	private static readonly SafeDictionary<ConstructorInfo, ObjectConstructor> _ObjectConstructorCache = new();


	public TinyIoCContainer() {
		_RegisteredTypes = new SafeDictionary<TypeRegistration, ObjectFactoryBase>();

		RegisterDefaultTypes();
	}

	private readonly TinyIoCContainer? _Parent;

	private TinyIoCContainer(TinyIoCContainer parent)
		: this() {
		_Parent = parent;
	}


	private void RegisterDefaultTypes() {
		Register(this);
	}

	private RegisterOptions RegisterInternal(Type registerType, string name, ObjectFactoryBase factory) {
		var typeRegistration = new TypeRegistration(registerType, name);

		return AddUpdateRegistration(typeRegistration, factory);
	}

	private RegisterOptions AddUpdateRegistration(TypeRegistration typeRegistration, ObjectFactoryBase factory) {
		_RegisteredTypes[typeRegistration] = factory;

		return new RegisterOptions();
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
				return GetBestConstructor(factory.CreatesType, parameters, options) != null;
			return CanConstruct(factory.Constructor, parameters, options);
		}

		if (checkType is { IsInterface: true, IsGenericType: true }) {
			if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType.GetGenericTypeDefinition(), name),
				    out factory)) {
				if (factory.AssumeConstruction)
					return true;

				if (factory.Constructor == null)
					return GetBestConstructor(factory.CreatesType, parameters, options) != null;
				return CanConstruct(factory.Constructor, parameters, options);
			}
		}

		if (!string.IsNullOrEmpty(name) &&
		    options.NamedResolutionFailureAction == NamedResolutionFailureActions.Fail)
			return _Parent?.CanResolveInternal(registration, parameters, options) ?? false;

		if (!string.IsNullOrEmpty(name) && options.NamedResolutionFailureAction ==
		    NamedResolutionFailureActions.AttemptUnnamedResolution) {
			if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType), out factory)) {
				if (factory.AssumeConstruction)
					return true;

				return GetBestConstructor(factory.CreatesType, parameters, options) != null;
			}
		}

		if (IsAutomaticLazyFactoryRequest(checkType))
			return true;

		if (IsIEnumerableRequest(registration.Type))
			return true;

		if (options.UnregisteredResolutionAction == UnregisteredResolutionActions.AttemptResolve ||
		    checkType.IsGenericType() &&
		    options.UnregisteredResolutionAction == UnregisteredResolutionActions.GenericsOnly)
			return GetBestConstructor(checkType, parameters, options) != null || (_Parent?.CanResolveInternal(registration, parameters, options) ?? false);

		return _Parent != null && _Parent.CanResolveInternal(registration, parameters, options);
	}

	private static bool IsIEnumerableRequest(Type type) {
		if (!type.IsGenericType())
			return false;

		var genericType = type.GetGenericTypeDefinition();

		return genericType == typeof(IEnumerable<>);
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

	private ObjectFactoryBase? GetParentObjectFactory(TypeRegistration registration) {
		if (_Parent == null)
			return null;

		if (_Parent._RegisteredTypes.TryGetValue(registration, out var factory)) {
			return factory.GetFactoryForChildContainer(registration.Type, _Parent);
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


		if (IsAutomaticLazyFactoryRequest(registration.Type))
			return GetLazyAutomaticFactoryRequest(registration.Type);

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

	private object GetLazyAutomaticFactoryRequest(Type type) {
		if (!type.IsGenericType())
			throw new ArgumentException("Automatic factory requests must be generic types.", nameof(type));

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

	private object GetIEnumerableRequest(Type type) {
		var genericResolveAllMethod = GetType().GetGenericMethod(
			BindingFlags.Public | BindingFlags.Instance, "ResolveAll", type.GetGenericArguments(),
			[typeof(bool)]);

		ArgumentNullException.ThrowIfNull(genericResolveAllMethod);
		return genericResolveAllMethod.Invoke(this, [false])!;
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

	private ConstructorInfo? GetBestConstructor(
		Type type, NamedParameterOverloads parameters, ResolveOptions options) {
		ArgumentNullException.ThrowIfNull(parameters);

		if (type.IsValueType())
			return null;

		var ctors = GetTypeConstructors(type);

		return ctors.FirstOrDefault(ctor => CanConstruct(ctor, parameters, options));
	}

	private static IEnumerable<ConstructorInfo> GetTypeConstructors(Type type) {
		return type.GetConstructors().OrderByDescending(ctor => ctor.GetParameters().Length);
	}

	private object ConstructType(
		Type? requestedType, Type implementationType, ConstructorInfo? constructor, ResolveOptions options) =>
		ConstructType(requestedType, implementationType, constructor, NamedParameterOverloads.Default,
			options);

	private object ConstructType(
		Type? requestedType, Type implementationType, NamedParameterOverloads parameters, ResolveOptions options) =>
		ConstructType(requestedType, implementationType, null, parameters, options);

	private object ConstructType(
		Type? requestedType, Type implementationType, ConstructorInfo? constructor,
		NamedParameterOverloads parameters, ResolveOptions options) {
		var typeToConstruct = implementationType;


		if (implementationType.IsGenericTypeDefinition()) {
			if (requestedType == null || !requestedType.IsGenericType() ||
			    requestedType.GetGenericArguments().Length == 0)
				throw new TinyIoCResolutionException(typeToConstruct);

			typeToConstruct = typeToConstruct.MakeGenericType(requestedType.GetGenericArguments());
		}

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


	private bool disposed;

	public void Dispose() {
		if (!disposed) {
			disposed = true;

			_RegisteredTypes.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}

public static class ReverseTypeExtender {
	extension(Type type) {
		public bool IsAbstract() => type.IsAbstract;
		public bool IsInterface() => type.IsInterface;
		public bool IsPrimitive() => type.IsPrimitive;
		public bool IsValueType() => type.IsValueType;
		public bool IsGenericType() => type.IsGenericType;
		public bool IsGenericTypeDefinition() => type.IsGenericTypeDefinition;
		public Type? BaseType() => type.BaseType;
	}
}