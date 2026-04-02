// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace MaterialFilter.Patches
{
    /// <summary>
    /// Hides our generated SpecialThingFilterDefs from the vanilla ThingFilterUI list,
    /// since we display them in our own popup window instead.
    /// </summary>
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public static class ThingFilterUI_DoThingFilterConfigWindow_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ref IEnumerable<SpecialThingFilterDef> forceHiddenFilters)
        {
            var generated = MaterialFilter_Init.GeneratedFilterDefs;
            if (generated == null || generated.Count == 0)
                return;

            if (forceHiddenFilters == null)
            {
                forceHiddenFilters = generated;
            }
            else
            {
                forceHiddenFilters = forceHiddenFilters.Concat(generated);
            }
        }
    }
}
