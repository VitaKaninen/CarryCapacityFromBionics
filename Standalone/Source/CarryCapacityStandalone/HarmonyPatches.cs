using HarmonyLib;
using Verse;

namespace Vita.CarryCapacityFromBionics
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("Vita.CarryCapacityFromBionics");
            harmony.PatchAll(typeof(HarmonyPatches).Assembly);
            Log.Message("[CarryCapacityFromBionics Debug] Harmony patches applied.");
        }
    }
}
