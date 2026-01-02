using System;
using RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;

internal interface IAggroMemory60 : IAggroMemory {
}

internal class AggroMemory60 : AggroMemory, IAggroMemory60 {
    private const int aggroEnmityOffset = -2336;

    // Aggro uses the same signature as Enmity
    public AggroMemory60(TinyIoCContainer container)
        : base(container, EnmityMemory60.enmitySignature, aggroEnmityOffset) {
    }

    public override Version GetVersion() => new(6, 0);
}