using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AutoRemoveFinishedBurns.Core;
using Brutal.ImGuiApi;
using Brutal.Logging;
using HarmonyLib;
using KSA;

namespace AutoRemoveFinishedBurns.Features;

/// <summary>
/// Inserts our drawer call before the Mods-tab close in GameSettings.OnDrawUi.
/// Insertion (rather than replacing the close call) lets several mods stack
/// their drawers at the same site without conflicting; we also accept a
/// previous mod's wrapper as the close anchor.
/// </summary>
static class SettingsTabPatch
{
    public static void ApplyPatches(Harmony harmony)
    {
        harmony.CreateClassProcessor(typeof(SettingsTabPatch)).Patch();

        if (DebugConfig.Settings)
            DefaultCategory.Log.Debug("[AutoRemoveFinishedBurns] Settings tab patch applied.");
    }

    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.OnDrawUi))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        MethodInfo endTabBar = AccessTools.Method(typeof(ImGui), nameof(ImGui.EndTabBar));
        MethodInfo endRegionTab = AccessTools.Method(typeof(ImGuiHelper), nameof(ImGuiHelper.EndRegionTab));
        MethodInfo drawerCall = AccessTools.Method(typeof(SettingsTabPatch), nameof(DrawBeforeEndRegionTab));

        int endTabBarIdx = -1;
        for (int i = codes.Count - 1; i >= 0; i--)
        {
            if (codes[i].Calls(endTabBar))
            {
                endTabBarIdx = i;
                break;
            }
        }

        if (endTabBarIdx < 0)
        {
            DefaultCategory.Log.Warning(
                "[AutoRemoveFinishedBurns] Transpiler: EndTabBar not found in GameSettings.OnDrawUi");
            return codes;
        }

        int anchorIdx = -1;
        for (int i = endTabBarIdx - 1; i >= 0; i--)
        {
            CodeInstruction code = codes[i];
            if (code.Calls(endRegionTab))
            {
                anchorIdx = i;
                break;
            }
            if ((code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt)
                && code.operand is MethodInfo mi
                && IsModsTabCloseWrapper(mi))
            {
                anchorIdx = i;
                break;
            }
        }

        if (anchorIdx < 0)
        {
            DefaultCategory.Log.Warning(
                "[AutoRemoveFinishedBurns] Transpiler: Mods-tab close call not found before EndTabBar");
            return codes;
        }

        // Parameterless call doesn't disturb the bool already on the stack
        // for the close. Labels stay on the close so jumps targeting it skip
        // our drawer.
        codes.Insert(anchorIdx, new CodeInstruction(OpCodes.Call, drawerCall));

        return codes;
    }

    // Convention-based loose-name match: any mod that wraps the close call should
    // expose a static void(bool) method named "EndModsTabWithSettings" so other
    // mods can find it as an anchor. AutoStage uses this name today.
    private static bool IsModsTabCloseWrapper(MethodInfo mi)
    {
        if (mi.Name != "EndModsTabWithSettings") return false;
        if (!mi.IsStatic) return false;
        if (mi.ReturnType != typeof(void)) return false;
        ParameterInfo[] parameters = mi.GetParameters();
        return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
    }

    public static void DrawBeforeEndRegionTab()
    {
        // Reset the 2-column mod-list layout. We're still inside the
        // BeginRegionTab child window.
        ImGui.Columns();

        try
        {
            if (ImGui.CollapsingHeader("Auto Remove Finished Burns Settings"u8,
                ImGuiTreeNodeFlags.DefaultOpen))
                DrawSettings();
        }
        catch (Exception ex)
        {
            LogHelper.ErrorOnce("Settings.Draw",
                $"[AutoRemoveFinishedBurns] Settings draw error: {ex.Message}");
        }
    }

    private static void DrawSettings()
    {
        ImGui.Indent();

        bool enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled"u8, ref enabled))
        {
            Config.Enabled = enabled;
            Config.Save();
        }

        ImGui.Spacing();
        ImGui.TextWrapped(
            "When on, finished auto-burns are automatically removed from the " +
            "burn plan. Detection only fires for completed auto-burns, never " +
            "manual burns. Out-of-fuel cases are left in place so you can " +
            "resume them after staging.");

        ImGui.Unindent();
    }
}
