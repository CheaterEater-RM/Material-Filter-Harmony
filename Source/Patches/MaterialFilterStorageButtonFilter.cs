// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MaterialFilter
{
    // Decides whether to suppress the Filter button on a storage. Material filtering only ever
    // affects apparel and weapons (MaterialFilter_Init.TryGetRelevantMaterials early-returns for
    // anything else), so the button is useless on storage that can never hold either — bookcases,
    // graves, food/feedstock containers, shell racks, gene banks, and the equivalent modded storage
    // (Reel's Expanded Storage etc.). This is a content rule, not a defName list, so it covers every
    // storage mod automatically; the editable defName list adds manual overrides on top.
    public static class MaterialFilterStorageButtonFilter
    {
        // Per-def cache of "can this storage ever hold apparel or weapons?" The answer derives from
        // the def's fixed storage settings, so it is stable for the life of the game.
        private static readonly Dictionary<ThingDef, bool> CanHoldApparelOrWeaponByDef =
            new Dictionary<ThingDef, bool>();

        public static bool ShouldHide(IStoreSettingsParent parent)
        {
            if (parent == null) return false;
            var settings = MaterialFilterMod.Settings;
            if (settings == null) return false;

            ThingDef def = ResolveDef(parent);

            List<string> extra = settings.extraExcludedDefNames;
            if (def != null && extra != null && extra.Count > 0 && extra.Contains(def.defName))
                return true;

            if (!settings.hideButtonWhereUseless) return false;

            return !CanEverHoldApparelOrWeapon(parent, def);
        }

        private static bool CanEverHoldApparelOrWeapon(IStoreSettingsParent parent, ThingDef def)
        {
            if (def != null && CanHoldApparelOrWeaponByDef.TryGetValue(def, out bool cached))
                return cached;

            bool result = Compute(parent);
            if (def != null) CanHoldApparelOrWeaponByDef[def] = result;
            return result;
        }

        private static bool Compute(IStoreSettingsParent parent)
        {
            // No fixed restriction → can hold anything → keep.
            StorageSettings fixedSettings = parent.GetParentStoreSettings();
            ThingFilter filter = fixedSettings?.filter;
            if (filter == null) return true;

            // Unrestricted storage (stockpile zones, and general shelves with no fixedStorageSettings)
            // returns the shared "ever storable" settings — it can hold apparel/weapons, so keep,
            // without enumerating that huge filter. Zones have no def so they aren't cached; this
            // keeps their per-frame cost O(1).
            if (ReferenceEquals(fixedSettings, StorageSettings.EverStorableFixedSettings())) return true;

            // Mirror MaterialFilter_Init.TryGetRelevantMaterials' relevance test exactly so the
            // button shows iff the mod could actually do something here.
            foreach (ThingDef d in filter.AllowedThingDefs)
                if (d.apparel != null || !d.weaponClasses.NullOrEmpty())
                    return true;

            return false;
        }

        private static ThingDef ResolveDef(IStoreSettingsParent parent)
        {
            if (parent is Thing t) return t.def;
            if (parent is ThingComp c) return c.parent?.def;
            return null;
        }
    }
}
