namespace RainbowMage.OverlayPlugin
{
    interface IApiBase : IEventReceiver
    {
        void OverlayMessage(string msg);

        void InitModernAPI();
    }
}
