using System.Collections.Generic;

namespace Celeste.Mod.MoreLockBlocks;

#nullable enable

public class MoreLockBlocksModuleSession : EverestModuleSession
{
    public class GlassLockBlockState
    {
        public required string StarColors { get; set; }
        public required string BgColor { get; set; }
        public required string LineColor { get; set; }
        public required string RayColor { get; set; }
        public bool Wavy { get; set; }
        public bool VanillaEdgeBehavior { get; set; }
    }

    public GlassLockBlockState? GlassLockBlockCurrentSettings = null;

    public List<EntityID> UnlockedDreamLockBlocks = new();

    public Dictionary<EntityID, bool> DreamBlockDummyStates = new();
}