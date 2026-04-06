using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CustomRulesPresets.UI;

/* 
TODO:
- Add a config file manager
- fix the new dropdown stretching
- fix the new dropdown outline
- add a debug bool to the eventual config file to toggle debug logging so others can get debug info if they want
*/

namespace CustomRulesPresets {
    [BepInAutoPlugin]
    [BepInProcess("Super Battle Golf.exe")]
    public partial class Plugin: BaseUnityPlugin {
        internal static ManualLogSource Log { get; private set; } = null!;
        public static Harmony harmony = null!;

        private void Awake() {
            Log = Logger;
            Log.LogInfo($"{Name} is loaded, beginning patches...");

            harmony = new Harmony(Id);
            harmony.PatchAll();

            Log.LogInfo($"{Name} finished patching, plugin is ready :D");
		}
    }

	[HarmonyPatch]
	public static class Patches {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnStartClient))]
		public static void hook(MatchSetupMenu __instance) {
            Plugin.Log.LogDebug("MatchSetupMenu.OnStartClient postfix hook called, setting up UIManager...");
			int setup_error_code = UIManager.setup(__instance);
            Plugin.Log.LogDebug($"UIManager exited setup with code: {setup_error_code}");
		}

        [HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnDestroy))]
		public static void reset() {
            Plugin.Log.LogDebug("MatchSetupMenu.OnDestroy postfix hook called, resetting UIManager...");
            UIManager.reset();
            Plugin.Log.LogDebug("Finished reset.");
		}
	}
}
