using System;
using UnityEngine;
using Verse;

namespace MaterialFilter
{
    internal static class MaterialFilterUI
    {
        internal const float PopupGap = 8f;

        private const float ScreenPadding = 12f;

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