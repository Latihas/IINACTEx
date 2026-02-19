using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SilverDasher.ACT;

public class StrNullToEmptyContractResolver : DefaultContractResolver {
    private class DynamicStrNullToEmptyValueProvider(MemberInfo memberInfo) : DynamicValueProvider(memberInfo), IValueProvider {
        public new object GetValue(object target) => base.GetValue(target) ?? string.Empty;
    }

    private class ReflectionStrNullToEmptyValueProvider(MemberInfo memberInfo) : ReflectionValueProvider(memberInfo), IValueProvider {
        public new object GetValue(object target) => base.GetValue(target) ?? string.Empty;
    }

    internal static StrNullToEmptyContractResolver DefaultInstance;

    static StrNullToEmptyContractResolver() {
        DefaultInstance = new StrNullToEmptyContractResolver();
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
        var jsonProperty = base.CreateProperty(member, memberSerialization);
        if (jsonProperty.PropertyType != typeof(string)) return jsonProperty;
        jsonProperty.ValueProvider = jsonProperty.ValueProvider switch {
            ReflectionValueProvider => new ReflectionStrNullToEmptyValueProvider(member),
            DynamicValueProvider => new DynamicStrNullToEmptyValueProvider(member),
            _ => jsonProperty.ValueProvider
        };
        return jsonProperty;
    }
}