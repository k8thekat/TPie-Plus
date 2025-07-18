using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using DelvUI.Helpers;
using ImGuiNET;
using Lumina.Excel;
using TPie_Plus.Helpers;
using TPie_Plus.Models.Elements;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace TPie_Plus.Config
{
    public class ActionElementWindow : RingElementWindow
    {
        private ActionElement? _actionElement = null;
        public ActionElement? ActionElement
        {
            get => _actionElement;
            set
            {
                _actionElement = value;
                _inputText = "";
                _searchResult.Clear();

                if (value != null && value.Data.HasValue)
                {
                    _inputText = value.Data.Value.Name.ToString();
                    _searchResult.Add(value.Data.Value);
                }
            }
        }

        protected override RingElement? Element
        {
            get => ActionElement;
            set => ActionElement = value is ActionElement o ? o : null;
        }

        private List<LuminaAction> _searchResult = new List<LuminaAction>();
        private ExcelSheet<LuminaAction>? _sheet;

        public ActionElementWindow(string name) : base(name)
        {
            _sheet = Plugin.DataManager.GetExcelSheet<LuminaAction>();
        }

        public override void Draw()
        {
            if (ActionElement == null) return;

            ImGui.PushItemWidth(210 * _scale);
            if (ImGui.InputText("ID or Name ##Action", ref _inputText, 100))
            {
                SearchActions(_inputText);
            }

            FocusIfNeeded();
            ImGui.BeginChild("##Actions_List", new Vector2(284 * _scale, 200 * _scale), true); // X, Y (Horizontal, Vertical)
            {
                foreach (LuminaAction data in _searchResult)
                {
                    // name
                    if (ImGui.Selectable($"\t\t\t{data.Name} (ID: {data.RowId})", false, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0, 24 * _scale)))
                    {
                        Plugin.Logger.Debug($"Selected: {data.Name} | Hovered: {ImGui.IsItemHovered()} | Double Click: {ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)}");
                        ActionElement.ActionID = data.RowId;
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            IsOpen = false;
                    }

                    // icon
                    ISharedImmediateTexture? texture = TexturesHelper.GetTextureFromIconId(data.Icon);
                    if (texture != null)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(10 * _scale);
                        ImGui.Image(texture.GetWrapOrEmpty().ImGuiHandle, new Vector2(24 * _scale));
                    }
                }
            }
            ImGui.EndChild();

            // border settings
            ImGui.NewLine();
            ActionElement.Border.Draw();

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
                Ring?.Items.Remove(ActionElement);
            ImGui.PopFont();
            DrawHelper.SetTooltip("Delete.");


        }



        private void SearchActions(string text)
        {
            if (_inputText.Length == 0 || _sheet == null)
            {
                _searchResult.Clear();
                return;
            }

            int intValue = 0;
            try
            {
                intValue = int.Parse(text);
            }
            catch { }

            if (intValue > 0)
            {
                _searchResult = _sheet.Where(row => row.RowId == intValue).ToList();
                return;
            }

            _searchResult = _sheet.Where(row => row.Name.ToString().ToUpper().Contains(text.ToUpper())).ToList();
        }
    }
}
