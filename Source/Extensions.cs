using Microsoft.Xna.Framework;

namespace Celeste.Mod.MoreLockBlocks;

public static class Extensions
{
    public static Vector2 TopLeft(this Rectangle rect)
    {
        return new Vector2(rect.Left, rect.Top);
    }
    public static Vector2 TopRight(this Rectangle rect)
    {
        return new Vector2(rect.Right, rect.Top);
    }
    public static Vector2 BottomLeft(this Rectangle rect)
    {
        return new Vector2(rect.Left, rect.Bottom);
    }
    public static Vector2 BottomRight(this Rectangle rect)
    {
        return new Vector2(rect.Right, rect.Bottom);
    }
}