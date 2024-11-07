using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Linq;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    [Tracked]
    [CustomEntity("MoreLockBlocks/GlassLockBlock")]
    public class GlassLockBlock : Solid
    {
        public EntityID ID;

        private const string spriteID = "lockdoor_glass_key";
        public readonly Sprite Sprite;

        private readonly Dictionary<string, Rectangle?> frameMetadata = new(StringComparer.OrdinalIgnoreCase);
        public Rectangle? RenderBounds
        {
            get
            {
                if (Sprite.Texture != null && frameMetadata.TryGetValue(Sprite.Texture.AtlasPath, out var value))
                {
                    return value;
                }
                return new(-16, -16, 32, 32);
            }
        }

        private readonly bool behindFgTiles;

        private bool opening;
        public bool UnlockingRegistered;

        private readonly bool stepMusicProgress;

        private readonly string unlockSfxName;

        public GlassLockBlock(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, 32f, 32f, false)
        {
            ID = id;
            Depth = (behindFgTiles = data.Bool("behindFgTiles", false)) ? -9995 : -10000;
            Add(new LightOcclude());
            Add(new MirrorSurface());
            SurfaceSoundIndex = 32;
            DisableLightsInside = false;
            Add(new PlayerCollider(OnPlayer, new Circle(60f, 16f, 16f)));

            Add(Sprite = MoreLockBlocksGFX.SpriteBank.Create(spriteID));
            Sprite.Play("idle");
            Sprite.Position = new Vector2(Width / 2f, Height / 2f);
            BuildFrameMetadata();

            stepMusicProgress = data.Bool("stepMusicProgress", false);
            if (string.IsNullOrWhiteSpace(unlockSfxName = data.Attr("unlock_sfx", "")))
                unlockSfxName = "event:/game/03_resort/key_unlock";
            else
                unlockSfxName = SFX.EventnameByHandle(unlockSfxName);
        }

        private void BuildFrameMetadata()
        {
            foreach (SpriteDataSource source in MoreLockBlocksGFX.SpriteBank.SpriteData[spriteID].Sources)
            {
                XmlElement xmlElement = source.XML["Metadata"];
                string text = source.Path;
                if (xmlElement == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(source.OverridePath))
                {
                    text = source.OverridePath;
                }
                foreach (XmlElement item in xmlElement.GetElementsByTagName("Frames"))
                {
                    string text2 = text + item.Attr("path", "");
                    string[] array = item.Attr("bounds").Split(';');
                    for (int i = 0; i < array.Length; i++)
                    {
                        string text3 = text2 + ((i < 10) ? "0" : "") + i;
                        if (i == 0 && !GFX.Game.Has(text3))
                        {
                            text3 = text2;
                        }
                        if (array[i].Equals("x", StringComparison.OrdinalIgnoreCase))
                        {
                            frameMetadata[text3] = null;
                        }
                        else
                        {
                            int[] args = array[i].Split(',').Select(s => Convert.ToInt32(s)).ToArray();
                            frameMetadata[text3] = new Rectangle(args[0], args[1], args[2], args[3]);
                        }
                    }
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

        private void OnPlayer(Player player)
        {
            if (opening)
            {
                return;
            }
            foreach (Follower follower in player.Leader.Followers)
            {
                if (follower.Entity is Key && !(follower.Entity as Key).StartedUsing)
                {
                    TryOpen(player, follower);
                    break;
                }
            }
        }

        private void TryOpen(Player player, Follower fol)
        {
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(player.Center, Center))
            {
                opening = true;
                (fol.Entity as Key).StartedUsing = true;
                Add(new Coroutine(UnlockRoutine(fol)));
            }
            Collidable = true;
        }

        private IEnumerator UnlockRoutine(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(unlockSfxName, this);
            emitter.Source.DisposeOnTransition = true;
            Level level = SceneAs<Level>();
            Key key = fol.Entity as Key;
            Add(new Coroutine(key.UseRoutine(Center + new Vector2(0f, 2f))));
            yield return 1.2f;
            UnlockingRegistered = true;
            if (stepMusicProgress)
            {
                level.Session.Audio.Music.Progress++;
                level.Session.Audio.Apply(forceSixteenthNoteHack: false);
            }
            level.Session.DoNotLoad.Add(ID);
            key.RegisterUsed();
            while (key.Turning)
            {
                yield return null;
            }
            Tag |= Tags.TransitionUpdate;
            Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return Sprite.PlayRoutine("open");
            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return Sprite.PlayRoutine("burst");
            RemoveSelf();
        }
    }
}