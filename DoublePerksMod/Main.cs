using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace DoublePerksMod
{
    public class Main : MBSubModuleBase
    {
        /// <summary>
        /// Initializes the mod by loading and applying Harmony patches when the submodule is loaded.
        /// </summary>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("com.geniussjack.doubleperksmod");
            harmony.PatchAll();
        }
    }
}