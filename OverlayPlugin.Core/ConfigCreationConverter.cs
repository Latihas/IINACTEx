using System;
using Newtonsoft.Json.Converters;

namespace RainbowMage.OverlayPlugin;

internal class ConfigCreationConverter(TinyIoCContainer container) : CustomCreationConverter<IOverlayConfig> {
	public override IOverlayConfig Create(Type objectType) {
		var construct = objectType.GetConstructor([typeof(TinyIoCContainer), typeof(string)]);
		if (construct == null) {
			construct = objectType.GetConstructor([typeof(string)]);
			if (construct == null) {
				throw new Exception("No valid constructor found for config type " + objectType + "!");
			}

			return (IOverlayConfig)construct.Invoke([null]);
		}
		return (IOverlayConfig)construct.Invoke([container, null]);
	}
}