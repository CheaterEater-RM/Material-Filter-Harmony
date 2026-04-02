using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MaterialFilter
{
    internal static class MaterialFilterUI
    {
        internal const float PopupGap = 8f;
        internal const float PopupHeight = 480f;
        internal const float MaterialIconSize = 22f;
        internal const float MaterialIconGap = 4f;

        private const float ScreenPadding = 12f;

        private static readonly Dictionary<ThingDef, MaterialIconData> MaterialIconCache =
            new Dictionary<ThingDef, MaterialIconData>();

        private sealed class MaterialIconData
        {
            internal Texture2D Texture;
            internal Material Material;
            internal Color Color;
            internal float Scale;
        }

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

        internal static bool TryDrawMaterialIcon(Rect rect, ThingDef materialDef)
        {
            if (materialDef == null)
                return false;

            MaterialIconData iconData;
            if (!MaterialIconCache.TryGetValue(materialDef, out iconData))
            {
                iconData = ResolveMaterialIcon(materialDef);
                MaterialIconCache[materialDef] = iconData;
            }

            if (iconData.Texture == null || iconData.Texture == BaseContent.BadTex)
                return false;

            Color previousColor = GUI.color;
            GUI.color = iconData.Color;
            Widgets.DrawTextureFitted(rect, iconData.Texture, iconData.Scale, iconData.Material);
            GUI.color = previousColor;
            return true;
        }

        private static MaterialIconData ResolveMaterialIcon(ThingDef materialDef)
        {
            var iconData = new MaterialIconData
            {
                Color = Color.white,
                Scale = 1f
            };

            try
            {
                Material material;
                Texture2D texture = Widgets.GetIconFor(materialDef, out material, materialDef);
                iconData.Texture = texture;
                iconData.Material = material;
                iconData.Color = material != null
                    ? Color.white
                    : materialDef.GetColorForStuff(materialDef);
                iconData.Scale = GenUI.IconDrawScale(materialDef);
            }
            catch (Exception ex)
            {
                Log.Warning("[Material Filter] Failed to resolve icon for "
                            + materialDef.defName + ": " + ex.Message);
            }

            return iconData;
        }

        internal static string GetMaterialTooltip(ThingDef materialDef, SpecialThingFilterDef filterDef)
        {
            if (materialDef == null)
                return filterDef?.LabelCap.ToString() ?? string.Empty;

            string label = materialDef.LabelCap.ToString();
            if (materialDef.description.NullOrEmpty())
                return label;

            return label + "\n\n" + materialDef.description;
        }
    }
}