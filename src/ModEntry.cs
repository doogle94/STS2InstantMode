using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Extensions;
using Godot;
using System;
using System.Reflection;

namespace InstantMode;

[ModInitializer("Init")]
public static class ModEntry
{
    private static bool _initialized = false;
    public const float FastSpeed = 10.0f;
    public static bool IsEnabled = true;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        Log.Warn("[InstantMode] Initializing...");

        try {
            var harmony = new Harmony("com.instantmode.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            TryPatchFastMode(harmony);
            
            // Add a headless node to manage speed persistence
            var manager = new SpeedManager();
            manager.Name = "InstantModeSpeedManager";
            NGame.Instance?.CallDeferred(Node.MethodName.AddChild, manager);

            Log.Warn("[InstantMode] Initialized. F8: Toggle Instant Mode.");
        } catch (Exception ex) {
            Log.Error($"[InstantMode] FATAL INIT ERROR: {ex}");
        }
    }

    private static void TryPatchFastMode(Harmony harmony)
    {
        try {
            var saveManagerType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager");
            var prefsSaveProp = AccessTools.Property(saveManagerType, "PrefsSave");
            var prefsSaveType = prefsSaveProp.PropertyType;
            var fastModeProp = AccessTools.Property(prefsSaveType, "FastMode");
            var getter = fastModeProp.GetGetMethod();
            var prefix = AccessTools.Method(typeof(FastModeGetterPatch), nameof(FastModeGetterPatch.Prefix));
            harmony.Patch(getter, new HarmonyMethod(prefix));
        } catch (Exception ex) {
            Log.Warn($"[InstantMode] Could not patch FastMode getter: {ex.Message}");
        }
    }
}

public static class FastModeGetterPatch
{
    public static bool Prefix(ref FastModeType __result)
    {
        if (ModEntry.IsEnabled)
        {
            // Reverting to True Instant as requested
            __result = FastModeType.Instant;
            return false;
        }
        return true;
    }
}

public partial class SpeedManager : Node
{
    public override void _Process(double delta)
    {
        if (!ModEntry.IsEnabled)
        {
            if (Engine.TimeScale != 1.0) Engine.TimeScale = 1.0;
            return;
        }

        if (Engine.TimeScale != (double)ModEntry.FastSpeed)
        {
            Engine.TimeScale = (double)ModEntry.FastSpeed;
        }
    }
}

[HarmonyPatch(typeof(Cmd), nameof(Cmd.Wait), new Type[] { typeof(float), typeof(bool) })]
public static class CmdWaitPatch
{
    static bool Prefix(ref float seconds)
    {
        if (ModEntry.IsEnabled)
        {
            seconds = 0f;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Tween), nameof(Tween.SetParallel))]
public static class TweenSpeedPatch
{
    static void Postfix(Tween __result)
    {
        if (ModEntry.IsEnabled && __result != null)
        {
            __result.SetSpeedScale(ModEntry.FastSpeed);
        }
    }
}

[HarmonyPatch(typeof(NGame), "_Input")]
public static class InputPatch
{
    static void Postfix(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.IsEcho())
        {
            if (keyEvent.Keycode == Key.F8)
            {
                ModEntry.IsEnabled = !ModEntry.IsEnabled;
                Log.Warn($"[InstantMode] Toggle -> {ModEntry.IsEnabled}");
                
                if (NGame.Instance != null)
                {
                    try {
                        NGame.Instance.AddChild(NFullscreenTextVfx.Create(ModEntry.IsEnabled ? "Instant Mode: ON" : "Instant Mode: OFF"));
                    } catch {}
                }
            }
        }
    }
}
