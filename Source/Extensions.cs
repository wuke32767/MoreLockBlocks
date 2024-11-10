using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

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

    public static int LastIndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        int index = 0;
        int resultIndex = -1;
        foreach (T item in source)
        {
            if (predicate.Invoke(item)) resultIndex = index;
            index++;
        }
        return resultIndex;
    }
}