using ImGuiNET;
using System.Numerics;
using TPie_Plus.Helpers;
using System;

namespace TPie_Plus.Models.Elements
{
    public class EmoteElement : RingElement
    {
        public string Name;
        public string Command;
        public bool DrawText;
        public bool DrawTextOnlyWhenSelected;
        private DateTime _drawTime = DateTime.MinValue; // Set's the DateTIme as close to `0` as possible.

        public EmoteElement(string name, string command, bool drawText, bool drawTextOnlyWhenSelected, uint iconId)
        {
            Name = name;
            Command = command;
            DrawText = drawText;
            DrawTextOnlyWhenSelected = drawTextOnlyWhenSelected;
            IconID = iconId;
        }

        public EmoteElement() : this("", "", false, false, 64062) { }

        public override string Description()
        {
            if (Name == null || Name.Length == 0) { return "Emote"; }

            return $"{Name} ({Command})";
        }

        public override void ExecuteAction()
        {
            ChatHelper.SendChatMessage(Command);
        }

        public override string InvalidReason()
        {
            return "Command format invalid";
        }

        public override bool IsValid()
        {
            return Command.StartsWith("/");
        }

        public override void Draw(Vector2 position, Vector2 size, float scale, bool selected, uint color, float alpha, bool tooltip, ImDrawListPtr drawList)
        {
            
            base.Draw(position, size, scale, selected, color, alpha, tooltip, drawList);

            // name
            if (!DrawText) { return; }

            if (!DrawTextOnlyWhenSelected || (DrawTextOnlyWhenSelected && selected))
            {
                DrawHelper.DrawOutlinedText($"{Name}", position, true, scale, drawList);
            }
        }
    }
}
