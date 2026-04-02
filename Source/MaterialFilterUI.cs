using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace MaterialFilter
{
    internal static class MaterialFilterUI
    {
        internal const float PopupGap = 8f;
        internal const float PopupHeight = 480f;

        private const float ScreenPadding = 12f;

        internal static float GetPopupWidth()
        {
            return Math.Min(300f, Math.Max(1f, UI.screenWidth - 300f));
        }

        internal static float GetButtonWidth(string label, float minWidth)
        {
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Small;
            float width = Math.Max(minWidth, Text.CalcSize(label).x + 20f);
            Text.Font = previousFont;
            return width;
        }

        internal static Vector2 GetPopupPosition(Rect hostRect, Rect anchorRect)
        {
            float popupWidth = GetPopupWidth();
            float popupX = hostRect.xMax + PopupGap;
            float popupY = anchorRect.y;
            float leftCandidate = hostRect.x - popupWidth - PopupGap;

            if (popupX + popupWidth > UI.screenWidth - ScreenPadding && leftCandidate >= ScreenPadding)
            {
                popupX = leftCandidate;
            }

            if (popupY + PopupHeight > UI.screenHeight - ScreenPadding)
            {
                popupY = Math.Min(anchorRect.y, hostRect.yMax - PopupHeight);
            }

            Rect popupRect = ClampToScreen(new Rect(popupX, popupY, popupWidth, PopupHeight));
            return new Vector2(popupRect.x, popupRect.y);
        }

        internal static Rect ClampToScreen(Rect rect)
        {
            float maxX = Math.Max(ScreenPadding, UI.screenWidth - rect.width - ScreenPadding);
            float maxY = Math.Max(ScreenPadding, UI.screenHeight - rect.height - ScreenPadding);

            rect.x = Mathf.Clamp(rect.x, ScreenPadding, maxX);
            rect.y = Mathf.Clamp(rect.y, ScreenPadding, maxY);
            return rect;
        }
    }
}