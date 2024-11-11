using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Linq;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    [Tracked]
    [CustomEntity("MoreLockBlocks/GlassLockBlock")]
    public class GlassLockBlock : BaseLockBlock
    {
        private const string spriteID = "MoreLockBlocks_generic_lock";

        private readonly List<Rectangle?> frameMetadata = new();
        public Rectangle? RenderBounds
        {
            get
            {
                if (Sprite.Texture is not null)
                {
                    if (TryGetFrameIndexFromPath(Sprite.Texture.AtlasPath, frameMetadata.Count, out int index))
                    {
                        return frameMetadata[index];
                    }
                    else
                    {
                        throw new Exception("Could not find metadata associated with current frame index, does your sprite match vanilla's format?");
                    }
                }
                return new(-16, -16, 32, 32);
            }
        }

        private static bool TryGetFrameIndexFromPath(string path, int max, out int index)
        {
            int lastDigitFromEndIndex = path.Length - 1 - path.Reverse().LastIndexWhere(char.IsDigit);
            bool result = int.TryParse(path.AsSpan(lastDigitFromEndIndex), out int i);
            if (result && i < max)
            {
                index = i;
                return true;
            }
            else
            {
                index = 0;
                return false;
            }
        }

        private readonly bool behindFgTiles;

        public GlassLockBlock(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id, spriteID)
        {
            Depth = (behindFgTiles = data.Bool("behindFgTiles", false)) ? -9995 : -10000;
            Add(new LightOcclude());
            Add(new MirrorSurface());
            SurfaceSoundIndex = 32;

            BuildFrameMetadata();
        }

        private void BuildFrameMetadata()
        {
            foreach (SpriteDataSource source in MoreLockBlocksGFX.SpriteBank.SpriteData[spriteID].Sources)
            {
                if (source.XML["Metadata"] is XmlElement item)
                {
                    string[] array = item.Attr("bounds").Split(';');
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i].Equals("x", StringComparison.OrdinalIgnoreCase))
                        {
                            frameMetadata.Add(null);
                        }
                        else
                        {
                            int[] args = array[i].Split(',').Select(s => Convert.ToInt32(s)).ToArray();
                            frameMetadata.Add(new Rectangle(args[0], args[1], args[2], args[3]));
                        }
                    }
                    return;
                }
            }
        }

        public override void Render()
        {
            if (RenderBounds is Rectangle rb && Scene.Tracker.GetEntity<GlassLockBlockController>() is GlassLockBlockController controller)
            {
                Rectangle outline = new((int)(Center.X + rb.Left), (int)(Center.Y + rb.Top), rb.Width, rb.Height);
                Color lineColor = controller.LineColor;

                if (controller.VanillaEdgeBehavior)
                {
                    Draw.Line(outline.TopLeft() - Vector2.UnitY, outline.TopRight() - Vector2.UnitY, lineColor);
                    Draw.Line(outline.TopRight() + Vector2.UnitX, outline.BottomRight() + Vector2.UnitX, lineColor);
                    Draw.Line(outline.BottomRight() + Vector2.UnitY, outline.BottomLeft() + Vector2.UnitY, lineColor);
                    Draw.Line(outline.BottomLeft() - Vector2.UnitX, outline.TopLeft() - Vector2.UnitX, lineColor);
                }
                else
                {
                    Draw.HollowRect(outline, lineColor);
                }
            }

            base.Render();
        }
    }
}