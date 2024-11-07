using Monocle;

namespace Celeste.Mod.MoreLockBlocks;

public static class MoreLockBlocksGFX
{
    public static SpriteBank SpriteBank { get; set; }

    internal static void LoadContent()
    {
        SpriteBank = new SpriteBank(GFX.Game, "Graphics/MoreLockBlocks/Sprites.xml");
    }
}