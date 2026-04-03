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
        internal const float MaterialRowHeight = 28f;
        internal const float MaterialIconInset = 2f;
        internal const float MaterialIconSize = MaterialRowHeight - MaterialIconInset * 2f;
        internal const float MaterialIconGap = 4f;

        private const float ScreenPadding = 12f;

        private static readonly Dictionary<ThingDef, MaterialIconData> MaterialIconCache =
            new Dictionary<ThingDef, MaterialIconData>();

        private sealed class MaterialIconData
        {
            internal Texture Texture;
            internal Material Material;
            internal Color Color;
            internal float Angle;
            internal Vector2 IconProportions;
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
            Widgets.DrawTextureFitted(
                rect,
                iconData.Texture,
                1f,
                iconData.IconProportions,
                new Rect(0f, 0f, 1f, 1f),
                iconData.Angle,
                iconData.Material);
            GUI.color = previousColor;
            return true;
        }

        private static MaterialIconData ResolveMaterialIcon(ThingDef materialDef)
        {
            var iconData = new MaterialIconData
            {
                Color = Color.white,
                IconProportions = Vector2.one
            };

            try
            {
                Thing previewThing = ThingMaker.MakeThing(materialDef);
                previewThing.stackCount = Math.Max(1, materialDef.stackLimit);
                float angle;
                Vector2 iconProportions;
                Color color;
                Material material;
                Texture texture = Widgets.GetIconFor(
                    previewThing,
                    new Vector2(MaterialIconSize, MaterialIconSize),
                    null,
                    false,
                    out _,
                    out angle,
                    out iconProportions,
                    out color,
                    out material);

                iconData.Texture = texture;
                iconData.Material = material;
                iconData.Color = color;
                iconData.Angle = angle;
                iconData.IconProportions = iconProportions;
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