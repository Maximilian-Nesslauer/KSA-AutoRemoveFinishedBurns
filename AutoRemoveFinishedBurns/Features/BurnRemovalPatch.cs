using System;
using System.Collections.Generic;
using AutoRemoveFinishedBurns.Core;
using Brutal.Logging;
using Brutal.Numerics;
using HarmonyLib;
using KSA;

namespace AutoRemoveFinishedBurns.Features;

static class BurnRemovalPatch
{
    public static void ApplyPatches(Harmony harmony)
    {
        harmony.CreateClassProcessor(typeof(Patch_BurnRemoval)).Patch();

        if (DebugConfig.Detection)
            DefaultCategory.Log.Debug("[AutoRemoveFinishedBurns] Detection patch applied.");
    }

    public static void Reset() => Patch_BurnRemoval.Reset();
}

[HarmonyPatch(typeof(Vehicle), "UpdateFromTaskResults")]
static class Patch_BurnRemoval
{
    // After the first UpdateFromTaskResults swap, vehicle.FlightComputer and
    // the worker's NewFlightComputer alias the same object; the worker mutates
    // BurnMode in place, so a Prefix here can't observe the pre-tick value.
    // We track the last mode seen per FlightComputer and detect the
    // Auto -> Manual transition across ticks instead.
    private static readonly Dictionary<FlightComputer, FlightComputerBurnMode> _lastBurnMode = new();

    public static void Reset() => _lastBurnMode.Clear();

    static void Postfix(Vehicle __instance)
    {
        try
        {
            FlightComputer fc = __instance.FlightComputer;

            bool hadPrevious = _lastBurnMode.TryGetValue(fc, out FlightComputerBurnMode previousMode);
            FlightComputerBurnMode currentMode = fc.BurnMode;
            _lastBurnMode[fc] = currentMode;

            if (!Config.Enabled) return;
            if (!hadPrevious) return;
            if (previousMode != FlightComputerBurnMode.Auto) return;
            if (currentMode != FlightComputerBurnMode.Manual) return;

            // Out-of-fuel also flips Auto -> Manual but leaves DeltaVToGo pointing
            // the same way as DeltaVTarget (dot > 0). Reversal (dot <= 0) is the
            // discriminator for actual completion.
            if (fc.Burn == null
                || float3.Dot(fc.Burn.DeltaVToGoCci, fc.Burn.DeltaVTargetCci) > 0f)
                return;

            if (!fc.BurnPlan.HasActiveBurns) return;

            if (DebugConfig.Detection)
                DefaultCategory.Log.Debug(
                    "[AutoRemoveFinishedBurns] Auto-burn finished, removing from plan.");

            fc.RemoveBurnAt(0);
        }
        catch (Exception ex)
        {
            LogHelper.WarnOnce("Postfix:" + ex.GetType().Name,
                $"[AutoRemoveFinishedBurns] Postfix error: {ex}");
        }
    }
}
