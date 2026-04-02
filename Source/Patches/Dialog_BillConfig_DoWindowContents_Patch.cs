// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MaterialFilter.Patches
{
    /// <summary>
    /// Adds a "Filter>>" button to the bill configuration window,
    /// but only if the recipe involves stuff-bearing ingredients.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
    public static class Dialog_BillConfig_DoWindowContents_Patch
    {
        private static readonly FieldInfo AbsorbInputAroundWindowField =
            AccessTools.Field(typeof(Window), "absorbInputAroundWindow");

        private static readonly FieldInfo CloseOnClickedOutsideField =
            AccessTools.Field(typeof(Window), "closeOnClickedOutside");

        [HarmonyPostfix]
        public static void Postfix(
            Dialog_BillConfig __instance,
            Rect __0,
            Bill_Production ___bill,
            ref bool ___absorbInputAroundWindow,
            ref bool ___closeOnClickedOutside,
            Rect ___windowRect)
        {
            if (!HasStuffIngredients(___bill) || ___bill?.ingredientFilter == null)
                return;

            ThingFilter filter = ___bill.ingredientFilter;
            var buttonSize = new Vector2(122f, 25f);
            var buttonRect = new Rect(
                __0.xMax - buttonSize.x,
                __0.y,
                buttonSize.x,
                buttonSize.y);

            if (Widgets.ButtonText(buttonRect,
                                   "MaterialFilter_FilterButton".Translate() + ">>"))
            {
                var existing = Find.WindowStack.WindowOfType<MaterialFilterWindow>();
                if (existing != null)
                {
                    ___absorbInputAroundWindow = true;
                    ___closeOnClickedOutside = true;
                    existing.Close();
                }
                else
                {
                    ___absorbInputAroundWindow = false;
                    ___closeOnClickedOutside = false;
                    Find.WindowStack.Add(new MaterialFilterWindow(
                        filter,
                        ___windowRect.y + buttonRect.y,
                        ___windowRect.xMax + MaterialFilterUI.PopupGap,
                        WindowLayer.Dialog,
                        () => RestoreParentWindowState(__instance)));
                }
            }
        }

        private static void RestoreParentWindowState(Window parentWindow)
        {
            if (parentWindow == null)
                return;

            AbsorbInputAroundWindowField?.SetValue(parentWindow, true);
            CloseOnClickedOutsideField?.SetValue(parentWindow, true);
        }

        private static bool HasStuffIngredients(Bill_Production bill)
        {
            if (bill?.recipe?.ingredients == null)
                return false;

            List<IngredientCount> ingredients = bill.recipe.ingredients;
            for (int i = 0; i < ingredients.Count; i++)
            {
                ThingFilter ingredientFilter = ingredients[i].filter;
                if (ingredientFilter == null)
                    continue;

                foreach (ThingDef td in ingredientFilter.AllowedThingDefs)
                {
                    if (td != null && td.MadeFromStuff)
                        return true;
                }
            }
            return false;
        }
    }
}
