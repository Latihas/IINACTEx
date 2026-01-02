using System;
using Newtonsoft.Json.Converters;

namespace RainbowMage.OverlayPlugin
{
    class ConfigCreationConverter : CustomCreationConverter<IOverlayConfig>
    {
        private TinyIoCContainer _container;

        public ConfigCreationConverter(TinyIoCContainer container)
        {
            _container = container;
        }

        public override IOverlayConfig Create(Type objectType)
        {
            var construct = objectType.GetConstructor(new[] { typeof(TinyIoCContainer), typeof(string) });
            if (construct == null)
            {
                construct = objectType.GetConstructor(new[] { typeof(string) });
                if (construct == null)
                {
                    throw new Exception("No valid constructor found for config type " + objectType + "!");
                }

                return (IOverlayConfig)construct.Invoke(new object[] { null });
            }
            return (IOverlayConfig)construct.Invoke(new object[] { _container, null });
        }
    }
}
