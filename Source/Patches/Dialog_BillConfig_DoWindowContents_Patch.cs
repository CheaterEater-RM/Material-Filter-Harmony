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
            Bill_Production ___bill,
            ref bool ___absorbInputAroundWindow,
            ref bool ___closeOnClickedOutside,
            Rect ___windowRect)
        {
            if (!HasStuffIngredients(___bill))
                return;

            ThingFilter filter = ___bill.ingredientFilter;
            var buttonSize = new Vector2(122f, 25f);
            float top = ___windowRect.y;
            float left = ___windowRect.x + ___windowRect.width;

            if (Widgets.ButtonText(new Rect(642f, 25f, buttonSize.x, buttonSize.y),
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
                        top,
                        left,
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
                foreach (ThingDef td in ingredients[i].filter.AllowedThingDefs)
                {
                    if (td.MadeFromStuff)
                        return true;
                }
            }
            return false;
        }
    }
}
