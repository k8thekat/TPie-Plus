using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using DelvUI.Helpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace TPie_Plus.Helpers
{
    internal static class DrawHelper
    {
        public static void DrawIcon(uint iconId, bool hq, Vector2 position, Vector2 size, float alpha, ImDrawListPtr drawList)
        {
            ISharedImmediateTexture? texture = TexturesHelper.GetTextureFromIconId(iconId, hq);
            if (texture == null) return;

            uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, alpha));
            drawList.AddImage(texture.GetWrapOrEmpty().ImGuiHandle, position, position + size, Vector2.Zero, Vector2.One, color);
        }

        public static void DrawOutlinedText(string text, Vector2 pos, bool centered, float scale, ImDrawListPtr drawList, int thickness = 1)
        {
            DrawOutlinedText(text, pos, centered, scale, 0xFFFFFFFF, 0xFF000000, drawList, thickness);
        }
        public static void DrawOutlinedText(string text, Vector2 pos, bool centered, float scale, uint color, uint outlineColor, ImDrawListPtr drawList, int thickness = 1)
        {
            FontsHelper.PushFont(scale);

            try
            {
                if (centered)
                {
                    Vector2 size = ImGui.CalcTextSize(text);
                    pos = pos - size / 2f;
                }

                // outline
                for (int i = 1; i < thickness + 1; i++)
                {
                    drawList.AddText(new Vector2(pos.X - i, pos.Y + i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X, pos.Y + i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X + i, pos.Y + i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X - i, pos.Y), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X + i, pos.Y), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X - i, pos.Y - i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X, pos.Y - i), outlineColor, text);
                    drawList.AddText(new Vector2(pos.X + i, pos.Y - i), outlineColor, text);
                }

                // text
                drawList.AddText(new Vector2(pos.X, pos.Y), color, text);
            }
            finally
            {
                FontsHelper.PopFont();
            }
        }

        public static void DrawCooldown(ActionType type, uint id, Vector2 position, Vector2 size, float scale, ImDrawListPtr drawList)
        {
            // arc
            float elapsed = CooldownHelper.GetRecastTimeElapsed(type, id);
            float total = CooldownHelper.GetRecastTime(type, id);
            float completion = 1 - (elapsed / total);
            float endAngle = (float)Math.PI * 2f * -completion;
            float offset = (float)Math.PI / 2;

            uint color = 0xCC000000;

            if (type == ActionType.Action && CooldownHelper.GetMaxCharges(id) > 1 && CooldownHelper.GetCharges(id) > 0)
            {
                color = 0x66000000;
            }

            ImGui.PushClipRect(position - size / 2, position + size / 2, false);
            drawList.PathArcTo(position, size.X / 2, endAngle - offset, -offset, 50);
            drawList.PathStroke(color, ImDrawFlags.None, size.X);
            ImGui.PopClipRect();

            // text
            if (elapsed > 0)
            {
                DrawOutlinedText($"{Math.Truncate(total - elapsed)}", position, true, scale, drawList);
            }
        }

        public static void SetTooltip(string message)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(message);
            }
        }

        public static (bool, bool) DrawConfirmationModal(string title, params string[] textLines)
        {
            bool didConfirm = false;
            bool didClose = false;

            ImGui.OpenPopup(title);

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            bool p_open = true; // i've no idea what this is used for

            if (ImGui.BeginPopupModal(title, ref p_open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            {
                float width = 300;
                float height = Math.Min((ImGui.CalcTextSize(" ").Y + 5) * textLines.Length, 240);

                ImGui.BeginChild("confirmation_modal_message", new Vector2(width, height), false);
                foreach (string text in textLines)
                {
                    ImGui.Text(text);
                }
                ImGui.EndChild();

                ImGui.NewLine();

                if (ImGui.Button("OK", new Vector2(width / 2f - 5, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didConfirm = true;
                    didClose = true;
                }

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(width / 2f - 5, 24)))
                {
                    ImGui.CloseCurrentPopup();
                    didClose = true;
                }

                ImGui.EndPopup();
            }
            // close button on nav
            else
            {
                didClose = true;
            }

            return (didConfirm, didClose);
        }

        public static Vector2 RotationPoint(Vector2 xy, double cosA, double sinA, bool invert = false)
        {
            double x;
            double y;
            if (invert == true)
            {
                x = xy.X * sinA - xy.Y * cosA;
                y = xy.X * cosA + xy.Y * sinA;
                return new Vector2((float)x, (float)y);
            }
            x = xy.X * cosA - xy.Y * sinA;
            y = xy.X * sinA + xy.Y * cosA;
            return new Vector2((float)x, (float)y);
        }
        /// <summary>
        /// Calculate's the "p1-p4" for ImDrawListPtr.AddImageQuad and draws the rotated image.
        /// </summary>
        /// <param name="origin">The source point for the image.</param>
        /// <param name="size">The overall size of the image.</param>
        /// <param name="angle">The current angle to draw the image at.</param>
        /// <param name="radius">The offset from the point of "origin".</param>
        public static void DrawRotation(Vector2 origin, Vector2 size, double angle, Vector2 radius, ImDrawListPtr drawList, IDalamudTextureWrap textureWrap, uint color)
        {
            double sinA = Math.Sin(angle);
            double cosA = Math.Cos(angle);
            origin = origin + RotationPoint(radius, cosA, sinA);
            Vector2[] pos =
            [
                origin + RotationPoint(new Vector2(+size.X * 0.5f, -size.Y * 0.5f), cosA, sinA),
                origin + RotationPoint(new Vector2(+size.X * 0.5f, +size.Y * 0.5f), cosA, sinA),
                origin + RotationPoint(new Vector2(-size.X * 0.5f, +size.Y * 0.5f), cosA, sinA),
                origin + RotationPoint(new Vector2(-size.X * 0.5f, -size.Y * 0.5f), cosA, sinA),
            ];
            Vector2 uv1 = new();
            Vector2 uv2 = new(1, 0);
            Vector2 uv3 = new(1, 1);
            Vector2 uv4 = new(0, 1);
            drawList.AddImageQuad(textureWrap.ImGuiHandle, p1: pos[0], p2: pos[1], p3: pos[2], p4: pos[3], uv1, uv2, uv3, uv4, color);

        }

    }
}
