using AutoRemoveFinishedBurns.Core;
using AutoRemoveFinishedBurns.Features;
using Brutal.Logging;
using HarmonyLib;
using KSA;
using StarMap.API;

namespace AutoRemoveFinishedBurns;

[StarMapMod]
public sealed class Mod
{
    private static Harmony? _harmony;

    private const string TestedGameVersion = "v2026.5.6.4337";

    [StarMapAllModsLoaded]
    public void OnFullyLoaded()
    {
        string gameVersion = VersionInfo.Current.VersionString;
        DefaultCategory.Log.Info($"[AutoRemoveFinishedBurns] Game version: {gameVersion}");
        if (gameVersion != TestedGameVersion)
            DefaultCategory.Log.Warning(
                $"[AutoRemoveFinishedBurns] Tested against {TestedGameVersion}, current is {gameVersion}. " +
                "Some features may not work correctly.");

        Config.Init();

        _harmony = new Harmony("com.maxi.autoremovefinishedburns");

        if (GameReflection.ValidateDetection())
            BurnRemovalPatch.ApplyPatches(_harmony);
        else
            DefaultCategory.Log.Warning(
                "[AutoRemoveFinishedBurns] Detection disabled - reflection targets not found.");

        if (GameReflection.ValidateSettings())
            SettingsTabPatch.ApplyPatches(_harmony);
        else
            DefaultCategory.Log.Warning(
                "[AutoRemoveFinishedBurns] Settings tab disabled - reflection targets not found.");

        DefaultCategory.Log.Info("[AutoRemoveFinishedBurns] Loaded.");
    }

    [StarMapUnload]
    public void Unload()
    {
        _harmony?.UnpatchAll(_harmony.Id);
        _harmony = null;
        BurnRemovalPatch.Reset();
        Config.Reset();
        LogHelper.Reset();
#if DEBUG
        PerfTracker.Reset();
#endif
        DefaultCategory.Log.Info("[AutoRemoveFinishedBurns] Unloaded.");
    }
}
