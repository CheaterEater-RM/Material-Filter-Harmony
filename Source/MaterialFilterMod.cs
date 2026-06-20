// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MaterialFilter
{
    // Mod settings. The mod shipped without any settings; this is the first. RimWorld auto-detects
    // Mod subclasses and shows the page when SettingsCategory() returns a non-empty string, so no
    // About.xml change is needed.
    public class MaterialFilterSettings : ModSettings
    {
        // Hide the Filter button on storage that can never hold apparel or weapons — the only items
        // material filtering affects (see MaterialFilter_Init.TryGetRelevantMaterials).
        public bool hideButtonWhereUseless = true;

        // Extra storage defNames to always hide the button on, even if they could hold
        // apparel/weapons. One exact, case-sensitive defName per entry; edited as one-per-line text.
        public List<string> extraExcludedDefNames = new List<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hideButtonWhereUseless, "hideButtonWhereUseless", true);
            Scribe_Collections.Look(ref extraExcludedDefNames, "extraExcludedDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && extraExcludedDefNames == null)
                extraExcludedDefNames = new List<string>();
        }
    }

    public class MaterialFilterMod : Mod
    {
        public static MaterialFilterSettings Settings { get; private set; }

        // Editable mirror of Settings.extraExcludedDefNames as one-per-line text. Seeded lazily on
        // first draw; parsed back into the list whenever it changes.
        private string excludedDefNamesBuffer;

        public MaterialFilterMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MaterialFilterSettings>();
        }

        public override string SettingsCategory()
        {
            return "MaterialFilter_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("MaterialFilter_SettingsStorageButtonHeader".Translate());
            listing.Gap(6f);
            listing.CheckboxLabeled("MaterialFilter_SettingsHideButtonUseless".Translate(),
                ref Settings.hideButtonWhereUseless,
                "MaterialFilter_SettingsHideButtonUselessTip".Translate());

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            listing.Label("MaterialFilter_SettingsExtraExcludedLabel".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (excludedDefNamesBuffer == null)
                excludedDefNamesBuffer = string.Join("\n", Settings.extraExcludedDefNames);
            string edited = Widgets.TextArea(listing.GetRect(72f), excludedDefNamesBuffer);
            if (edited != excludedDefNamesBuffer)
            {
                excludedDefNamesBuffer = edited;
                Settings.extraExcludedDefNames = edited
                    .Split('\n')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
            }

            listing.End();
        }
    }
}
