// Material Filter — Harmony fork of Kami Katze's Material Filter (GPL-3.0)
// Rewritten by CheaterEater.

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MaterialFilter
{
    /// <summary>
    /// Popup window listing all material filters with checkboxes.
    /// Displayed when the player clicks "Filter>>" on a storage, bill, or outfit window.
    /// </summary>
    public class MaterialFilterWindow : Window
    {
        private readonly ThingFilter filter;
        private readonly float left;
        private readonly Action onClose;
        private readonly float top;
        private Vector2 scrollPosition;

        // Cached from the pre-sorted list built at startup.
        private readonly List<SpecialThingFilterDef> filterDefs;

        protected override float Margin => 8f;

        public override Vector2 InitialSize => new Vector2(
            Math.Min(300, Math.Max(1, UI.screenWidth - 300)),
            480);

        public MaterialFilterWindow(
            ThingFilter filter,
            float top,
            float left,
            WindowLayer layer,
            Action onClose = null)
        {
            this.layer = layer;
            this.preventCameraMotion = false;
            this.soundAppear = SoundDefOf.TabOpen;
            this.soundClose = SoundDefOf.TabClose;
            this.doCloseX = false;
            this.closeOnClickedOutside = true;
            this.resizeable = false;
            this.draggable = false;
            this.left = left;
            this.onClose = onClose;
            this.top = top;
            this.filter = filter;

            // Use the pre-sorted list built once at startup — no allocations here.
            filterDefs = MaterialFilter_Init.GeneratedFilterDefs;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            windowRect = new Rect(left, top, InitialSize.x, InitialSize.y);
        }

        public override void PreClose()
        {
            base.PreClose();
            onClose?.Invoke();
        }

        private void SetAllowAll(bool allow)
        {
            for (int i = 0; i < filterDefs.Count; i++)
            {
                filter.SetAllow(filterDefs[i], allow);
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            const float lineHeight = 25f;
            const float indent = 5f;
            const float padding = 5f;
            string headerText = "MaterialFilter_WindowHeader".Translate();

            // Measure the longest label for layout.
            float longestFilterName = 0f;
            for (int i = 0; i < filterDefs.Count; i++)
            {
                float w = Text.CalcSize(filterDefs[i].LabelCap).x;
                if (w > longestFilterName)
                    longestFilterName = w;
            }

            float innerWidth = Math.Max(Text.CalcSize(headerText).x,
                                        longestFilterName + padding + lineHeight);
            float scrollWidth = indent + innerWidth
                + GUI.skin.verticalScrollbar.margin.left
                + GUI.skin.verticalScrollbar.fixedWidth
                + GUI.skin.verticalScrollbar.margin.right;

            var headerRect = new Rect(0, 0, scrollWidth, lineHeight);
            var stuffRect = new Rect(0f, 0f, innerWidth, lineHeight);
            var viewRect = new Rect(0f, 0f, innerWidth, filterDefs.Count * lineHeight);
            var scrollRect = new Rect(stuffRect.x, lineHeight, scrollWidth, rect.height - lineHeight);

            // Header
            TextAnchor prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, headerText);
            Text.Anchor = TextAnchor.UpperLeft;

            stuffRect.width = longestFilterName + padding;

            // White border
            Widgets.DrawMenuSection(scrollRect);
            scrollRect.x += 1f;
            scrollRect.width -= 2f;

            // Clear/Allow buttons
            var clearRect = new Rect(scrollRect.x + 1f, scrollRect.y + 1f,
                                     (scrollRect.width - 2f) / 2f, lineHeight);
            if (Widgets.ButtonText(clearRect, "ClearAll".Translate()))
            {
                SetAllowAll(false);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
            var allowRect = new Rect(clearRect.xMax + 1f, clearRect.y,
                                     scrollRect.xMax - 2f - clearRect.xMax, lineHeight);
            if (Widgets.ButtonText(allowRect, "AllowAll".Translate()))
            {
                SetAllowAll(true);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }

            // Scroll list
            scrollRect.y += lineHeight + 2f;
            scrollRect.height -= lineHeight + 3f;
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float curY = 0f;
            for (int i = 0; i < filterDefs.Count; i++)
            {
                var sdef = filterDefs[i];
                var labelRect = new Rect(indent, curY, longestFilterName + padding, lineHeight);
                Widgets.Label(labelRect, sdef.LabelCap);

                bool isAllowed = filter.Allows(sdef);
                bool prev = isAllowed;
                Widgets.Checkbox(indent + longestFilterName + padding, curY, ref isAllowed,
                                 lineHeight, false, true);
                if (isAllowed != prev)
                    filter.SetAllow(sdef, isAllowed);

                curY += lineHeight;
            }

            Text.Anchor = prevAnchor;
            Widgets.EndScrollView();

            // Resize window to fit content.
            windowRect.width = scrollWidth + 2 * Margin;
        }
    }
}
