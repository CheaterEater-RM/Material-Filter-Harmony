// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System;
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
    [HarmonyPatch]
    public static class Dialog_ManageApparelPolicies_DoWindowContents_Patch
    {
        private static readonly FieldInfo SelectedPolicyField =
            AccessTools.Field(typeof(Dialog_ManagePolicies<ApparelPolicy>), "policyInt");

        private static readonly FieldInfo AbsorbInputAroundWindowField =
            AccessTools.Field(typeof(Window), "absorbInputAroundWindow");

        private static readonly FieldInfo CloseOnClickedOutsideField =
            AccessTools.Field(typeof(Window), "closeOnClickedOutside");

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Dialog_ManagePolicies<ApparelPolicy>),
                nameof(Dialog_ManagePolicies<ApparelPolicy>.DoWindowContents));
        }

        [HarmonyPostfix]
        public static void Postfix(
            Dialog_ManagePolicies<ApparelPolicy> __instance,
            Rect __0,
            ref bool ___absorbInputAroundWindow,
            ref bool ___closeOnClickedOutside,
            Rect ___windowRect)
        {
            Dialog_ManageApparelPolicies apparelDialog = __instance as Dialog_ManageApparelPolicies;
            if (apparelDialog == null)
                return;

            ApparelPolicy selectedPolicy = SelectedPolicyField?.GetValue(apparelDialog) as ApparelPolicy;
            if (selectedPolicy?.filter == null)
                return;

            ThingFilter filter = selectedPolicy.filter;
            string buttonLabel = "MaterialFilter_FilterButton".Translate();
            var buttonSize = new Vector2(MaterialFilterUI.GetButtonWidth(buttonLabel, 80f), 28f);
            const float headerHeight = 32f;
            const float leftColumnWidth = 200f;
            const float columnGap = 10f;
            const float headerGap = 10f;
            const float reservedIconWidth = 158f;
            const float buttonGap = 12f;

            Rect policyHeaderRect = new Rect(
                __0.x + leftColumnWidth + columnGap,
                __0.y + headerHeight * 2f + headerGap,
                __0.width - leftColumnWidth - columnGap,
                headerHeight);

            float buttonX = Math.Max(
                policyHeaderRect.x,
                policyHeaderRect.xMax - reservedIconWidth - buttonGap - buttonSize.x);
            var buttonRect = new Rect(
                buttonX,
                policyHeaderRect.y + (policyHeaderRect.height - buttonSize.y) / 2f,
                buttonSize.x,
                buttonSize.y);
            var buttonScreenRect = new Rect(
                ___windowRect.x + buttonRect.x,
                ___windowRect.y + buttonRect.y,
                buttonRect.width,
                buttonRect.height);
            TooltipHandler.TipRegion(buttonRect, "MaterialFilter_WindowHeader".Translate());

            if (Widgets.ButtonText(buttonRect, buttonLabel))
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
                    Vector2 popupPosition = MaterialFilterUI.GetPopupPosition(
                        ___windowRect,
                        buttonScreenRect);
                    Find.WindowStack.Add(new MaterialFilterWindow(
                        filter,
                        popupPosition.y,
                        popupPosition.x,
                        WindowLayer.Dialog,
                        () => RestoreParentWindowState(apparelDialog)));
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
