using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace IINACT;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
public static class PrivateHelpers {
	extension(object obj) {
		public T GetProperty<T>(string propName) {
			var pi = obj.GetType()
				.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			return (T)pi?.GetValue(obj, null)!;
		}

		public T GetField<T>(string propName) {
			var t = obj.GetType();
			FieldInfo? fi = null;
			while (fi == null && t != null) {
				fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				t = t.BaseType;
			}

			return (T)fi?.GetValue(obj)!;
		}

		public void SetProperty<T>(string propName, T val) {
			var t = obj.GetType();
			if (t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null) {
				throw new ArgumentOutOfRangeException(nameof(propName),
					$@"Property {propName} was not found in Type {obj.GetType().FullName}");
			}

			t.InvokeMember(propName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
				null, obj,
				[val]);
		}

		public void SetField<T>(string propName, T val) {
			var t = obj.GetType();
			FieldInfo? fi = null;
			while (fi == null && t != null) {
				fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				t = t.BaseType;
			}

			fi?.SetValue(obj, val);
		}

		public MethodInfo? GetMethod(string methodName) {
			var type = obj.GetType();
			var method = type.GetMethod(methodName);
			return method ?? type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public object? CallMethod(string methodName, object?[]? parameters) {
			var method = obj.GetMethod(methodName);
			return method?.Invoke(obj, parameters);
		}
	}
}