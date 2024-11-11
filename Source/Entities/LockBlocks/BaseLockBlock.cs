using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Runtime.CompilerServices;
using Celeste.Mod.DzhakeHelper;
using Celeste.Mod.DzhakeHelper.Entities;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    public abstract class BaseLockBlock : Solid
    {
        public EntityID ID;

        protected readonly string overrideSpritePath;
        public readonly Sprite Sprite;

        protected struct OpeningSettings
        {
            public bool VanillaKeys;

            public bool DzhakeHelperKeysNone;
            public bool DzhakeHelperKeysAll;
            public int DzhakeHelperKeyGroup;
        }
        protected OpeningSettings openingSettings;

        protected bool opening;
        public bool UnlockingRegistered;

        protected readonly bool stepMusicProgress;

        protected readonly string unlockSfxName;

        public BaseLockBlock(EntityData data, Vector2 offset, EntityID id, string defaultSpriteID = "MoreLockBlocks_generic_lock", string defaultUnlockSfx = "event:/game/03_resort/key_unlock") : base(data.Position + offset, 32f, 32f, false)
        {
            ID = id;
            DisableLightsInside = false;
            Add(new PlayerCollider(OnPlayer, new Circle(60f, 16f, 16f)));

            Add(Sprite = string.IsNullOrWhiteSpace(overrideSpritePath = data.Attr("spritePath", "")) ? MoreLockBlocksGFX.SpriteBank.Create(defaultSpriteID) : BuildCustomSprite(overrideSpritePath));
            Sprite.Play("idle");
            Sprite.Position = new Vector2(Width / 2f, Height / 2f);

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

        protected static Sprite BuildCustomSprite(string spritePath)
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

        protected void OnPlayer(Player player)
        {
            if (MoreLockBlocksModule.Instance.DzhakeHelperLoaded)
            {
                OnPlayer_DzhakeHelperLoaded(player);
            }
            else
            {
                OnPlayer_DzhakeHelperUnloaded(player);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual void OnPlayer_DzhakeHelperLoaded(Player player)
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
        protected virtual void OnPlayer_DzhakeHelperUnloaded(Player player)
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

        protected void TryOpen(Player player, Follower fol)
        {
            if (MoreLockBlocksModule.Instance.DzhakeHelperLoaded)
            {
                TryOpen_DzhakeHelperLoaded(player, fol);
            }
            else
            {
                TryOpen_DzhakeHelperUnloaded(player, fol);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual void TryOpen_DzhakeHelperLoaded(Player player, Follower fol)
        {
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(player.Center, Center))
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
                Add(new Coroutine(UnlockRoutine(fol)));
            }
            Collidable = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual void TryOpen_DzhakeHelperUnloaded(Player player, Follower fol)
        {
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(player.Center, Center))
            {
                opening = true;
                if (fol.Entity is Key key)
                {
                    key.StartedUsing = true;
                }
                Add(new Coroutine(UnlockRoutine(fol)));
            }
            Collidable = true;
        }

        #endregion
        #region UnlockRoutine

        protected IEnumerator UnlockRoutine(Follower fol)
        {
            if (MoreLockBlocksModule.Instance.DzhakeHelperLoaded)
            {
                yield return new SwapImmediately(UnlockRoutine_DzhakeHelperLoaded(fol));
            }
            else
            {
                yield return new SwapImmediately(UnlockRoutine_DzhakeHelperUnloaded(fol));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual IEnumerator UnlockRoutine_DzhakeHelperLoaded(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(unlockSfxName, this);
            emitter.Source.DisposeOnTransition = true;
            Level level = SceneAs<Level>();

            Key key = fol.Entity as Key;
            CustomKey key2 = fol.Entity as CustomKey;
            if (key is not null)
            {
                Add(new Coroutine(key.UseRoutine(Center + new Vector2(0f, 2f))));
            }
            else if (key2 is not null)
            {
                Add(new Coroutine(key2.UseRoutine(Center + new Vector2(0f, 2f))));
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

            Tag |= Tags.TransitionUpdate;
            Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return Sprite.PlayRoutine("burst");

            RemoveSelf();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual IEnumerator UnlockRoutine_DzhakeHelperUnloaded(Follower fol)
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
                level.Session.Audio.Apply();
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

        #endregion
    }
}