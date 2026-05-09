#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.CompilerServices;
using AutoRemoveFinishedBurns.Core;
using Brutal.Logging;
using Brutal.Numerics;
using HarmonyLib;
using KSA;

namespace AutoRemoveFinishedBurns.Features;

[HarmonyPatch(typeof(Vehicle), nameof(Vehicle.UpdateFromTaskResults),
    new[] { typeof(SimStep), typeof(VehicleUpdateData), typeof(ReadOnlySpan<Vehicle>) },
    new[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
static class BurnRemovalPatch
{
    private sealed class BurnModeBox
    {
        public FlightComputerBurnMode Mode;
    }

    private static readonly ConditionalWeakTable<Vehicle, BurnModeBox> _lastBurnMode = new();

    public static void Reset() => _lastBurnMode.Clear();

    static void Postfix(Vehicle __instance)
    {
#if DEBUG
        long perfStart = DebugConfig.Performance ? Stopwatch.GetTimestamp() : 0;
#endif
        try
        {
            if (Program.ControlledVehicle != __instance) return;

            FlightComputer fc = __instance.FlightComputer;
            FlightComputerBurnMode currentMode = fc.BurnMode;

            bool hadPrevious = _lastBurnMode.TryGetValue(__instance, out BurnModeBox? box);
            FlightComputerBurnMode previousMode = hadPrevious ? box!.Mode : default;

            if (hadPrevious)
                box!.Mode = currentMode;
            else
                _lastBurnMode.Add(__instance, new BurnModeBox { Mode = currentMode });

            if (!Config.Enabled) return;
            if (!hadPrevious) return;
            if (previousMode != FlightComputerBurnMode.Auto) return;
            if (currentMode != FlightComputerBurnMode.Manual) return;

            // Out-of-fuel also flips Auto -> Manual but leaves DeltaVToGoCci
            // pointing the same way as DeltaVTargetCci (dot > 0). Reversal
            // (dot <= 0) is the discriminator for actual completion. This
            // mirrors stock FlightComputer.UpdateBurnTarget which sets
            // BurnMode = Manual on the same dot-product check.
            BurnTarget? burn = fc.Burn;
            if (burn == null) return;
            if (float3.Dot(burn.DeltaVToGoCci, burn.DeltaVTargetCci) > 0f) return;
            if (!fc.BurnPlan.HasActiveBurns) return;

            if (DebugConfig.Detection)
            {
                float dvToGo = burn.DeltaVToGoCci.Length();
                float dvTarget = burn.DeltaVTargetCci.Length();
                DefaultCategory.Log.Debug(
                    $"[AutoRemoveFinishedBurns] vehicle='{__instance.Id}' " +
                    $"auto-burn finished (dvToGo={dvToGo:F2}m/s, dvTarget={dvTarget:F2}m/s); " +
                    "removing from plan.");
            }

            fc.RemoveBurnAt(0);
        }
        catch (Exception ex)
        {
            LogHelper.ErrorOnce("Postfix:" + ex.GetType().Name,
                $"[AutoRemoveFinishedBurns] vehicle='{__instance?.Id ?? "<null>"}' " +
                $"postfix threw: {ex}");
        }
#if DEBUG
        if (DebugConfig.Performance)
            PerfTracker.Record("BurnRemovalPatch.Postfix",
                Stopwatch.GetTimestamp() - perfStart);
#endif
    }
}
