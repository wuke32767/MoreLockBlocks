using Celeste.Mod.DzhakeHelper;
using Celeste.Mod.DzhakeHelper.Entities;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    [CustomEntity("MoreLockBlocks/DreamLockBlock")]
    [TrackedAs(typeof(DreamBlock))]
    internal class DreamLockBlockV2 : DreamBlock
    {
        readonly bool ignoreInventory;
        BaseLockBlockComponent component;
        bool unlocked;
        public DreamLockBlockV2(EntityData data, Vector2 offset, EntityID id)
            : base(data.Position + offset, 32, 32, null, false, false, data.Bool("below", false))
        //    : base(data, offset, id, defaultUnlockSfx: MoreLockBlocksSFX.game_lockblocks_dreamlockblock_key_unlock)
        {
            Add(component = new BaseLockBlockComponent(this, data, offset, id, defaultUnlockSfx: MoreLockBlocksSFX.game_lockblocks_dreamlockblock_key_unlock));
            SurfaceSoundIndex = 11;
            ignoreInventory = data.Bool("ignoreInventory", false);
            if (MoreLockBlocksModule.Instance.DzhakeHelperLoaded)
            {
                component.UnlockRoutine = UnlockRoutine_DzhakeHelperLoaded;
            }
            else
            {
                component.UnlockRoutine = UnlockRoutine_DzhakeHelperUnloaded;
            }
        }
        public override void Added(Scene scene)
        {
            if (MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(component.ID))
            {
                unlocked = true;
                component.Remove();
            }
            base.Added(scene);
        }
        public override void Render()
        {
            base.Render();
            //:sobeline:
            Entity_Render(this);
        }
        [MonoModLinkTo("Monocle.Entity", "System.Void Render()")]
        private static void Entity_Render(Entity self)
        {
            throw new NotImplementedException();
        }

        private const float chargeUpDuration = 6f, unlockDuration = 0.25f, chargeDownDuration = 0.1f;

        public IEnumerator DummyUnlockRoutine()
        {
            if (!Activated)
            {
                yield break;
            }
            Level level = SceneAs<Level>();
            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            Add(shaker = new Shaker(true, delegate (Vector2 s)
            {
                shake = s;
            }));
            shaker.Interval = 0.02f;

            for (float percent = 0f; percent < 1f; percent += Engine.DeltaTime / chargeUpDuration)
            {
                whiteFill = Ease.CubeIn(percent);
                yield return null;
            }
            UpdateNoRoutine(); // in some cases, this will not be called.
            shaker.On = false; // so better to close it manually.

            whiteHeight = 1f;
            whiteFill = 1f;
            for (float percent = 1f; percent > 0f; percent -= Engine.DeltaTime / unlockDuration)
            {
                whiteHeight = percent;
                Glitch.Value = percent * 0.2f;
                if (level.OnInterval(0.1f))
                {
                    for (int i = 0; i < Width; i += 4)
                    {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(X + i, Y + Height * whiteHeight + 1f));
                    }
                }
                if (level.OnInterval(0.1f))
                {
                    level.Shake();
                }
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                yield return null;
            }
            whiteHeight = Glitch.Value = 0f;

            while (whiteFill > 0f)
            {
                whiteFill -= Engine.DeltaTime / chargeDownDuration;
                yield return null;
            }
        }

        IEnumerator UnlockRoutine_DzhakeHelperLoaded(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(component.unlockSfxName, this);
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

            component.UnlockingRegistered = true;
            if (component.stepMusicProgress)
            {
                level.Session.Audio.Music.Progress++;
                level.Session.Audio.Apply();
            }
            MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Add(component.ID);
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
            //Collidable = false;
            unlocked = true;
            emitter.Source.DisposeOnTransition = false;
            Add(new Coroutine(DummyUnlockRoutine()));
            SurfaceSoundIndex = 12;
            yield return component.Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return component.Sprite.PlayRoutine("burst");

        }

        IEnumerator UnlockRoutine_DzhakeHelperUnloaded(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(component.unlockSfxName, this);
            emitter.Source.DisposeOnTransition = true;
            Level level = SceneAs<Level>();

            Key key = fol.Entity as Key;
            Add(new Coroutine(key.UseRoutine(Center + new Vector2(0f, 2f))));
            yield return 1.2f;

            component.UnlockingRegistered = true;
            if (component.stepMusicProgress)
            {
                level.Session.Audio.Music.Progress++;
                level.Session.Audio.Apply();
            }
            MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Add(component.ID);
            key.RegisterUsed();
            while (key.Turning)
            {
                yield return null;
            }

            Tag |= Tags.TransitionUpdate;
            //Collidable = false;
            unlocked = true;
            emitter.Source.DisposeOnTransition = false;
            Add(new Coroutine(DummyUnlockRoutine()));
            SurfaceSoundIndex = 12;
            yield return component.Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return component.Sprite.PlayRoutine("burst");
        }
        static Hook patch;
        internal static void Load()
        {
            patch = new Hook(typeof(DreamBlock).GetProperty(nameof(DreamBlock.Activated)).GetMethod, static (Func<DreamBlock, bool> orig, DreamBlock self) =>
            {
                bool o = orig(self);
                if (self is DreamLockBlockV2 v2)
                {
                    if (!v2.unlocked)
                    {
                        return false;
                    }
                    if (v2.ignoreInventory)
                    {
                        return v2.unlocked;
                    }
                }
                return o;
            });

        }

        internal static void Unload()
        {
            patch?.Dispose();
        }
    }
}




