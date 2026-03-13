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
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InstantMode;

[ModInitializer("Init")]
public static class ModEntry
{
    private static bool _initialized = false;
    private static bool _logCleared = false;
    public const float FastSpeed = 10.0f;
    public static bool IsEnabled = true;

    private static string GetLogPath()
    {
        try {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string modDir = Path.GetDirectoryName(assemblyPath);
            return Path.Combine(modDir, "instant_mode_debug.log");
        } catch {
            return "instant_mode_debug.log";
        }
    }

    public static void LogDebug(string msg)
    {
        string path = GetLogPath();
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string fullMsg = $"[{timestamp}] {msg}";
        
        Log.Warn($"[InstantMode] {msg}");

        try {
            if (!_logCleared) {
                File.WriteAllText(path, fullMsg + System.Environment.NewLine);
                _logCleared = true;
            } else {
                File.AppendAllText(path, fullMsg + System.Environment.NewLine);
            }
        } catch {}
    }

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        LogDebug("Initializing InstantMode (Stable Version)...");

        try {
            var harmony = new Harmony("com.instantmode.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            TryPatchFastMode(harmony);
            
            var manager = new SpeedManager();
            manager.Name = "InstantModeSpeedManager";
            NGame.Instance?.CallDeferred(Node.MethodName.AddChild, manager);

            LogDebug("Init complete. F8 to toggle.");
        } catch (Exception ex) {
            LogDebug($"FATAL INIT ERROR: {ex}");
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
            LogDebug("FastMode property getter patched to return 'Fast'.");
        } catch (Exception ex) {
            LogDebug($"Could not patch FastMode getter: {ex}");
        }
    }

    public static void Toggle()
    {
        try {
            IsEnabled = !IsEnabled;
            LogDebug($"Toggle -> {IsEnabled}");
            
            if (NGame.Instance != null)
            {
                NGame.Instance.AddChild(NFullscreenTextVfx.Create(IsEnabled ? "Instant Mode: ON" : "Instant Mode: OFF"));
            }
        } catch (Exception ex) {
            LogDebug($"Error during toggle: {ex}");
        }
    }
}

public static class FastModeGetterPatch
{
    public static bool Prefix(ref FastModeType __result)
    {
        if (ModEntry.IsEnabled)
        {
            // We return 'Fast' globally. This ensures Transitions play (avoiding flicker).
            // We use our Cmd.Wait patch to make logical waits actually instant.
            __result = FastModeType.Fast;
            return false;
        }
        return true;
    }
}

public partial class SpeedManager : Node
{
    public override void _Process(double delta)
    {
        try {
            if (!ModEntry.IsEnabled)
            {
                if (Engine.TimeScale != 1.0) Engine.TimeScale = 1.0;
                return;
            }

            if (Engine.TimeScale != (double)ModEntry.FastSpeed)
            {
                Engine.TimeScale = (double)ModEntry.FastSpeed;
            }
        } catch {}
    }
}

[HarmonyPatch(typeof(NTransition))]
public static class TransitionPatch
{
    [HarmonyPatch(nameof(NTransition.FadeOut))]
    [HarmonyPrefix]
    static void FadeOutPrefix(ref float time)
    {
        if (ModEntry.IsEnabled)
        {
            time = 1.0f; // 0.1s visual
        }
    }

    [HarmonyPatch(nameof(NTransition.FadeIn))]
    [HarmonyPrefix]
    static void FadeInPrefix(ref float time)
    {
        if (ModEntry.IsEnabled)
        {
            time = 1.0f; // 0.1s visual
        }
    }
}

[HarmonyPatch(typeof(Cmd))]
public static class CmdWaitPatch
{
    [HarmonyPatch(nameof(Cmd.Wait), new Type[] { typeof(float), typeof(bool) })]
    [HarmonyPrefix]
    static bool Prefix1(ref Task __result)
    {
        if (ModEntry.IsEnabled)
        {
            __result = Task.CompletedTask;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Cmd.Wait), new Type[] { typeof(float), typeof(CancellationToken), typeof(bool) })]
    [HarmonyPrefix]
    static bool Prefix2(ref Task __result)
    {
        if (ModEntry.IsEnabled)
        {
            __result = Task.CompletedTask;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Tween), nameof(Tween.SetParallel))]
public static class TweenSpeedPatch
{
    static void Postfix(Tween __result)
    {
        try {
            if (ModEntry.IsEnabled && __result != null)
            {
                __result.SetSpeedScale(ModEntry.FastSpeed);
            }
        } catch {}
    }
}

[HarmonyPatch(typeof(NGame), "_Input")]
public static class InputPatch
{
    static void Postfix(InputEvent inputEvent)
    {
        try {
            if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.IsEcho())
            {
                if (keyEvent.Keycode == Key.F8)
                {
                    ModEntry.Toggle();
                }
            }
        } catch {}
    }
}
