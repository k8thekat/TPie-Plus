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
    public class GameMacroElementWindow : RingElementWindow
    {
        private GameMacroElement? _gameMacroElement = null;
        public GameMacroElement? GameMacroElement
        {
            get => _gameMacroElement;
            set
            {
                _gameMacroElement = value;

                _inputText = value != null ? value.Name : "";
                _macroId = value != null ? value.Identifier : 0;
                _iconInputText = value != null ? $"{value.IconID}" : "";
            }
        }

        protected override RingElement? Element
        {
            get => GameMacroElement;
            set => GameMacroElement = value is GameMacroElement o ? o : null;
        }

        protected int _macroId = 0;
        protected string _iconInputText = "";

        private string[] _macroIds = null!;

        public GameMacroElementWindow(string name) : base(name)
        {
            _macroIds = new string[100];
            for (int i = 0; i < 100; i++)
            {
                _macroIds[i] = $"{i}";
            }
        }

        public override void Draw()
        {
            if (GameMacroElement == null) return;

            ImGui.PushItemWidth(210 * _scale);

            // name
            FocusIfNeeded();
            if (ImGui.InputText("Name ##GameMacro", ref _inputText, 100))
            {
                GameMacroElement.Name = _inputText;
            }

            // id
            ImGui.PushItemWidth(100 * _scale);
            if (ImGui.Combo("ID", ref _macroId, _macroIds, _macroIds.Length))
            {
                GameMacroElement.Identifier = _macroId;
            }

            ImGui.SameLine();
            ImGui.Checkbox("Shared", ref GameMacroElement.IsShared);

            ImGui.NewLine();
            ImGui.NewLine();

            // icon id
            ImGui.PushItemWidth(154 * _scale);
            string str = _iconInputText;
            if (ImGui.InputText("Icon ID ##GameMacro", ref str, 100, ImGuiInputTextFlags.CharsDecimal))
            {
                _iconInputText = Regex.Replace(str, @"[^\d]", "");

                try
                {
                    GameMacroElement.IconID = uint.Parse(_iconInputText);
                }
                catch
                {
                    GameMacroElement.IconID = 0;
                }
            }

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf2f9"))
            {
                GameMacroElement.IconID = 66001;
                _iconInputText = "66001";
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("Reset to default");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Search.ToIconString()))
            {
                Plugin.ShowIconBrowserWindow(GameMacroElement.IconID, (iconId) =>
                {
                    GameMacroElement.IconID = iconId;
                    _iconInputText = $"{iconId}";
                });
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("Search ");

            ImGui.NewLine();

            // icon
            if (GameMacroElement.IconID > 0)
            {
                ISharedImmediateTexture? texture = TexturesHelper.GetTextureFromIconId(GameMacroElement.IconID);
                if (texture != null)
                {
                    ImGui.SetCursorPosX(110 * _scale);
                    ImGui.Image(texture.GetWrapOrEmpty().ImGuiHandle, new Vector2(80 * _scale));
                }
            }

            // draw text
            ImGui.NewLine();
            ImGui.Checkbox("Draw Text", ref GameMacroElement.DrawText);

            if (GameMacroElement.DrawText)
            {
                ImGui.SameLine();
                ImGui.Checkbox("Only When Selected", ref GameMacroElement.DrawTextOnlyWhenSelected);
            }

            // border
            ImGui.NewLine();
            GameMacroElement.Border.Draw();

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
                Ring?.Items.Remove(GameMacroElement);
            ImGui.PopFont();
            DrawHelper.SetTooltip("Delete.");

        }
    }
}
