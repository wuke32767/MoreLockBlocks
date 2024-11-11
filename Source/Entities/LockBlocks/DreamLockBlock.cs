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
                Add(shaker = new Shaker(true, delegate (Vector2 t)
                {
                    shake = t;
                }));
                shaker.Interval = 0.02f;
                for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime / chargeUpDuration)
                {
                    whiteFill = Ease.CubeIn(p2);
                    yield return null;
                }
                shaker.On = false;
                ActivateNoRoutine();
                whiteHeight = 1f;
                whiteFill = 1f;
                for (float p2 = 1f; p2 > 0f; p2 -= Engine.DeltaTime / unlockDuration)
                {
                    whiteHeight = p2;
                    Glitch.Value = p2 * 0.2f;
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
            }

            public static void Unload()
            {
                IL.Celeste.DreamBlock.Added -= DreamBlock_Added;
            }

            private static void DreamBlock_Added(ILContext il)
            {
                ILCursor cursor = new(il);

                cursor.GotoNext(MoveType.Before, instr => instr.MatchStfld(typeof(DreamBlock).GetField("playerHasDreamDash", BindingFlags.Instance | BindingFlags.NonPublic)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(DetermineDreamBlockActive);
            }

            private static bool DetermineDreamBlockActive(bool orig, DreamBlock block)
            {
                if (block is DreamBlockDummy dummy)
                {
                    return MoreLockBlocksModule.Session.UnlockedDreamLockBlocks.Contains(dummy.parent.ID);
                }
                else
                {
                    return orig;
                }
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