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
            harmony.PatchAll();
            Log.Message("[CarryCapacityFromBionics Debug] Harmony patches applied.");
        }
    }
}
