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
    public enum Error {
        Success = 0,
        GenericFailure = 1,
        ArgumentNull = 2,
        ArgumentOutOfRange = 3,
        LocalVariableIsNull = 4,
        GlobalVariableIsNull = 5,
    }

    [BepInAutoPlugin]
    [BepInProcess("Super Battle Golf.exe")]
    public partial class Plugin: BaseUnityPlugin {
        internal static ManualLogSource Log { get; private set; } = null!;
        public static Harmony harmony = null!;

        private void Awake() {
            Log = Logger;
            Utilities.do_log_debug = true;
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
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "MatchSetupMenu.OnStartClient postfix hook called, setting up UIManager...");};
			Error setup_error_code = UIManager.setup(__instance);
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"UIManager exited setup with code: {setup_error_code.ToString()}");};
		}

        [HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnDestroy))]
		public static void reset() {
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "MatchSetupMenu.OnDestroy postfix hook called, resetting UIManager...");};
            UIManager.reset();
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Finished reset.");};
		}
	}
}
