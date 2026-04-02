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
        private readonly List<SpecialThingFilterDef> visibleFilterDefs;
        private string searchText = string.Empty;
        private Vector2 scrollPosition;

        // Cached from the pre-sorted list built at startup.
        private readonly List<SpecialThingFilterDef> filterDefs;

        protected override float Margin => 8f;

        public override Vector2 InitialSize => new Vector2(
            MaterialFilterUI.GetPopupWidth(),
            MaterialFilterUI.PopupHeight);

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
            visibleFilterDefs = new List<SpecialThingFilterDef>(filterDefs.Count);
            RebuildVisibleFilterDefs();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            windowRect = MaterialFilterUI.ClampToScreen(
                new Rect(left, top, InitialSize.x, InitialSize.y));
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

        private void RebuildVisibleFilterDefs()
        {
            visibleFilterDefs.Clear();

            string trimmedSearch = searchText.Trim();
            if (trimmedSearch.Length == 0)
            {
                visibleFilterDefs.AddRange(filterDefs);
                return;
            }

            for (int i = 0; i < filterDefs.Count; i++)
            {
                SpecialThingFilterDef filterDef = filterDefs[i];
                string labelText = filterDef.LabelCap.ToString();
                if (labelText.IndexOf(trimmedSearch, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    visibleFilterDefs.Add(filterDef);
                }
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            const float lineHeight = 25f;
            const float indent = 5f;
            const float padding = 5f;
            const float sectionBorder = 1f;
            const float controlsGap = 2f;
            const float searchClearButtonWidth = 24f;
            string headerText = "MaterialFilter_WindowHeader".Translate();
            string searchLabel = "MaterialFilter_SearchLabel".Translate();

            // Measure the longest label for layout.
            float longestFilterName = 0f;
            for (int i = 0; i < filterDefs.Count; i++)
            {
                float w = Text.CalcSize(filterDefs[i].LabelCap).x;
                if (w > longestFilterName)
                    longestFilterName = w;
            }

            float searchLabelWidth = Text.CalcSize(searchLabel).x + padding * 2f;
            float minSearchFieldWidth = 120f;
            float innerWidth = Math.Max(
                Text.CalcSize(headerText).x,
                Math.Max(
                    longestFilterName + padding + lineHeight,
                    searchLabelWidth + minSearchFieldWidth + searchClearButtonWidth + padding));
            float scrollWidth = indent + innerWidth
                + GUI.skin.verticalScrollbar.margin.left
                + GUI.skin.verticalScrollbar.fixedWidth
                + GUI.skin.verticalScrollbar.margin.right;

            var headerRect = new Rect(0, 0, scrollWidth, lineHeight);
            var contentRect = new Rect(0f, lineHeight, scrollWidth, rect.height - lineHeight);

            // Header
            TextAnchor prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, headerText);
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawMenuSection(contentRect);

            var controlsRect = new Rect(
                contentRect.x + sectionBorder,
                contentRect.y + sectionBorder,
                contentRect.width - 2f * sectionBorder,
                lineHeight * 2f + controlsGap + sectionBorder);

            // Clear/Allow buttons
            var clearRect = new Rect(
                controlsRect.x,
                controlsRect.y,
                (controlsRect.width - controlsGap) / 2f,
                lineHeight);
            if (Widgets.ButtonText(clearRect, "ClearAll".Translate()))
            {
                SetAllowAll(false);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
            var allowRect = new Rect(
                clearRect.xMax + controlsGap,
                clearRect.y,
                controlsRect.xMax - clearRect.xMax - controlsGap,
                lineHeight);
            if (Widgets.ButtonText(allowRect, "AllowAll".Translate()))
            {
                SetAllowAll(true);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }

            float clearButtonWidth = string.IsNullOrEmpty(searchText) ? 0f : searchClearButtonWidth + controlsGap;
            var searchLabelRect = new Rect(
                controlsRect.x,
                clearRect.yMax + controlsGap,
                searchLabelWidth,
                lineHeight);
            Widgets.Label(searchLabelRect, searchLabel);

            var searchFieldRect = new Rect(
                searchLabelRect.xMax,
                searchLabelRect.y,
                controlsRect.xMax - searchLabelRect.xMax - clearButtonWidth,
                lineHeight);
            string updatedSearchText = Widgets.TextField(searchFieldRect, searchText);
            if (!string.Equals(updatedSearchText, searchText, StringComparison.Ordinal))
            {
                searchText = updatedSearchText;
                scrollPosition.y = 0f;
                RebuildVisibleFilterDefs();
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                var clearSearchRect = new Rect(
                    searchFieldRect.xMax + controlsGap,
                    searchFieldRect.y,
                    searchClearButtonWidth,
                    lineHeight);
                if (Widgets.ButtonText(clearSearchRect, "X"))
                {
                    searchText = string.Empty;
                    scrollPosition.y = 0f;
                    RebuildVisibleFilterDefs();
                    GUI.FocusControl(null);
                }
            }

            // Scroll list
            var scrollRect = new Rect(
                contentRect.x + sectionBorder,
                controlsRect.yMax + controlsGap,
                contentRect.width - 2f * sectionBorder,
                contentRect.yMax - controlsRect.yMax - controlsGap - sectionBorder);
            var viewRect = new Rect(
                0f,
                0f,
                innerWidth,
                Math.Max(lineHeight, visibleFilterDefs.Count * lineHeight));
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float curY = 0f;
            for (int i = 0; i < visibleFilterDefs.Count; i++)
            {
                var sdef = visibleFilterDefs[i];
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

            if (visibleFilterDefs.Count == 0)
            {
                var noResultsRect = new Rect(indent, 0f, innerWidth - indent * 2f, lineHeight);
                Widgets.Label(noResultsRect, "MaterialFilter_NoResults".Translate());
            }

            Text.Anchor = prevAnchor;
            Widgets.EndScrollView();

            // Resize window to fit content.
            windowRect.width = scrollWidth + 2 * Margin;
            windowRect = MaterialFilterUI.ClampToScreen(windowRect);
        }
    }
}
