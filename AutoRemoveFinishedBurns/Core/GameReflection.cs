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
        AccessTools.Method(typeof(Vehicle), "UpdateFromTaskResults");

    #endregion

    #region Settings

    public static readonly MethodInfo? GameSettings_OnDrawUi =
        AccessTools.Method(typeof(GameSettings), nameof(GameSettings.OnDrawUi));

    public static readonly MethodInfo? ImGui_EndTabBar =
        AccessTools.Method(typeof(ImGui), nameof(ImGui.EndTabBar));

    public static readonly MethodInfo? ImGuiHelper_EndRegionTab =
        AccessTools.Method(typeof(ImGuiHelper), nameof(ImGuiHelper.EndRegionTab));

    #endregion

    #region Validation

    public static bool ValidateDetection()
    {
        var targets = new (string name, object? target)[]
        {
            ("Vehicle.UpdateFromTaskResults", Vehicle_UpdateFromTaskResults),
        };
        return ValidateTargets("Detection", targets);
    }

    public static bool ValidateSettings()
    {
        var targets = new (string name, object? target)[]
        {
            ("GameSettings.OnDrawUi", GameSettings_OnDrawUi),
            ("ImGui.EndTabBar", ImGui_EndTabBar),
            ("ImGuiHelper.EndRegionTab", ImGuiHelper_EndRegionTab),
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
