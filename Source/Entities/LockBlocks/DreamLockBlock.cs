using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;
using System.Collections;
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;
using Celeste.Mod.DzhakeHelper;
using Celeste.Mod.DzhakeHelper.Entities;
using System.Runtime.CompilerServices;
using System;
using MonoMod.Utils;

namespace Celeste.Mod.MoreLockBlocks.Entities
{
    [Tracked]
    [CustomEntity("MoreLockBlocks/DreamLockBlock")]
    public class DreamLockBlock : BaseLockBlock
    {
        [TrackedAs(typeof(DreamBlock))]
        internal class DreamBlockDummy : DreamBlock
        {
            private readonly DreamLockBlock parent;

            private const float chargeUpDuration = 0.6f, unlockDuration = 0.25f, chargeDownDuration = 0.1f;

            public DreamBlockDummy(Vector2 position, DreamLockBlock parent, bool below) : base(position, 32, 32, null, false, false, below)
            {
                this.parent = parent;
            }

            public IEnumerator DummyUnlockRoutine()
            {
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
                // 
                shaker.On = false;
                playerHasDreamDash = true;
                Setup();
                Remove(occlude);

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

            #region DreamBlockDummy Hooks

            public static void Load()
            {
                IL.Celeste.DreamBlock.Added += DreamBlock_Added;
                On.Celeste.DreamBlock.Activate += DreamBlock_Activate;
                On.Celeste.DreamBlock.FastActivate += DreamBlock_FastActivate;
                On.Celeste.DreamBlock.ActivateNoRoutine += DreamBlock_ActivateNoRoutine;
                On.Celeste.DreamBlock.Deactivate += DreamBlock_Deactivate;
                On.Celeste.DreamBlock.FastDeactivate += DreamBlock_FastDeactivate;
                On.Celeste.DreamBlock.DeactivateNoRoutine += DreamBlock_DeactivateNoRoutine;
            }

            public static void Unload()
            {
                IL.Celeste.DreamBlock.Added -= DreamBlock_Added;
                On.Celeste.DreamBlock.Activate -= DreamBlock_Activate;
                On.Celeste.DreamBlock.FastActivate -= DreamBlock_FastActivate;
                On.Celeste.DreamBlock.ActivateNoRoutine -= DreamBlock_ActivateNoRoutine;
                On.Celeste.DreamBlock.Deactivate -= DreamBlock_Deactivate;
                On.Celeste.DreamBlock.FastDeactivate -= DreamBlock_FastDeactivate;
                On.Celeste.DreamBlock.DeactivateNoRoutine -= DreamBlock_DeactivateNoRoutine;
            }

            private static void DreamBlock_Added(ILContext il)
            {
                ILCursor cursor = new(il);

                cursor.GotoNext(MoveType.Before, instr => instr.MatchStfld(typeof(DreamBlock).GetField("playerHasDreamDash", BindingFlags.Instance | BindingFlags.NonPublic)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(DetermineDreamBlockActive);
            }

            private static bool DetermineDreamBlockActive(bool orig, DreamBlock self)
            {
                if (self is DreamBlockDummy dummy)
                {
                    return MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(dummy.parent.ID);
                }
                else
                {
                    return orig;
                }
            }

            private static IEnumerator DreamBlock_Activate(On.Celeste.DreamBlock.orig_Activate orig, DreamBlock self) => DoNothingIfDummy((self) => orig(self), self);
            private static IEnumerator DreamBlock_FastActivate(On.Celeste.DreamBlock.orig_FastActivate orig, DreamBlock self) => DoNothingIfDummy((self) => orig(self), self);
            private static void DreamBlock_ActivateNoRoutine(On.Celeste.DreamBlock.orig_ActivateNoRoutine orig, DreamBlock self) => DoNothingIfDummy((self) => orig(self), self);

            private static IEnumerator DreamBlock_Deactivate(On.Celeste.DreamBlock.orig_Deactivate orig, DreamBlock self) => DoNothingIfDummy((self) => orig(self), self);
            private static IEnumerator DreamBlock_FastDeactivate(On.Celeste.DreamBlock.orig_FastDeactivate orig, DreamBlock self) => DoNothingIfDummy((self) => orig(self), self);
            private static void DreamBlock_DeactivateNoRoutine(On.Celeste.DreamBlock.orig_DeactivateNoRoutine orig, DreamBlock self) => DoNothingIfDummy((self) => orig(self), self);

            private static void DoNothingIfDummy(Action<DreamBlock> orig, DreamBlock self)
            {
                if (self is DreamBlockDummy dummy && !MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(dummy.parent.ID))
                    return;
                else
                    orig(self);
            }

            private static IEnumerator DoNothingIfDummy(Func<DreamBlock, IEnumerator> orig, DreamBlock self)
            {
                if (self is DreamBlockDummy dummy && !MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(dummy.parent.ID))
                    yield break;
                else
                    yield return new SwapImmediately(orig(self));
            }

            #endregion
        }

        private DreamBlockDummy dummy;

        private readonly bool dummyBelow;

        public DreamLockBlock(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id, defaultUnlockSfx: MoreLockBlocksSFX.game_lockblocks_dreamlockblock_key_unlock)
        {
            SurfaceSoundIndex = 11;
            dummyBelow = data.Bool("below", false);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Scene.Add(dummy = new DreamBlockDummy(Position, this, dummyBelow));
            Depth = dummy.Depth - 1;
            if (MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(ID))
            {
                RemoveSelf();
            }
        }

        #region TryOpen

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void TryOpen_DzhakeHelperLoaded(Player player, Follower fol)
        {
            Collidable = dummy.Collidable = false;
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
            Collidable = dummy.Collidable = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void TryOpen_DzhakeHelperUnloaded(Player player, Follower fol)
        {
            Collidable = dummy.Collidable = false;
            if (!Scene.CollideCheck<Solid>(player.Center, Center))
            {
                opening = true;
                if (fol.Entity is Key key)
                {
                    key.StartedUsing = true;
                }
                Add(new Coroutine(UnlockRoutine(fol)));
            }
            Collidable = dummy.Collidable = true;
        }

        #endregion
        #region UnlockRoutine

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override IEnumerator UnlockRoutine_DzhakeHelperLoaded(Follower fol)
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
            MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Add(ID);
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
            dummy.Add(new Coroutine(dummy.DummyUnlockRoutine()));
            SurfaceSoundIndex = 12;
            yield return Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return Sprite.PlayRoutine("burst");

            RemoveSelf();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override IEnumerator UnlockRoutine_DzhakeHelperUnloaded(Follower fol)
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
            MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Add(ID);
            key.RegisterUsed();
            while (key.Turning)
            {
                yield return null;
            }

            Tag |= Tags.TransitionUpdate;
            Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            dummy.Add(new Coroutine(dummy.DummyUnlockRoutine()));
            SurfaceSoundIndex = 12;
            yield return Sprite.PlayRoutine("open");

            level.Shake();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return Sprite.PlayRoutine("burst");

            RemoveSelf();
        }

        #endregion
    }
}