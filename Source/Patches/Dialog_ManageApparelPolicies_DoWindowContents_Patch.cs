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
    /// Adds a "Filter>>" button to the apparel policy management window.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_ManagePolicies<ApparelPolicy>),
                  nameof(Dialog_ManagePolicies<ApparelPolicy>.DoWindowContents))]
    public static class Dialog_ManageApparelPolicies_DoWindowContents_Patch
    {
        private static readonly FieldInfo AbsorbInputAroundWindowField =
            AccessTools.Field(typeof(Window), "absorbInputAroundWindow");

        private static readonly FieldInfo CloseOnClickedOutsideField =
            AccessTools.Field(typeof(Window), "closeOnClickedOutside");

        [HarmonyPostfix]
        public static void Postfix(
            Dialog_ManagePolicies<ApparelPolicy> __instance,
            Rect __0,
            ApparelPolicy ___policyInt,
            ref bool ___absorbInputAroundWindow,
            ref bool ___closeOnClickedOutside,
            Rect ___windowRect)
        {
            if (___policyInt?.filter == null)
                return;

            ThingFilter filter = ___policyInt.filter;
            var buttonSize = new Vector2(80f, 30f);
            const float titleHeight = 32f;
            const float tipHeight = 32f;
            const float leftColumnWidth = 200f;
            const float columnGap = 10f;

            var buttonRect = new Rect(
                __0.x + leftColumnWidth + columnGap,
                __0.y + titleHeight + (tipHeight - buttonSize.y) / 2f,
                buttonSize.x,
                buttonSize.y);

            if (Widgets.ButtonText(buttonRect,
                                   "MaterialFilter_FilterButton".Translate()))
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
    }
}
