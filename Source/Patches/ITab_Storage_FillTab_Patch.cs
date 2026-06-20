// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MaterialFilter.Patches
{
    /// <summary>
    /// Adds a "Filter>>" button to the storage tab (stockpiles, shelves, etc.).
    /// </summary>
    [HarmonyPatch(typeof(ITab_Storage), "FillTab")]
    public static class ITab_Storage_FillTab_Patch
    {
        private const float ButtonY = 10f;
        private const float ButtonHeight = 29f;
        private const float ButtonRightMargin = 30f;
        // With PSC present the button sits on the SAME top row as PSC's priority/letter controls, so
        // shift it further left of the X close button to leave a clear gap on both sides (PSC's letter
        // box ends ~x=216 on the 360px-wide tab; the X sits at the right edge).
        private const float PscButtonRightMargin = 46f;
        private const float ButtonMinWidth = 80f;

        private static readonly PropertyInfo SelStoreSettingsParentProperty =
            AccessTools.Property(typeof(ITab_Storage), "SelStoreSettingsParent");

        private static readonly PropertyInfo TabRectProperty =
            AccessTools.Property(typeof(ITab), "TabRect");

        private static bool missingTabRectPropertyLogged;

        [HarmonyPostfix]
        public static void Postfix(ITab_Storage __instance, Vector2 ___WinSize)
        {
            // Get the storage filter from the selected storage parent.
            var parent = SelStoreSettingsParentProperty?.GetValue(__instance) as IStoreSettingsParent;
            if (parent == null)
                return;

            var settings = parent.GetStoreSettings();
            if (settings == null)
                return;

            // Hide on storage that can never hold apparel/weapons (the only things material
            // filtering affects), plus any player-listed defNames.
            if (MaterialFilterStorageButtonFilter.ShouldHide(parent))
                return;

            ThingFilter filter = settings.filter;

            // Get the tab rect to position the popup window.
            Rect tabRect;
            if (TabRectProperty != null)
            {
                tabRect = (Rect)TabRectProperty.GetValue(__instance);
            }
            else
            {
                if (!missingTabRectPropertyLogged)
                {
                    missingTabRectPropertyLogged = true;
                    Log.Warning("[Material Filter] Missing ITab.TabRect property for "
                                + __instance.GetType().FullName
                                + "; using fallback popup positioning.");
                }

                tabRect = new Rect(0f, 0f, ___WinSize.x, ___WinSize.y);
            }

            string buttonLabel = "MaterialFilter_FilterButton".Translate() + ">>";

            // The button always lives on the top row. With PSC present, PSC's widened tab leaves room
            // for it in the gap between PSC's priority/letter controls (left) and the X close button
            // (right); the larger right margin keeps it clear of both. Standalone, it stays exactly
            // where it always was.
            float rightMargin = MaterialFilter_Init.PscActive ? PscButtonRightMargin : ButtonRightMargin;
            var buttonSize = new Vector2(MaterialFilterUI.GetButtonWidth(buttonLabel, ButtonMinWidth), ButtonHeight);
            var buttonRect = new Rect(
                ___WinSize.x - buttonSize.x - rightMargin,
                ButtonY,
                buttonSize.x,
                buttonSize.y);
            var buttonScreenRect = new Rect(
                tabRect.xMax - buttonSize.x - rightMargin,
                tabRect.y + buttonRect.y,
                buttonSize.x,
                buttonSize.y);
            TooltipHandler.TipRegion(buttonRect, "MaterialFilter_WindowHeader".Translate());
            if (Widgets.ButtonText(buttonRect, buttonLabel))
            {
                var existing = Find.WindowStack.WindowOfType<MaterialFilterWindow>();
                if (existing != null)
                {
                    existing.Close();
                }
                else
                {
                    Vector2 popupPosition = MaterialFilterUI.GetPopupPosition(
                        tabRect,
                        buttonScreenRect);
                    Find.WindowStack.Add(new MaterialFilterWindow(
                        filter,
                        popupPosition.y,
                        popupPosition.x,
                        WindowLayer.GameUI));
                }
            }
        }
    }
}
