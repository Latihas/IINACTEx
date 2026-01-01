using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.OverlayPlugin
{
    interface IApiBase : IEventReceiver
    {
        void OverlayMessage(string msg);

        void InitModernAPI();
    }
}
