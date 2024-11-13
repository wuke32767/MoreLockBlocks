using Monocle;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable
namespace Celeste.Mod.MoreLockBlocks.Imports
{
    public static class ReverseHelperCall
    {
        /// <summary>
        /// if a entity acts like DreamBlock but is not DreamBlock, use this. 
        /// it should access Inventory.DreamDash only when awaking, and save it to a field.
        /// then, it controls visuals with that field and [De]ActivateNoRoutine. 
        /// for example, Communal Helper Dream Tunnel.
        /// 
        /// anyway, better to ask me for it.
        /// </summary>
        public static void RegisterDreamBlockLike(Type targetType, Action<Entity> ActivateNoRoutine, Action<Entity> DeactivateNoRoutine)
        {
            ReverseHelper.RegisterDreamBlockLike?.Invoke(targetType, ActivateNoRoutine, DeactivateNoRoutine);
        }

        /// <summary> 
        /// or, use this to check if your entity is enabled.
        /// notice that because of awake order, your entity might awake before reversed.
        /// better to awake at the end of awake frame.
        /// </summary>
        /// <param name="e">the entity to be checked.</param>
        /// <param name="fallback">if ReverseHelper is not loaded, use fallback instead.</param>
        public static bool PlayerHasDreamDash(Entity e, Func<bool>? fallback = null)
        {
            if (ReverseHelper.PlayerHasDreamDash is null)
            {
                return fallback?.Invoke() ?? (Engine.Scene as Level)?.Session?.Inventory.DreamDash ?? false;
            }
            return ReverseHelper.PlayerHasDreamDash(e);
        }

        /// <summary>
        /// generic version of these options.
        /// https://github.com/wuke32767/CelesteReverseHelper/blob/a4919894497bc501be7f9f8f5c08923a1187af1c/Src/Entities/DreamBlock/DreamBlockConfigurer.cs#L15
        /// those option name and value will not be changed (wip excluded), so you can just hardcode them.
        /// getter.
        /// </summary>
        /// <returns>
        /// if this flag is set.
        /// null: not set / not loaded.
        /// not null: get flag.
        /// </returns>
        public static bool? ConfigureGetFromEnum(Entity e, long i)
        {
            return ReverseHelper.ConfigureGetFromEnum?.Invoke(e, i);
        }

        /// <summary>
        /// generic version of these option.
        /// setter.
        /// </summary>
        /// notice that it can clear a flag.
        public static void ConfigureSetFromEnum(Entity e, long i, bool? value)
        {
            ReverseHelper.ConfigureSetFromEnum?.Invoke(e, i, value);
        }

        /// <summary>
        /// get option from string.
        /// </summary>
        /// <returns> one of Enum.GetValues<DreamBlockConfigFlags>() </returns>
        /// (param) one of Enum.GetNames<DreamBlockConfigFlags>()
        public static long ConfigureGetEnum(string s)
        {
            return ReverseHelper.ConfigureGetEnum?.Invoke(s) ?? 0;
        }

        /// <summary>
        /// returns trackers for these flags.
        /// </summary>
        /// <returns>
        /// index it with the enum and you would get all dreamblock that has the flag.
        /// sometimes it contains Dream Tunnel [Communal Helper], which is not DreamBlock. 
        /// </returns>
        public static ImmutableArray<List<Entity>>? GetDreamBlockTrackers(Scene scene)
        {
            return ReverseHelper.GetDreamBlockTrackers?.Invoke(scene);
        }
    }

    [ModImportName("ReverseHelper.DreamBlock")]
    public static class ReverseHelper
    {
        public static Action<Type, Action<Entity>, Action<Entity>>? RegisterDreamBlockLike;
        public static Func<Entity, bool>? PlayerHasDreamDash;
        public static Func<Entity, long, bool?>? ConfigureGetFromEnum;
        public static Action<Entity, long, bool?>? ConfigureSetFromEnum;
        public static Func<string, long>? ConfigureGetEnum;
        public static Func<Scene, ImmutableArray<List<Entity>>>? GetDreamBlockTrackers;
    }
}
