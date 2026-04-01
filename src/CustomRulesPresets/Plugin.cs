using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CustomRulesPresets.Core;
using CustomRulesPresets.UI;

namespace CustomRulesPresets {
    [BepInAutoPlugin]
    [BepInProcess("Super Battle Golf.exe")]

    public partial class Plugin: BaseUnityPlugin {
        internal static ManualLogSource Log { get; private set; } = null!;
        public static Harmony harmony = null!;

        public static bool shownErrorThisSession = false;

        private void Awake() {
            Log = Logger;
            Log.LogInfo($"{Name} is loaded, beginning patches...");

            harmony = new Harmony(Id);
            harmony.PatchAll();

            Log.LogInfo($"{Name} finished patching");
		}
    }

	[HarmonyPatch]
	public static class Patches {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(MenuTabs), nameof(MenuTabs.Awake))]
		public static void HookIntoGUI(MenuTabs __instance) {
			UIManager.Inject(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MatchSetupRules), "Awake")]
		public static void HookIntoMatchSetupRules(MatchSetupRules __instance) {
			CustomRulesPresetsManager.setup(__instance);
		}
	}
}
