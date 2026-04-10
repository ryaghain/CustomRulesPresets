using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CustomRulesPresets.UI;
using CustomRulesPresets.Core;

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
    public partial class CustomRulesPresetsPlugin: BaseUnityPlugin {
        internal static ManualLogSource Log {get; private set;} = null!;
        public static ConfigManager config_manager = null!;
        public static Harmony harmony = null!;

        private void Awake() {
            Log = Logger;

            Log.LogInfo("Beginning patches...");
            harmony = new Harmony(Id);
            harmony.PatchAll();

            Log.LogInfo("Loading config file...");
            config_manager = new ConfigManager(Config);
            Error config_load_error = config_manager.load_config_values();
            Logger.LogInfo($"Config load status: {config_load_error.ToString()}");
            if (Utilities.do_log_debug) {Logger.LogInfo("Debug logging is enabled.");};

            Log.LogInfo($"{Name} is ready :D");
		}
    }

	[HarmonyPatch]
	public static class Patches {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnStartClient))]
		public static void hook(MatchSetupMenu __instance) {
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "MatchSetupMenu.OnStartClient postfix hook called, setting up UIManager...");};
			Error setup_error_code = UIManager.setup(__instance);
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"UIManager exited setup with error: {setup_error_code.ToString()}");};
		}

        [HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnMenuExit))]
        public static void on_menu_exit() {
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "MatchSetupMenu.OnMenuExit postfix hook called, saving presets to config...");};
            Error config_save_error = CustomRulesPresetsPlugin.config_manager.save_presets_to_config(CustomRulesPresetsManager.custom_rules_presets_data.to_json());
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Finished saving presets to config with error: {config_save_error.ToString()}");};
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
