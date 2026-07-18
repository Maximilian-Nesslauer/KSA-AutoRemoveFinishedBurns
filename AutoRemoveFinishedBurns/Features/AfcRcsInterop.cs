using System.Reflection;
using AutoRemoveFinishedBurns.Core;
using Brutal.Logging;
using HarmonyLib;
using KSA;

namespace AutoRemoveFinishedBurns.Features;

// Soft dependency on AdvancedFlightComputer's RCS translation executor.
// Its burns complete without the stock Auto -> Manual transition
// BurnRemovalPatch watches (BurnMode stays Manual for the whole RCS run),
// so AFC raises a public completion event and this class applies the same
// removal policy to it. Bound by name via reflection: AFC absent, or its
// event renamed or reshaped, leaves the interop off with the mod otherwise
// untouched. The event signature uses only KSA types on purpose, so no
// AFC reference is needed to build the delegate.
static class AfcRcsInterop
{
    private const string CompletionsTypeName =
        "AdvancedFlightComputer.Features.RcsTranslation.RcsBurnCompletions";
    private const string EventName = "Completed";

    private static EventInfo? _event;
    private static Delegate? _handler;

    internal static bool Active => _handler != null;

    // True when the subscription is in place, false when AFC is not loaded.
    // A present-but-mismatched event logs once and returns false (that is
    // drift, not absence).
    public static bool TryEnable()
    {
        if (_handler != null)
            return true;
        Type? type = AccessTools.TypeByName(CompletionsTypeName);
        if (type == null)
            return false;
        EventInfo? evt = type.GetEvent(EventName, BindingFlags.Public | BindingFlags.Static);
        if (evt == null || evt.EventHandlerType != typeof(Action<Vehicle, Burn>))
        {
            LogHelper.ErrorOnce("AfcRcsInterop:signature",
                "[AutoRemoveFinishedBurns] AdvancedFlightComputer's RCS completion event is missing " +
                "or has an unexpected signature; RCS interop disabled.");
            return false;
        }
        Delegate handler = Delegate.CreateDelegate(evt.EventHandlerType,
            typeof(AfcRcsInterop).GetMethod(nameof(OnRcsBurnCompleted),
                BindingFlags.NonPublic | BindingFlags.Static)!);
        evt.AddEventHandler(null, handler);
        _event = evt;
        _handler = handler;
        return true;
    }

    public static void Disable()
    {
        if (_event != null && _handler != null)
        {
            try
            {
                _event.RemoveEventHandler(null, _handler);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorOnce("AfcRcsInterop:disable",
                    $"[AutoRemoveFinishedBurns] Failed to unsubscribe RCS interop: {ex.Message}");
            }
        }
        _event = null;
        _handler = null;
    }

    // Same policy as BurnRemovalPatch: only while enabled, only for the
    // controlled vehicle, and only while the burn is still in the plan.
    // AFC raises from its main-thread per-tick driver, the same context the
    // removal patch runs in - the live BurnPlan list has no locking, so a
    // raise from any other thread would be an AFC contract break.
    // TryGetBurn and RemoveBurn match by Burn value equality (Time +
    // DeltaVVlf), not reference; for the burn AFC just completed they are
    // the same entry.
    internal static void OnRcsBurnCompleted(Vehicle vehicle, Burn burn)
    {
        try
        {
            if (!Config.Enabled)
                return;
            if (Program.ControlledVehicle != vehicle)
                return;
            FlightComputer fc = vehicle.FlightComputer;
            if (!fc.BurnPlan.TryGetBurn(burn))
                return;

            if (DebugConfig.Detection)
                DefaultCategory.Log.Debug(
                    $"[AutoRemoveFinishedBurns] vehicle='{vehicle.Id}' RCS burn finished " +
                    $"(dv={burn.DeltaVVlf.Length():F2}m/s); removing from plan.");

            fc.RemoveBurn(burn);
        }
        catch (Exception ex)
        {
            LogHelper.ErrorOnce("AfcRcsInterop:" + ex.GetType().Name,
                $"[AutoRemoveFinishedBurns] vehicle='{vehicle?.Id ?? "<null>"}' RCS completion " +
                $"handler threw: {ex}");
        }
    }
}
