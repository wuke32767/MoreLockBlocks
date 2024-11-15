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
using MonoMod.RuntimeDetour;
using Celeste.Mod.MoreLockBlocks.Imports;

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

            private bool CanDashThrough
            {
                get
                {
                    if (MoreLockBlocksModule.Session.DreamBlockDummyStates.TryGetValue(parent.ID, out bool value))
                        return value;
                    else
                    {
                        bool state = MoreLockBlocksModule.Session.DreamBlockDummyStates[parent.ID] = false;
                        if (ignoreInventory)
                        {
                            SetReverseHelperDummyState(state);
                        }
                        return state;
                    }
                }

                set
                {
                    MoreLockBlocksModule.Session.DreamBlockDummyStates[parent.ID] = value;
                    if (ignoreInventory)
                    {
                        SetReverseHelperDummyState(value);
                    }
                }
            }

            private void SetReverseHelperDummyState(bool value)
            {
                ReverseHelperCallHelper.ConfigureSetFromEnum(this, /*alwaysEnable =*/ 1 << 1, value);
                ReverseHelperCallHelper.ConfigureSetFromEnum(this, /*alwaysDisable =*/ 1 << 2, !value);
            }

            private bool Unlocked => MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(parent.ID); // whether we can change state

            private readonly bool ignoreInventory;

            private static ILHook ilHook_Player_DashCoroutine;

            public DreamBlockDummy(Vector2 position, DreamLockBlock parent, bool below, bool ignoreInventory) : base(position, 32, 32, null, false, false, below)
            {
                this.parent = parent;
                this.ignoreInventory = ignoreInventory;
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
                ActivateNoRoutine(); // we can do this because the block is already registered as unlocked

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

                if (!MoreLockBlocksModule.Instance.ReverseHelperLoaded)
                {
                    IL.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;

                    ilHook_Player_DashCoroutine = new ILHook(typeof(Player).GetMethod("DashCoroutine", BindingFlags.Instance | BindingFlags.NonPublic).GetStateMachineTarget(), Player_DashCoroutine);
                }
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

                if (!MoreLockBlocksModule.Instance.ReverseHelperLoaded)
                {
                    IL.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;

                    ilHook_Player_DashCoroutine?.Dispose();
                    ilHook_Player_DashCoroutine = null;
                }
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
                    bool canDashThrough = dummy.CanDashThrough;
                    if (dummy.ignoreInventory)
                    {
                        dummy.SetReverseHelperDummyState(canDashThrough);
                    }
                    return canDashThrough;
                }
                else
                    return orig;
            }

            private static IEnumerator DreamBlock_Activate(On.Celeste.DreamBlock.orig_Activate orig, DreamBlock self) => DoNothingIfDummy(() => orig(self), self, true);
            private static IEnumerator DreamBlock_FastActivate(On.Celeste.DreamBlock.orig_FastActivate orig, DreamBlock self) => DoNothingIfDummy(() => orig(self), self, true);
            private static void DreamBlock_ActivateNoRoutine(On.Celeste.DreamBlock.orig_ActivateNoRoutine orig, DreamBlock self) => DoNothingIfDummy(() => orig(self), self, true);

            private static IEnumerator DreamBlock_Deactivate(On.Celeste.DreamBlock.orig_Deactivate orig, DreamBlock self) => DoNothingIfDummy(() => orig(self), self, false);
            private static IEnumerator DreamBlock_FastDeactivate(On.Celeste.DreamBlock.orig_FastDeactivate orig, DreamBlock self) => DoNothingIfDummy(() => orig(self), self, false);
            private static void DreamBlock_DeactivateNoRoutine(On.Celeste.DreamBlock.orig_DeactivateNoRoutine orig, DreamBlock self) => DoNothingIfDummy(() => orig(self), self, false);

            private static void DoNothingIfDummy(Action callOrig, DreamBlock self, bool canDashThrough)
            {
                if (self is DreamBlockDummy dummy)
                {
                    if (dummy.Unlocked)
                    {
                        dummy.CanDashThrough = canDashThrough;
                    }
                    else
                        return;
                }
                callOrig();
            }

            private static IEnumerator DoNothingIfDummy(Func<IEnumerator> callOrig, DreamBlock self, bool canDashThrough)
            {
                if (self is DreamBlockDummy dummy)
                {
                    if (dummy.Unlocked)
                    {
                        dummy.CanDashThrough = canDashThrough;
                    }
                    else
                        yield break;
                }
                yield return new SwapImmediately(callOrig());
            }

            private static void Player_DreamDashCheck(ILContext il)
            {
                ILCursor cursor = new(il);

                cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld(typeof(PlayerInventory).GetField("DreamDash", BindingFlags.Instance | BindingFlags.Public)));
                cursor.GotoNext(MoveType.Before, instr => instr.MatchBrfalse(out ILLabel _));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(DetermineInventoryCheckOverride);
                cursor.Emit(OpCodes.Or);
            }

            private static void Player_DashCoroutine(ILContext il)
            {
                ILCursor cursor = new(il);

                cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld(typeof(PlayerInventory).GetField("DreamDash", BindingFlags.Instance | BindingFlags.Public)));
                cursor.GotoNext(MoveType.Before, instr => instr.MatchBrfalse(out ILLabel _));
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitCall(typeof(Vector2).GetProperty("UnitY", BindingFlags.Static | BindingFlags.Public).GetGetMethod());
                cursor.EmitDelegate(DetermineInventoryCheckOverride);
                cursor.Emit(OpCodes.Or);
            }

            private static bool DetermineInventoryCheckOverride(Player player, Vector2 dir)
            {
                return player.CollideFirst<DreamBlock>(player.Position + dir) is DreamBlockDummy dummy && dummy.CanDashThrough && dummy.ignoreInventory;
            }

            #endregion
        }

        private DreamBlockDummy dummy;
        private readonly bool dummyBelow;
        private readonly bool dummyIgnoreInventory;

        public DreamLockBlock(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id, defaultUnlockSfx: MoreLockBlocksSFX.game_lockblocks_dreamlockblock_key_unlock)
        {
            SurfaceSoundIndex = 11;
            dummyBelow = data.Bool("below", false);
            dummyIgnoreInventory = data.Bool("ignoreInventory", false);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Scene.Add(dummy = new DreamBlockDummy(Position, this, dummyBelow, dummyIgnoreInventory));
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