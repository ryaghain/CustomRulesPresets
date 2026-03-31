using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CustomRulesPresets.Core;

namespace CustomRulesPresets {
    // Here are some basic resources on code style and naming conventions to help you in your first CSharp plugin!
    // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
    // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
    // https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces

    // This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin NuGet package, and it will generate the BepInPlugin attribute for you! For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin

    [BepInAutoPlugin]
    [BepInProcess("Super Battle Golf.exe")]

    public partial class Plugin: BaseUnityPlugin {
        internal static ManualLogSource Log { get; private set; } = null!;
        public static Harmony harmony = null!;

        public static bool shownErrorThisSession = false;

        private void Awake() {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");

            harmony = new Harmony(Id);
            harmony.PatchAll();
        }

        public static bool IsBepInExModInstalled(string guid) {
            return Chainloader.PluginInfos.ContainsKey(guid);
        }
    }
}
