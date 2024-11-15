using Celeste.Mod.DzhakeHelper;
using Celeste.Mod.DzhakeHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    public abstract class LegacyBaseLockBlock : Solid
    {
        public EntityID ID => component.ID;

        protected internal string overrideSpritePath => component.overrideSpritePath;
        public Sprite Sprite => component.Sprite;

        protected internal BaseLockBlockComponent.OpeningSettings openingSettings { get => component.openingSettings; set => component.openingSettings = value; }

        protected internal bool opening { get => component.opening; set => component.opening = value; }
        public bool UnlockingRegistered { get => component.UnlockingRegistered; set => component.UnlockingRegistered = value; }

        protected internal bool stepMusicProgress => component.stepMusicProgress;

        protected internal string unlockSfxName => component.unlockSfxName;
        public LegacyBaseLockBlock(EntityData data, Vector2 offset, EntityID id, string defaultSpriteID = "MoreLockBlocks_generic_lock", string defaultUnlockSfx = "event:/game/03_resort/key_unlock")
            : base(data.Position + offset, 32f, 32f, false)
        {
            Add(component = new BaseLockBlockComponent(this, data, offset, id, defaultSpriteID, defaultSpriteID));
        }
        protected readonly BaseLockBlockComponent component;
    }
    public class BaseLockBlockComponent : Component
    {
        public Solid RealEntity;
        public EntityID ID;

        protected internal readonly string overrideSpritePath;
        public readonly Sprite Sprite;

        public struct OpeningSettings
        {
            public bool VanillaKeys;

            public bool DzhakeHelperKeysNone;
            public bool DzhakeHelperKeysAll;
            public int DzhakeHelperKeyGroup;
        }
        protected internal OpeningSettings openingSettings;

        protected internal bool opening;
        public bool UnlockingRegistered;

        protected internal readonly bool stepMusicProgress;

        protected internal readonly string unlockSfxName;

        private PlayerCollider playerCollider;

        public BaseLockBlockComponent(Solid This, EntityData data, Vector2 offset, EntityID id, string defaultSpriteID = "MoreLockBlocks_generic_lock", string defaultUnlockSfx = "event:/game/03_resort/key_unlock")
            //: base(data.Position + offset, 32f, 32f, false)
            : base(true, true)
        {
            RealEntity = This;
            if (MoreLockBlocksModule.Instance.DzhakeHelperLoaded)
            {
                OnPlayer = default_OnPlayer_DzhakeHelperLoaded;
                UnlockRoutine = default_UnlockRoutine_DzhakeHelperLoaded;
                TryOpen = default_TryOpen_DzhakeHelperLoaded;
            }
            else
            {
                OnPlayer = default_OnPlayer_DzhakeHelperUnloaded;
                UnlockRoutine = default_UnlockRoutine_DzhakeHelperUnloaded;
                TryOpen = default_TryOpen_DzhakeHelperUnloaded;
            }



            ID = id;
            RealEntity.DisableLightsInside = false;
            RealEntity.Add(playerCollider = new PlayerCollider(OnPlayer, new Circle(60f, 16f, 16f)));

            RealEntity.Add(Sprite = string.IsNullOrWhiteSpace(overrideSpritePath = data.Attr("spritePath", "")) ? MoreLockBlocksGFX.SpriteBank.Create(defaultSpriteID) : BuildCustomSprite(overrideSpritePath));
            Sprite.Play("idle");
            Sprite.Position = new Vector2(RealEntity.Width / 2f, RealEntity.Height / 2f);

            string dzhakeHelperKeySettings = data.Attr("dzhakeHelperKeySettings", "");
            bool _ = int.TryParse(dzhakeHelperKeySettings, out int dzhakeHelperKeyGroup);
            openingSettings = new()
            {
                VanillaKeys = data.Bool("useVanillaKeys", true),
                DzhakeHelperKeysNone = string.Equals(dzhakeHelperKeySettings, ""),
                DzhakeHelperKeysAll = string.Equals(dzhakeHelperKeySettings, "*"),
                DzhakeHelperKeyGroup = dzhakeHelperKeyGroup,
            };

            stepMusicProgress = data.Bool("stepMusicProgress", false);
            if (string.IsNullOrWhiteSpace(unlockSfxName = data.Attr("unlock_sfx", "")))
                unlockSfxName = defaultUnlockSfx;
            else
                unlockSfxName = SFX.EventnameByHandle(unlockSfxName);
        }

        protected internal static Sprite BuildCustomSprite(string spritePath)
        {
            /*
                <Justify x="0.5" y="0.5" />
                <Loop id="idle" delay="0.1" frames="0"/>
                <Anim id="open" delay="0.06" frames="0-9"/>
                <Anim id="burst" delay="0.06" frames="10-18"/>
            */

            Sprite sprite = new(GFX.Game, spritePath);

            sprite.AddLoop("idle", "", 0.1f, 0);
            sprite.Add("open", "", 0.06f, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            sprite.Add("burst", "", 0.06f, 10, 11, 12, 13, 14, 15, 16, 17, 18);

            sprite.JustifyOrigin(0.5f, 0.5f);
            return sprite;
        }

        #region OnPlayer

        protected internal Action<Player> OnPlayer;
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal virtual void default_OnPlayer_DzhakeHelperLoaded(Player player)
        {
            if (opening)
            {
                return;
            }
            foreach (Follower follower in player.Leader.Followers)
            {
                if (follower.Entity is Key key && !key.StartedUsing && openingSettings.VanillaKeys)
                {
                    TryOpen(player, follower);
                    break;
                }
                if (follower.Entity is CustomKey key2 && !key2.StartedUsing && !openingSettings.DzhakeHelperKeysNone && (openingSettings.DzhakeHelperKeysAll || key2.OpenAny || key2.Group == openingSettings.DzhakeHelperKeyGroup))
                {
                    TryOpen(player, follower);
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal virtual void default_OnPlayer_DzhakeHelperUnloaded(Player player)
        {
            if (opening)
            {
                return;
            }
            foreach (Follower follower in player.Leader.Followers)
            {
                if (follower.Entity is Key key && !key.StartedUsing && openingSettings.VanillaKeys)
                {
                    TryOpen(player, follower);
                    break;
                }
            }
        }

        #endregion
        #region TryOpen

        protected internal Action<Player, Follower> TryOpen;
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal virtual void default_TryOpen_DzhakeHelperLoaded(Player player, Follower fol)
        {
            RealEntity.Collidable = false;
            if (!RealEntity.Scene.CollideCheck<Solid>(player.Center, RealEntity.Center))
            {
                opening = true;
                if (fol.Entity is Key key)
                {
                    key.StartedUsing = true;
                }
                else if (fol.Entity is CustomKey key2)
                {
                    key2.StartedUsing = true;
                }
                RealEntity.Add(new Coroutine(UnlockRoutine(fol)));
            }
            RealEntity.Collidable = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal virtual void default_TryOpen_DzhakeHelperUnloaded(Player player, Follower fol)
        {
            RealEntity.Collidable = false;
            if (!RealEntity.Scene.CollideCheck<Solid>(player.Center, RealEntity.Center))
            {
                opening = true;
                if (fol.Entity is Key key)
                {
                    key.StartedUsing = true;
                }
                RealEntity.Add(new Coroutine(UnlockRoutine(fol)));
            }
            RealEntity.Collidable = true;
        }

        #endregion
        #region UnlockRoutine

        protected internal Func<Follower, IEnumerator> UnlockRoutine;
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal virtual IEnumerator default_UnlockRoutine_DzhakeHelperLoaded(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(unlockSfxName, RealEntity);
            emitter.Source.DisposeOnTransition = true;
            Level level = RealEntity.SceneAs<Level>();

            Key key = fol.Entity as Key;
            CustomKey key2 = fol.Entity as CustomKey;
            if (key is not null)
            {
                RealEntity.Add(new Coroutine(key.UseRoutine(RealEntity.Center + new Vector2(0f, 2f))));
            }
            else if (key2 is not null)
            {
                RealEntity.Add(new Coroutine(key2.UseRoutine(RealEntity.Center + new Vector2(0f, 2f))));
            }
            yield return 1.2f;

            UnlockingRegistered = true;
            if (stepMusicProgress)
            {
                level.Session.Audio.Music.Progress++;
                level.Session.Audio.Apply();
            }
            level.Session.DoNotLoad.Add(ID);
            if (key is not null)
            {
                key.RegisterUsed();

                while (key.Turning)
                {
                    yield return null;
                }
            }
            else if (key2 is not null)
            {
                key2.RegisterUsed();
                DzhakeHelperModule.Session.CurrentKeys.RemoveAll(info => info.ID.ID == key2.ID.ID);

                while (key2.Turning)
                {
                    yield return null;
                }
            }

            RealEntity.Tag |= Tags.TransitionUpdate;
            RealEntity.Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return Sprite.PlayRoutine("burst");

            RealEntity.RemoveSelf();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal virtual IEnumerator default_UnlockRoutine_DzhakeHelperUnloaded(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(unlockSfxName, RealEntity);
            emitter.Source.DisposeOnTransition = true;
            Level level = RealEntity.SceneAs<Level>();

            Key key = fol.Entity as Key;
            RealEntity.Add(new Coroutine(key.UseRoutine(RealEntity.Center + new Vector2(0f, 2f))));
            yield return 1.2f;

            UnlockingRegistered = true;
            if (stepMusicProgress)
            {
                level.Session.Audio.Music.Progress++;
                level.Session.Audio.Apply();
            }
            level.Session.DoNotLoad.Add(ID);
            key.RegisterUsed();
            while (key.Turning)
            {
                yield return null;
            }

            RealEntity.Tag |= Tags.TransitionUpdate;
            RealEntity.Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return Sprite.PlayRoutine("burst");

            RealEntity.RemoveSelf();
        }

        internal void Remove()
        {
            playerCollider.RemoveSelf();
            Sprite.RemoveSelf();
        }

        #endregion
    }
}