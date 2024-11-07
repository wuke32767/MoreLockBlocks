using System;
using Celeste.Mod.MoreLockBlocks.Entities;

namespace Celeste.Mod.MoreLockBlocks;

public class MoreLockBlocksModule : EverestModule
{
    public static MoreLockBlocksModule Instance { get; private set; }

    public override Type SettingsType => typeof(MoreLockBlocksModuleSettings);
    public static MoreLockBlocksModuleSettings Settings => (MoreLockBlocksModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(MoreLockBlocksModuleSession);
    public static MoreLockBlocksModuleSession Session => (MoreLockBlocksModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(MoreLockBlocksModuleSaveData);
    public static MoreLockBlocksModuleSaveData SaveData => (MoreLockBlocksModuleSaveData)Instance._SaveData;

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

    public override void Load()
    {
        // TODO: apply any hooks that should always be active
        GlassLockBlockController.Load();
    }

    public override void Unload()
    {
        // TODO: unapply any hooks applied in Load()
        GlassLockBlockController.Unload();
    }

    public override void LoadContent(bool firstLoad)
    {
        MoreLockBlocksGFX.LoadContent();
    }
}