using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using IL.Celeste;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    [Tracked]
    [CustomEntity("MoreLockBlocks/GlassLockBlockController")]
    public class GlassLockBlockController : Entity
    {
        private struct Star
        {
            public Vector2 Position;
            public MTexture Texture;
            public Color Color;
            public Vector2 Scroll;
        }

        private struct Ray
        {
            public Vector2 Position;
            public float Width;
            public float Length;
            public Color Color;
        }

        private const int StarCount = 100;
        private const int RayCount = 50;

        private Star[] stars = new Star[StarCount];
        private Ray[] rays = new Ray[RayCount];

        private readonly VertexPositionColor[] verts = new VertexPositionColor[2700];

        private Vector2 rayNormal = Calc.SafeNormalize(new Vector2(-5f, -8f));

        private VirtualRenderTarget beamsTarget;
        private VirtualRenderTarget starsTarget;
        private VirtualRenderTarget stencilTarget;

        private readonly BlendState overwriteColorBlendState = new()
        {
            ColorSourceBlend = Blend.DestinationAlpha,
            ColorDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
        };

        private bool hasBlocks;

        public readonly Color[] StarColors;
        public readonly Color BgColor, LineColor, RayColor;
        public readonly bool VanillaEdgeBehavior;

        public GlassLockBlockController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            string[] starColorsAsStrings = data.Attr("starColors").Split(',');
            StarColors = new Color[starColorsAsStrings.Length];
            for (int i = 0; i < StarColors.Length; i++)
            {
                StarColors[i] = Calc.HexToColor(starColorsAsStrings[i]);
            }
            BgColor = Calc.HexToColor(data.Attr("bgColor"));
            LineColor = Calc.HexToColor(data.Attr("lineColor"));
            RayColor = Calc.HexToColor(data.Attr("rayColor"));
            VanillaEdgeBehavior = data.Bool("vanillaEdgeBehavior");

            Add(new BeforeRenderHook(BeforeRender));
            Depth = -9990;
            if (data.Bool("wavy"))
            {
                Add(new DisplacementRenderHook(OnDisplacementRender));
            }

            if (data.Bool("persistent"))
            {
                MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings = new MoreLockBlocksModuleSession.GlassLockBlockState
                {
                    StarColors = data.Attr("starColors"),
                    BgColor = data.Attr("bgColor"),
                    LineColor = data.Attr("lineColor"),
                    RayColor = data.Attr("rayColor"),
                    Wavy = data.Bool("wavy"),
                    VanillaEdgeBehavior = data.Bool("vanillaEdgeBehavior"),
                };
            }
            else
            {
                MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings = null;
            }
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            List<MTexture> starTextures = GFX.Game.GetAtlasSubtextures("particles/stars/");
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].Position.X = Calc.Random.Next(320);
                stars[i].Position.Y = Calc.Random.Next(180);
                stars[i].Texture = Calc.Random.Choose(starTextures);
                stars[i].Color = Calc.Random.Choose(StarColors);
                stars[i].Scroll = Vector2.One * Calc.Random.NextFloat(0.05f);
            }

            for (int k = 0; k < rays.Length; k++)
            {
                rays[k].Position.X = Calc.Random.Next(320);
                rays[k].Position.Y = Calc.Random.Next(180);
                rays[k].Width = Calc.Random.Range(4f, 16f);
                rays[k].Length = Calc.Random.Choose(48, 96, 128);
                rays[k].Color = RayColor * Calc.Random.Range(0.2f, 0.4f);
            }
        }

        private void BeforeRender()
        {
            List<GlassLockBlock> glassBlocks = GetGlassBlocksToAffect().ToList();
            hasBlocks = glassBlocks.Count > 0;
            if (!hasBlocks)
            {
                return;
            }

            Camera camera = (Scene as Level).Camera;
            int screenWidth = GameplayBuffers.Gameplay.Width;
            int screenHeight = GameplayBuffers.Gameplay.Height;

            starsTarget ??= VirtualContent.CreateRenderTarget("MoreLockBlocks/glass-lock-block-surfaces", screenWidth, screenHeight);
            Engine.Graphics.GraphicsDevice.SetRenderTarget(starsTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            Vector2 origin = new(8f, 8f);
            for (int i = 0; i < stars.Length; i++)
            {
                MTexture starTexture = stars[i].Texture;
                Color starColor = stars[i].Color;
                Vector2 starScroll = stars[i].Scroll;
                Vector2 starActualPosition = new(Mod(stars[i].Position.X - camera.X * (1f - starScroll.X), screenWidth), Mod(stars[i].Position.Y - camera.Y * (1f - starScroll.Y), screenHeight));
                starTexture.Draw(starActualPosition, origin, starColor);
                if (starActualPosition.X < origin.X)
                {
                    starTexture.Draw(starActualPosition + new Vector2(screenWidth, 0f), origin, starColor);
                }
                else if (starActualPosition.X > screenWidth - origin.X)
                {
                    starTexture.Draw(starActualPosition - new Vector2(screenWidth, 0f), origin, starColor);
                }
                if (starActualPosition.Y < origin.Y)
                {
                    starTexture.Draw(starActualPosition + new Vector2(0f, screenHeight), origin, starColor);
                }
                else if (starActualPosition.Y > screenHeight - origin.Y)
                {
                    starTexture.Draw(starActualPosition - new Vector2(0f, screenHeight), origin, starColor);
                }
            }
            Draw.SpriteBatch.End();

            int vertex = 0;
            for (int j = 0; j < rays.Length; j++)
            {
                Vector2 rayPosition = new(Mod(rays[j].Position.X - camera.X * 0.9f, screenWidth), Mod(rays[j].Position.Y - camera.Y * 0.9f, screenHeight));
                DrawRay(rayPosition, ref vertex, ref rays[j]);
                if (rayPosition.X < 64f)
                {
                    DrawRay(rayPosition + new Vector2(screenWidth, 0f), ref vertex, ref rays[j]);
                }
                else if (rayPosition.X > (screenWidth - 64))
                {
                    DrawRay(rayPosition - new Vector2(screenWidth, 0f), ref vertex, ref rays[j]);
                }
                if (rayPosition.Y < 64f)
                {
                    DrawRay(rayPosition + new Vector2(0f, screenHeight), ref vertex, ref rays[j]);
                }
                else if (rayPosition.Y > (screenHeight - 64))
                {
                    DrawRay(rayPosition - new Vector2(0f, screenHeight), ref vertex, ref rays[j]);
                }
            }

            beamsTarget ??= VirtualContent.CreateRenderTarget("MoreLockBlocks/glass-lock-block-beams", screenWidth, screenHeight);
            Engine.Graphics.GraphicsDevice.SetRenderTarget(beamsTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
            GFX.DrawVertices(Matrix.Identity, verts, vertex, null, null);
        }

        private void DrawRay(Vector2 position, ref int vertex, ref Ray ray)
        {
            Vector2 val = new(0f - rayNormal.Y, rayNormal.X);
            Vector2 value2 = rayNormal * ray.Width * 0.5f;
            Vector2 value3 = val * ray.Length * 0.25f * 0.5f;
            Vector2 value4 = val * ray.Length * 0.5f * 0.5f;
            Vector2 v = position + value2 - value3 - value4;
            Vector2 v2 = position - value2 - value3 - value4;
            Vector2 vector = position + value2 - value3;
            Vector2 vector2 = position - value2 - value3;
            Vector2 vector3 = position + value2 + value3;
            Vector2 vector4 = position - value2 + value3;
            Vector2 v3 = position + value2 + value3 + value4;
            Vector2 v4 = position - value2 + value3 + value4;
            Color color = ray.Color;

            Quad(ref vertex, v, vector, vector2, v2, Color.Transparent, color, color, Color.Transparent);
            Quad(ref vertex, vector, vector3, vector4, vector2, color, color, color, color);
            Quad(ref vertex, vector3, v3, v4, vector4, color, Color.Transparent, Color.Transparent, color);
        }

        private void Quad(ref int vertex, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color c0, Color c1, Color c2, Color c3)
        {
            verts[vertex].Position.X = v0.X;
            verts[vertex].Position.Y = v0.Y;
            verts[vertex++].Color = c0;
            verts[vertex].Position.X = v1.X;
            verts[vertex].Position.Y = v1.Y;
            verts[vertex++].Color = c1;
            verts[vertex].Position.X = v2.X;
            verts[vertex].Position.Y = v2.Y;
            verts[vertex++].Color = c2;
            verts[vertex].Position.X = v0.X;
            verts[vertex].Position.Y = v0.Y;
            verts[vertex++].Color = c0;
            verts[vertex].Position.X = v2.X;
            verts[vertex].Position.Y = v2.Y;
            verts[vertex++].Color = c2;
            verts[vertex].Position.X = v3.X;
            verts[vertex].Position.Y = v3.Y;
            verts[vertex++].Color = c3;
        }

        public override void Render()
        {
            if (!hasBlocks)
            {
                return;
            }

            Vector2 position = (Scene as Level).Camera.Position;
            IEnumerable<GlassLockBlock> glassBlocks = GetGlassBlocksToAffect();

            foreach (GlassLockBlock block in glassBlocks)
            {
                if (block.RenderBounds is Rectangle rb)
                {
                    Draw.Rect(block.Center.X + rb.Left, block.Center.Y + rb.Top, rb.Width, rb.Height, BgColor);
                }
            }

            if (starsTarget != null && !starsTarget.IsDisposed)
            {
                foreach (GlassLockBlock block in glassBlocks)
                {
                    if (block.RenderBounds is Rectangle rb)
                    {
                        Rectangle clipTarget = new((int)(block.Center.X + rb.Left - position.X), (int)(block.Center.Y + rb.Top - position.Y), rb.Width, rb.Height);
                        Draw.SpriteBatch.Draw(starsTarget, block.Center + rb.TopLeft(), clipTarget, Color.White);
                    }
                }
            }

            if (beamsTarget != null && !beamsTarget.IsDisposed)
            {
                foreach (GlassLockBlock block in glassBlocks)
                {
                    if (block.RenderBounds is Rectangle rb)
                    {
                        Rectangle clipTarget = new((int)(block.Center.X + rb.Left - position.X), (int)(block.Center.Y + rb.Top - position.Y), rb.Width, rb.Height);
                        Draw.SpriteBatch.Draw(beamsTarget, block.Center + rb.TopLeft(), clipTarget, Color.White);
                    }
                }
            }
        }

        protected virtual IEnumerable<GlassLockBlock> GetGlassBlocksToAffect()
        {
            return Scene.Tracker.GetEntities<GlassLockBlock>().OfType<GlassLockBlock>();
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Dispose();
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
            Dispose();
        }

        public void Dispose()
        {
            if (starsTarget != null && !starsTarget.IsDisposed)
            {
                starsTarget.Dispose();
            }
            if (beamsTarget != null && !beamsTarget.IsDisposed)
            {
                beamsTarget.Dispose();
            }
            if (stencilTarget != null && !stencilTarget.IsDisposed)
            {
                stencilTarget.Dispose();
            }
            starsTarget = null;
            beamsTarget = null;
            stencilTarget = null;
        }

        private static float Mod(float x, float m)
        {
            return (x % m + m) % m;
        }

        private void OnDisplacementRender()
        {
            Camera camera = SceneAs<Level>().Camera;
            IEnumerable<GlassLockBlock> blocks = GetGlassBlocksToAffect();

            foreach (GlassLockBlock block in blocks)
            {
                if (block.RenderBounds is Rectangle rb)
                {
                    if (VanillaEdgeBehavior)
                    {
                        Draw.Rect(block.Center.X + rb.Left, block.Center.Y + rb.Top, rb.Width, rb.Height, new Color(0.5f, 0.5f, 0.2f, 1f));
                    }
                    else
                    {
                        Draw.Rect(block.Center.X + rb.Left + 1f, block.Center.Y + rb.Top + 1f, rb.Width - 2f, rb.Height - 2f, new Color(0.5f, 0.5f, 0.2f, 1f));
                    }
                }
            }
            Draw.SpriteBatch.End();

            stencilTarget ??= VirtualContent.CreateRenderTarget("MoreLockBlocks/glass-lock-block-displacement-override", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height);
            Engine.Graphics.GraphicsDevice.SetRenderTarget(stencilTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);
            foreach (GlassLockBlock block in blocks)
            {
                block.Sprite.Texture.DrawCentered(block.Center);
            }
            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, overwriteColorBlendState, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);
            foreach (GlassLockBlock block in blocks)
            {
                MTexture tex = block.Sprite.Texture;
                Draw.Rect(block.Center.X - tex.Width / 2, block.Center.Y - tex.Height / 2, tex.Width, tex.Height, new Color(0.5f, 0.5f, 0f, 1f));
            }
            Draw.SpriteBatch.End();

            Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Displacement);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);
            Draw.SpriteBatch.Draw(stencilTarget, Vector2.Zero, Color.White);
            Draw.SpriteBatch.End();

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);
        }

        #region Hooks

        public static void Load()
        {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
        }

        public static void Unload()
        {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            if (self.Session.LevelData != null && !self.Session.LevelData.Entities.Any((EntityData entity) => entity.Name == "MoreLockBlocks/GlassLockBlockController"))
            {
                EntityData restoredData = new();
                if (MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings is not null)
                {
                    restoredData.Values = new Dictionary<string, object>
                    {
                        {
                            "starColors",
                            MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings.StarColors
                        },
                        {
                            "bgColor",
                            MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings.BgColor
                        },
                        {
                            "lineColor",
                            MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings.LineColor
                        },
                        {
                            "rayColor",
                            MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings.RayColor
                        },
                        {
                            "wavy",
                            MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings.Wavy
                        },
                        {
                            "vanillaEdgeBehavior",
                            MoreLockBlocksModule.Session.GlassLockBlockCurrentSettings.VanillaEdgeBehavior
                        },
                        { "persistent", true }
                    };
                    self.Add(new GlassLockBlockController(restoredData, Vector2.Zero));
                }
                else
                {
                    restoredData.Values = new Dictionary<string, object>
                    {
                        { "starColors", "7f9fba,9bd1cd,bacae3" },
                        { "bgColor", "0d2e89" },
                        { "lineColor", "ffffff" },
                        { "rayColor", "ffffff" },
                        { "wavy", true },
                        { "vanillaEdgeBehavior", true },
                        { "persistent", true }
                    };

                }
                self.Add(new GlassLockBlockController(restoredData, Vector2.Zero));
            }

            orig(self, playerIntro, isFromLoader);
        }

        #endregion
    }
}