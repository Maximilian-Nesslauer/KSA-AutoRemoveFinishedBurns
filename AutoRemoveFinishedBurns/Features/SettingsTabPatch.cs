#if DEBUG
using System.Diagnostics;
#endif
using System.Reflection;
using System.Reflection.Emit;
using AutoRemoveFinishedBurns.Core;
using Brutal.ImGuiApi;
using Brutal.Logging;
using HarmonyLib;
using KSA;

namespace AutoRemoveFinishedBurns.Features;

[HarmonyPatch(typeof(GameSettings), nameof(GameSettings.OnDrawUi),
    new[] { typeof(Camera) })]
static class SettingsTabPatch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        MethodInfo endTabBar = GameReflection.ImGui_EndTabBar!;
        MethodInfo endRegionTab = GameReflection.ImGuiHelper_EndRegionTab!;
        MethodInfo drawerCall = AccessTools.Method(typeof(SettingsTabPatch),
            nameof(DrawBeforeEndRegionTab), Type.EmptyTypes)!;

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
                $"[AutoRemoveFinishedBurns] Transpiler: EndTabBar not found in " +
                $"GameSettings.OnDrawUi (scanned {codes.Count} IL instructions). " +
                "Settings tab not patched.");
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
                $"[AutoRemoveFinishedBurns] Transpiler: Mods-tab close call " +
                $"(EndRegionTab or EndModsTabWithSettings wrapper) not found in " +
                $"the {endTabBarIdx} IL instructions before EndTabBar. " +
                "Settings tab not patched.");
            return codes;
        }

        // Parameterless call doesn't disturb the bool already on the stack
        // for the close. Labels stay on the close so jumps targeting it skip
        // our drawer.
        codes.Insert(anchorIdx, new CodeInstruction(OpCodes.Call, drawerCall));

        return codes;
    }

    // Convention-based loose-name match: any mod that wraps the close call
    // exposes a static void(bool) method named "EndModsTabWithSettings" so
    // other mods can find it as an anchor. AutoStage uses this name today.
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
#if DEBUG
        long perfStart = DebugConfig.Performance ? Stopwatch.GetTimestamp() : 0;
#endif
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
            LogHelper.ErrorOnce("Settings.Draw:" + ex.GetType().Name,
                $"[AutoRemoveFinishedBurns] Settings draw threw: {ex}");
        }
#if DEBUG
        if (DebugConfig.Performance)
            PerfTracker.Record("SettingsTabPatch.DrawBeforeEndRegionTab",
                Stopwatch.GetTimestamp() - perfStart);
#endif
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
