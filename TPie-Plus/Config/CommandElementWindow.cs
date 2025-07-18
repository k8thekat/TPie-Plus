using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using DelvUI.Helpers;
using ImGuiNET;
using TPie_Plus.Helpers;
using TPie_Plus.Models.Elements;

namespace TPie_Plus.Config
{
    public class CommandElementWindow : RingElementWindow
    {
        private CommandElement? _commandElement = null;
        public CommandElement? CommandElement
        {
            get => _commandElement;
            set
            {
                _commandElement = value;

                _inputText = value != null ? value.Name : "";
                _commandInputText = value != null ? value.Command : "";
                _iconInputText = value != null ? $"{value.IconID}" : "";
            }
        }

        protected override RingElement? Element
        {
            get => CommandElement;
            set => CommandElement = value is CommandElement o ? o : null;
        }

        protected string _commandInputText = "";
        protected string _iconInputText = "";

        public CommandElementWindow(string name) : base(name)
        {

        }

        public override void Draw()
        {
            if (CommandElement == null) return;

            ImGui.PushItemWidth(210 * _scale);

            // name
            FocusIfNeeded();
            if (ImGui.InputText("Name ##Command", ref _inputText, 100))
            {
                CommandElement.Name = _inputText;
            }

            // command
            if (ImGui.InputText("Command ##Command", ref _commandInputText, 100))
            {
                CommandElement.Command = _commandInputText;
            }

            ImGui.NewLine();
            ImGui.NewLine();

            // icon id
            ImGui.PushItemWidth(154 * _scale);
            string str = _iconInputText;
            if (ImGui.InputText("Icon ID ##Command", ref str, 100, ImGuiInputTextFlags.CharsDecimal))
            {
                _iconInputText = Regex.Replace(str, @"[^\d]", "");

                try
                {
                    CommandElement.IconID = uint.Parse(_iconInputText);
                }
                catch
                {
                    CommandElement.IconID = 0;
                }
            }

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf2f9"))
            {
                CommandElement.IconID = 66001;
                _iconInputText = "66001";
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("Reset to default");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Search.ToIconString()))
            {
                Plugin.ShowIconBrowserWindow(CommandElement.IconID, (iconId) =>
                {
                    CommandElement.IconID = iconId;
                    _iconInputText = $"{iconId}";
                });
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("Search ");

            ImGui.NewLine();

            // icon
            if (CommandElement.IconID > 0)
            {
                ISharedImmediateTexture? texture = TexturesHelper.GetTextureFromIconId(CommandElement.IconID);
                if (texture != null)
                {
                    ImGui.SetCursorPosX(110 * _scale);
                    ImGui.Image(texture.GetWrapOrEmpty().ImGuiHandle, new Vector2(80 * _scale));
                }
            }

            // draw text
            ImGui.NewLine();
            ImGui.Checkbox("Draw Text", ref CommandElement.DrawText);

            if (CommandElement.DrawText)
            {
                ImGui.SameLine();
                ImGui.Checkbox("Only When Selected", ref CommandElement.DrawTextOnlyWhenSelected);
            }

            // border
            ImGui.NewLine();
            CommandElement.Border.Draw();

            // Padding between the previous elements and the buttons.
            ImGui.Dummy(new Vector2(0, ImGui.GetContentRegionAvail().Y - 65));
            // Manual close/save button instead of clicking the `X` for the window.
            ImGui.NewLine();
            if (ImGui.Button("Close", new Vector2(90, 30)))
                IsOpen = false;
            DrawHelper.SetTooltip("Close.");

            ImGui.SameLine(ImGui.GetWindowWidth() - 40);
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
                Ring?.Items.Remove(CommandElement);
            ImGui.PopFont();
            DrawHelper.SetTooltip("Delete.");

        }
    }
}
