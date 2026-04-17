using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CustomRulesPresets.UI;
using CustomRulesPresets.Core;
using System.Reflection;

namespace CustomRulesPresets {
    public enum Error {
        Success = 0,
        GenericFailure = 1,
        ArgumentNull = 2,
        ArgumentOutOfRange = 3,
        LocalVariableIsNull = 4,
        GlobalVariableIsNull = 5,
        FileDoesNotExist = 6,
        ObjectNotFound = 7
    }

    [BepInAutoPlugin]
    [BepInProcess("Super Battle Golf.exe")]
    public partial class CustomRulesPresetsPlugin: BaseUnityPlugin {
        internal static ManualLogSource Log {get; private set;} = null!;
        internal static string cache_path = "";
        public static UIManager ui_manager = null!;
        public static CustomRulesPresetsManager custom_rules_presets_manager = null!;
        public static ConfigManager config_manager = null!;
        public static Harmony harmony = null!;

        private void Awake() {
            Log = Logger;
            cache_path = Paths.CachePath + "/";

            Log.LogInfo("Loading config file...");
            config_manager = new ConfigManager(Config);
            Error config_load_error = config_manager.load_config_values();
            Logger.LogInfo($"Config load status: {config_load_error.ToString()}");
            Logger.LogInfo(Utilities.do_log_debug ? "Debug logging is enabled." : "Debug logging is disabled, set 'EnableLogDebugMessages' to 'true' to enable.");

            Log.LogInfo("Beginning patches...");
            harmony = new Harmony(Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{Name} is ready :D");
		}
    }

	[HarmonyPatch]
	public static class Patches {
        public static CustomRulesPresetsManager custom_rules_presets_manager = null!;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnStartClient))]
		public static void hook(MatchSetupMenu __instance) {
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "MatchSetupMenu.OnStartClient postfix hook called, setting up...");};

            if (CustomRulesPresetsPlugin.custom_rules_presets_manager == null) {
                if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Setting up CustomRulesPresetsManager...");}
			    custom_rules_presets_manager = new CustomRulesPresetsManager(__instance.rules);
                CustomRulesPresetsPlugin.custom_rules_presets_manager = custom_rules_presets_manager;
			    if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"CustomRulesPresetsManager exited setup with error: {custom_rules_presets_manager.construction_error.ToString()}");}
            }

			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Setting up UIManager...");}
            CustomRulesPresetsPlugin.ui_manager = new UIManager(__instance);
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"UIManager exited setup with error: {CustomRulesPresetsPlugin.ui_manager.construction_error.ToString()}");}
		}

        [HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupMenu), nameof(MatchSetupMenu.OnDestroy))]
		public static void reset() {
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "MatchSetupMenu.OnDestroy postfix hook called, freeing UIManager...");}
            CustomRulesPresetsPlugin.ui_manager = null!;
		}
	}
}
