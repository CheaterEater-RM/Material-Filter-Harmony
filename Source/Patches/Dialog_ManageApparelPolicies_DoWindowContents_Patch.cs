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
            ApparelPolicy ___policyInt,
            ref bool ___absorbInputAroundWindow,
            ref bool ___closeOnClickedOutside,
            Rect ___windowRect)
        {
            if (___policyInt == null)
                return;

            ThingFilter filter = ___policyInt.filter;
            var buttonSize = new Vector2(80f, 30f);
            float top = ___windowRect.y;
            float left = ___windowRect.x + ___windowRect.width;

            if (Widgets.ButtonText(new Rect(215f, 50f, buttonSize.x, buttonSize.y),
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
    }
}
