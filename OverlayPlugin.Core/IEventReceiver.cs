using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin
{
    public interface IEventReceiver
    {
        string Name { get; }

        void HandleEvent(JObject e);
    }
}
