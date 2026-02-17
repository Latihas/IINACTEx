using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SilverDasher.ACT;

public class StrNullToEmptyContractResolver : DefaultContractResolver
{
	private class DynamicStrNullToEmptyValueProvider : DynamicValueProvider, IValueProvider
	{
		public DynamicStrNullToEmptyValueProvider(MemberInfo memberInfo)
			: base(memberInfo)
		{
		}

		public new object GetValue(object target)
		{
			object value = base.GetValue(target);
			if (value == null)
			{
				return string.Empty;
			}
			return value;
		}
	}

	private class ReflectionStrNullToEmptyValueProvider : ReflectionValueProvider, IValueProvider
	{
		public ReflectionStrNullToEmptyValueProvider(MemberInfo memberInfo)
			: base(memberInfo)
		{
		}

		public new object GetValue(object target)
		{
			object value = base.GetValue(target);
			if (value == null)
			{
				return string.Empty;
			}
			return value;
		}
	}

	public static StrNullToEmptyContractResolver DefaultInstance;

	static StrNullToEmptyContractResolver()
	{
		DefaultInstance = new StrNullToEmptyContractResolver();
	}

	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		JsonProperty jsonProperty = base.CreateProperty(member, memberSerialization);
		if (jsonProperty.PropertyType != typeof(string))
		{
			return jsonProperty;
		}
		if (jsonProperty.ValueProvider is ReflectionValueProvider)
		{
			jsonProperty.ValueProvider = new ReflectionStrNullToEmptyValueProvider(member);
		}
		else if (jsonProperty.ValueProvider is DynamicValueProvider)
		{
			jsonProperty.ValueProvider = new DynamicStrNullToEmptyValueProvider(member);
		}
		return jsonProperty;
	}
}
