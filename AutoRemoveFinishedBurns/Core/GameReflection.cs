using System.Reflection;
using Brutal.ImGuiApi;
using Brutal.Logging;
using HarmonyLib;
using KSA;

namespace AutoRemoveFinishedBurns.Core;

static class GameReflection
{
    #region Detection

    public static readonly MethodInfo? Vehicle_UpdateFromTaskResults =
        AccessTools.Method(typeof(Vehicle), nameof(Vehicle.UpdateFromTaskResults),
            new[]
            {
                typeof(SimStep),
                typeof(VehicleUpdateData).MakeByRefType(),
                typeof(ReadOnlySpan<Vehicle>),
            });

    #endregion

    #region Settings

    public static readonly MethodInfo? GameSettings_OnDrawUi =
        AccessTools.Method(typeof(GameSettings), nameof(GameSettings.OnDrawUi),
            new[] { typeof(Camera) });

    public static readonly MethodInfo? ImGui_EndTabBar =
        AccessTools.Method(typeof(ImGui), nameof(ImGui.EndTabBar), Type.EmptyTypes);

    public static readonly MethodInfo? ImGuiHelper_EndRegionTab =
        AccessTools.Method(typeof(ImGuiHelper), nameof(ImGuiHelper.EndRegionTab),
            new[] { typeof(bool) });

    #endregion

    #region Validation

    public static bool ValidateDetection()
    {
        var targets = new (string name, object? target)[]
        {
            ("Vehicle.UpdateFromTaskResults(SimStep, ref readonly VehicleUpdateData, ReadOnlySpan<Vehicle>)",
                Vehicle_UpdateFromTaskResults),
        };
        return ValidateTargets("Detection", targets);
    }

    public static bool ValidateSettings()
    {
        var targets = new (string name, object? target)[]
        {
            ("GameSettings.OnDrawUi(Camera)", GameSettings_OnDrawUi),
            ("ImGui.EndTabBar()", ImGui_EndTabBar),
            ("ImGuiHelper.EndRegionTab(bool)", ImGuiHelper_EndRegionTab),
        };
        return ValidateTargets("Settings", targets);
    }

    private static bool ValidateTargets(string feature, (string name, object? target)[] targets)
    {
        bool allOk = true;
        foreach (var (name, target) in targets)
        {
            if (target == null)
            {
                DefaultCategory.Log.Error(
                    $"[AutoRemoveFinishedBurns] {feature}: {name} not found - game version may have changed.");
                allOk = false;
            }
        }
        return allOk;
    }

    #endregion
}
