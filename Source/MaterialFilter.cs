// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater: removed HugsLib dependency, eliminated dynamic
// assembly generation, improved performance.

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MaterialFilter
{
    /// <summary>
    /// Harmony entry point. Fires after all Defs are loaded.
    /// Generates SpecialThingFilterDefs for every stuff material, then applies patches.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MaterialFilter_Init
    {
        /// <summary>
        /// All material filter defs we generated, sorted by label. Built once at startup.
        /// </summary>
        internal static List<SpecialThingFilterDef> GeneratedFilterDefs;

        internal static Dictionary<SpecialThingFilterDef, ThingDef> GeneratedFilterMaterials;

        static MaterialFilter_Init()
        {
            GenerateFilterDefs();

            var harmony = new Harmony("com.cheatereater.materialfilter");
            harmony.PatchAll();
            Log.Message("[Material Filter] Harmony patches applied — "
                        + GeneratedFilterDefs.Count + " material filters registered.");
        }

        private static void GenerateFilterDefs()
        {
            // Match the original HugsLib mod's generation rules so old saves
            // resolve against the same material-filter def set.
            var seenDefNames = new HashSet<string>();
            var materialDefs = new List<ThingDef>();

            foreach (var td in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (ShouldGenerateMaterialFilter(td) && seenDefNames.Add(td.defName))
                {
                    materialDefs.Add(td);
                }
            }

            GeneratedFilterDefs = new List<SpecialThingFilterDef>(materialDefs.Count);
            GeneratedFilterMaterials = new Dictionary<SpecialThingFilterDef, ThingDef>(materialDefs.Count);

            // Get the mod content pack so the defs are associated with this mod.
            var mcp = LoadedModManager.RunningMods
                .FirstOrDefault(m => m.PackageId.ToLower() == "cheatereater.materialfilter");

            foreach (var stuffDef in materialDefs)
            {
                var filterDefName = "MaterialFilter_allow" + stuffDef.defName;

                var filterDef = new SpecialThingFilterDef
                {
                    defName = filterDefName,
                    label = stuffDef.label,
                    description = "Allow " + stuffDef.label,
                    parentCategory = ThingCategoryDefOf.Root,
                    allowedByDefault = true,
                    saveKey = "allow" + stuffDef.defName,
                    configurable = true,
                    workerClass = typeof(MaterialSpecialThingFilterWorker),
                    modContentPack = mcp
                };

                if (filterDef.parentCategory.childSpecialFilters == null)
                    filterDef.parentCategory.childSpecialFilters = new List<SpecialThingFilterDef>();

                filterDef.parentCategory.childSpecialFilters.Add(filterDef);
                DefGenerator.AddImpliedDef(filterDef);

                // Force-instantiate the worker, then register the mapping from
                // that specific worker instance to the target stuff ThingDef.
                // SpecialThingFilterDef.Worker uses Activator.CreateInstance internally
                // and caches the result, so each def gets its own worker instance.
                var worker = filterDef.Worker;
                if (worker is MaterialSpecialThingFilterWorker mw)
                {
                    mw.SetStuffDef(stuffDef);
                }
                else
                {
                    Log.Warning("[Material Filter] Expected "
                                + typeof(MaterialSpecialThingFilterWorker).FullName
                                + " for filter " + filterDef.defName
                                + " (workerClass=" + filterDef.workerClass?.FullName
                                + ", workerType=" + (worker?.GetType().FullName ?? "<null>")
                                + "); SetStuffDef was not called for stuff "
                                + stuffDef.defName + ".");
                }

                GeneratedFilterDefs.Add(filterDef);
                GeneratedFilterMaterials[filterDef] = stuffDef;
            }

            // Sort by label for consistent UI display.
            GeneratedFilterDefs.SortBy(d => d.label);
        }

        private static bool ShouldGenerateMaterialFilter(ThingDef thingDef)
        {
            return thingDef?.stuffProps?.categories != null
                   && thingDef.stuffProps.categories.Count > 0;
        }

        internal static bool TryGetMaterialDef(SpecialThingFilterDef filterDef, out ThingDef materialDef)
        {
            if (filterDef != null && GeneratedFilterMaterials != null)
                return GeneratedFilterMaterials.TryGetValue(filterDef, out materialDef);

            materialDef = null;
            return false;
        }
    }

    /// <summary>
    /// Single shared worker *class* for all material filters. Each SpecialThingFilterDef
    /// gets its own *instance* (via Activator.CreateInstance), and we call SetStuffDef()
    /// immediately after instantiation to bind it to the correct material.
    ///
    /// This replaces the original mod's approach of dynamically emitting hundreds of IL
    /// classes at startup (one AssemblyBuilder + TypeBuilder per material), which was both
    /// slow and fragile.
    /// </summary>
    public class MaterialSpecialThingFilterWorker : SpecialThingFilterWorker
    {
        private ThingDef stuffDef;

        internal void SetStuffDef(ThingDef def)
        {
            stuffDef = def;
        }

        public override bool Matches(Thing t)
        {
            return t.Stuff != null && t.Stuff == stuffDef;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.MadeFromStuff;
        }
    }
}
