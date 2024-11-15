using System;
using System.Reflection;
using Celeste.Mod.MoreLockBlocks.Entities;
using Celeste.Mod.MoreLockBlocks.Imports;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.MoreLockBlocks;

public class MoreLockBlocksModule : EverestModule
{
    public static readonly bool PatchLoaded = typeof(DreamBlock).GetField("DreamBlockPatch", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is not null;

    public static MoreLockBlocksModule Instance { get; private set; }

    public override Type SettingsType => typeof(MoreLockBlocksModuleSettings);
    public static MoreLockBlocksModuleSettings Settings => (MoreLockBlocksModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(MoreLockBlocksModuleSession);
    public static MoreLockBlocksModuleSession Session => (MoreLockBlocksModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(MoreLockBlocksModuleSaveData);
    public static MoreLockBlocksModuleSaveData SaveData => (MoreLockBlocksModuleSaveData)Instance._SaveData;

    private static readonly FieldInfo Everest__ContentLoaded = typeof(Everest).GetField("_ContentLoaded", BindingFlags.NonPublic | BindingFlags.Static);
    private static Hook hook_Everest_Register = null;

    internal bool DzhakeHelperLoaded;
    internal bool ReverseHelperLoaded;

    public MoreLockBlocksModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(MoreLockBlocksModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(MoreLockBlocksModule), LogLevel.Info);
#endif
    }

    private void HookMods()
    {
        if (!DzhakeHelperLoaded && Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "DzhakeHelper", Version = new Version(1, 4, 9) }))
            LoadDzhakeHelper();

        if (!ReverseHelperLoaded && Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "ReverseHelper", Version = new Version(1, 15, 0) }))
            LoadReverseHelper();
    }

    public override void Load()
    {
        GlassLockBlockController.Load();
        DreamLockBlock.DreamBlockDummy.Load();

        typeof(ReverseHelper).ModInterop();

        hook_Everest_Register = new Hook(typeof(Everest).GetMethod("Register"), typeof(MoreLockBlocksModule).GetMethod("Everest_Register", BindingFlags.NonPublic | BindingFlags.Instance), this);
    }

    public override void Unload()
    {
        GlassLockBlockController.Unload();
        DreamLockBlock.DreamBlockDummy.Unload();

        hook_Everest_Register?.Dispose();
        hook_Everest_Register = null;

        if (DzhakeHelperLoaded)
            UnloadDzhakeHelper();

        if (ReverseHelperLoaded)
            UnloadReverseHelper();
    }

    public override void LoadContent(bool firstLoad)
    {
        MoreLockBlocksGFX.LoadContent();

        HookMods();
    }

    private void LoadDzhakeHelper() => DzhakeHelperLoaded = true;
    private void UnloadDzhakeHelper() => DzhakeHelperLoaded = false;

    private void LoadReverseHelper() => ReverseHelperLoaded = true;
    private void UnloadReverseHelper() => ReverseHelperLoaded = false;

    private void Everest_Register(Action<EverestModule> orig, EverestModule module)
    {
        orig(module);

        if ((bool)Everest__ContentLoaded.GetValue(null))
        {
            // the game was already initialized and a new mod was loaded at runtime:
            // make sure we applied all mod hooks we want to apply.
            HookMods();
        }
    }
}