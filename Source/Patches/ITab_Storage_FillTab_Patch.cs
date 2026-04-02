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
            var buttonSize = new Vector2(MaterialFilterUI.GetButtonWidth(buttonLabel, 80f), 29f);
            var buttonRect = new Rect(
                ___WinSize.x - buttonSize.x - 10f,
                10f,
                buttonSize.x,
                buttonSize.y);
            var buttonScreenRect = new Rect(
                tabRect.xMax - buttonSize.x - 10f,
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
