using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DelvUI.Helpers;
using ImGuiNET;
using TPie_Plus.Helpers;
using TPie_Plus.Models;
using TPie_Plus.Models.Elements;

namespace TPie_Plus.Config
{
    public class RingSettingsWindow : Window
    {
        private Ring? _ring = null;
        public Ring? Ring
        {
            get => _ring;
            set
            {
                _selectedIndex = -1;
                _ring?.EndPreview();
                _ring = value;
            }
        }

        private int _selectedIndex = -1;
        private float _scale => ImGuiHelpers.GlobalScale;

        private Vector2 _windowPos = Vector2.Zero;
        private Vector2 ItemWindowPos => _windowPos + new Vector2(410 * _scale, 0);

        public RingSettingsWindow(string name) : base(name)
        {
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(400, 470);

            PositionCondition = ImGuiCond.Appearing;
        }

        public override void PreDraw()
        {
            if (Ring == null || !Plugin.Settings.Rings.Contains(Ring))
            {
                IsOpen = false;
            }
        }
        /// <summary>
        /// This is the Ring Settings Window
        /// </summary>
        public override void Draw()
        {
            if (Ring == null) return;

            _windowPos = ImGui.GetWindowPos();

            // ring preview
            Vector2 margin = new Vector2(20 * _scale);
            Vector2 ringCenter = _windowPos + new Vector2(Size!.Value.X * _scale + Ring.Radius + margin.X, Size!.Value.Y * _scale / 2f);
            Ring.Preview(ringCenter);

            float infoHeight = Ring.KeyBind.Toggle ? 190 : 164;

            // If Ring.Keybind pressed, close menu.
            if (KeyboardHelper.Instance.IsKeysPressed(Ring.KeyBind.Key))
                IsOpen = false;

            // info
            ImGui.BeginChild("##Ring_Info", new Vector2(384 * _scale, infoHeight * _scale), false);
            {
                // ImGui.PushItemWidth(310 * _scale);
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * .25f);
                if (ImGui.InputText("Name ##Ring_Info_Name", ref Ring.Name, 100))
                {
                    WotsitHelper.Instance?.Update();
                }
                ImGui.Dummy(new Vector2(0, 1));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 1));
                if (ImGui.Button(Ring.KeyBind.Description(), new Vector2(ImGui.GetContentRegionAvail().X * .15f, 28)))
                {
                    Plugin.ShowKeyBindWindow(ImGui.GetMousePos(), Ring);
                }
                DrawHelper.SetTooltip("Click to edit keybind.");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 4);
                ImGui.Text("Keybind");

                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * .15f);
                ImGui.DragFloat("Radius ##Ring_Info_Radius", ref Ring.Radius, 1, 150, 500, "%.0f");

                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * .15f);
                ImGui.DragFloat("Icon Size ##Ring_Info_ItemSize", ref Ring.ItemSize, 1, 10, 500, "%.0f");

                ImGui.Checkbox("Arrow", ref Ring.DrawLine);
                DrawHelper.SetTooltip("This will draw an arrow around the center ring following your mouse position.");

                ImGui.SameLine();
                ImGui.Checkbox("Tooltips", ref Ring.ShowTooltips);
                DrawHelper.SetTooltip("This will show a tooltip with a description of an element when hovering on top of it.");

                ImGui.SameLine();
                ImGui.Checkbox("Selection Background", ref Ring.DrawSelectionBackground);


                if (Ring.KeyBind.Toggle)
                {
                    ImGui.Checkbox("Only execute actions on click", ref Ring.PreventActionOnClose);
                    DrawHelper.SetTooltip("When enabled, hovering on a item and closing the ring will not execute the hovered action.");
                }

                ImGui.Dummy(new Vector2(0, 1));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 1));
                // Ring Center Color
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * .5f);
                Vector3 color = new Vector3(Ring.Color.X, Ring.Color.Y, Ring.Color.Z);
                if (ImGui.ColorEdit3("Color ##Ring_Info_Color", ref color))
                {
                    Ring.Color = new Vector4(color.X, color.Y, color.Z, 1);
                }
            }
            ImGui.EndChild();
            #region Items Table
            // items
            var flags = ImGuiTableFlags.RowBg |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.BordersOuter |
                ImGuiTableFlags.BordersInner |
                ImGuiTableFlags.ScrollY;

            float tableHeight = Ring.KeyBind.Toggle ? 242 : 268;

            // This is Table with the array/column of elements.
            if (ImGui.BeginTable("##Item_Table", 4, flags, new Vector2(ImGui.GetWindowWidth() - 60, tableHeight * _scale)))
            {
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, ImGui.GetWindowWidth() * .075f, 0);
                // This is the Icon Column
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() * .045f, 1);
                // This is the Quick Action Column
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() * .06f, 2);
                ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch, ImGui.GetWindowWidth() * .15f, 3);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();
                // for (int i = 0; i < 4; i++)
                //     Plugin.Logger.Debug($"Column Widths | {i} : {ImGui.GetColumnWidth(i)}");

                for (int i = 0; i < Ring.Items.Count; i++)
                {
                    RingElement item = Ring.Items[i];

                    ImGui.PushID(i.ToString());
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                    // type
                    if (ImGui.TableSetColumnIndex(0))

                    {
                        if (ImGui.Selectable(item.UserFriendlyName(), _selectedIndex == i, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0, 27)))
                        {
                            _selectedIndex = i;
                            // Only open the editor if we "double click".
                            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                                ShowEditItemWindow();
                        }
                        DrawHelper.SetTooltip($"Double click to edit {item.UserFriendlyName()}: {item.Description()}");
                    }

                    #region Icon
                    // icon
                    if (ImGui.TableSetColumnIndex(1))
                    {
                        ISharedImmediateTexture? texture = TexturesHelper.GetTextureFromIconId(item.IconID, item.isHQ());
                        if (texture != null)
                        {
                            ImGui.Image(texture.GetWrapOrEmpty().ImGuiHandle, new Vector2(ImGui.GetColumnWidth(1)));
                        }
                    }
                    #endregion

                    #region Quick Action
                    // Quick Action Column Index 2
                    // TODO(@k8thekat): Figure out better math for the Checkbox positioning. (Currently working)
                    if (ImGui.TableSetColumnIndex(2))
                    {
                        if (item is not NestedRingElement)
                        {

                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 1);

                            bool active = Ring.QuickActionIndex == i;
                            if (ImGui.Checkbox("", ref active))
                            {
                                Ring.QuickActionIndex = active ? i : -1;
                            }
                        }
                        DrawHelper.SetTooltip($"Set {item.Description()} as the quick action, only one is allowed.");
                    }
                    #endregion
                    #region Description
                    // description
                    if (ImGui.TableSetColumnIndex(3))
                    {
                        bool valid = item.IsValid();
                        Vector4 c = valid ? Vector4.One : new(1, 0, 0, 1);
                        ImGui.TextColored(c, item.Description());

                        if (!valid)
                        {
                            DrawHelper.SetTooltip(item.InvalidReason());
                        }
                    }
                    #endregion

                    // Bugfix for Push/PopID Error.
                    ImGui.PopID();
                }

                ImGui.EndTable();
                #endregion Items Table
            }

            float buttonsStartY = Ring.KeyBind.Toggle ? infoHeight + 50 : infoHeight + 66;

            ImGui.SetCursorPos(new Vector2(369 * _scale, buttonsStartY * _scale));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
            {
                ImGui.OpenPopup("##TPie_Add_Item_Menu");
            }
            ImGui.PopFont();
            DrawHelper.SetTooltip("Add");

            if (_selectedIndex >= 0)
            {
                ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 30) * _scale));
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Pen.ToIconString()))
                {
                    ShowEditItemWindow();
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("Edit");

                ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 60) * _scale));
                ImGui.PushFont(UiBuilder.IconFont);

                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
                {
                    Ring.Items.RemoveAt(_selectedIndex);

                    if (Ring.QuickActionIndex == _selectedIndex)
                    {
                        Ring.QuickActionIndex = -1;
                    }
                    _selectedIndex = Ring.Items.Count - 1;
                }
                ImGui.PopFont();
                DrawHelper.SetTooltip("Delete");

                int count = Ring.Items.Count;
                if (count > 0)
                {
                    ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 150) * _scale));
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.ArrowUp.ToIconString()))
                    {
                        var tmp = Ring.Items[_selectedIndex];
                        bool moveQuickActionIndex = _selectedIndex == Ring.QuickActionIndex;

                        // circular?
                        if (_selectedIndex == 0)
                        {
                            Ring.Items.Remove(tmp);
                            Ring.Items.Add(tmp);
                            _selectedIndex = count - 1;
                        }
                        else
                        {
                            Ring.Items[_selectedIndex] = Ring.Items[_selectedIndex - 1];
                            Ring.Items[_selectedIndex - 1] = tmp;
                            _selectedIndex--;
                        }

                        if (moveQuickActionIndex)
                        {
                            Ring.QuickActionIndex = _selectedIndex;
                        }
                    }
                    ImGui.PopFont();
                    DrawHelper.SetTooltip("Move up");

                    ImGui.SetCursorPos(new Vector2(369 * _scale, (buttonsStartY + 180) * _scale));
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.ArrowDown.ToIconString()))
                    {
                        var tmp = Ring.Items[_selectedIndex];
                        bool moveQuickActionIndex = _selectedIndex == Ring.QuickActionIndex;

                        // circular?
                        if (_selectedIndex == count - 1)
                        {
                            Ring.Items.Remove(tmp);
                            Ring.Items.Insert(0, tmp);
                            _selectedIndex = 0;
                        }
                        else
                        {
                            Ring.Items[_selectedIndex] = Ring.Items[_selectedIndex + 1];
                            Ring.Items[_selectedIndex + 1] = tmp;
                            _selectedIndex++;
                        }

                        if (moveQuickActionIndex)
                        {
                            Ring.QuickActionIndex = _selectedIndex;
                        }
                    }
                    ImGui.PopFont();
                    DrawHelper.SetTooltip("Move down");
                }
            }

            DrawAddItemMenu();
        }

        private void DrawAddItemMenu()
        {
            if (Ring == null) return;

            ImGui.SetNextWindowSize(new(94 * _scale, 150 * _scale));

            if (ImGui.BeginPopup("##TPie_Add_Item_Menu"))
            {
                RingElement? elementToAdd = null;

                if (ImGui.Selectable("Action"))
                {
                    elementToAdd = new ActionElement();
                }

                if (ImGui.Selectable("Item"))
                {
                    elementToAdd = new ItemElement();
                }

                if (ImGui.Selectable("Gear Set"))
                {
                    elementToAdd = new GearSetElement();
                }

                if (ImGui.Selectable("Command"))
                {
                    elementToAdd = new CommandElement();
                }

                if (ImGui.Selectable("Game Macro"))
                {
                    elementToAdd = new GameMacroElement();
                }

                if (ImGui.Selectable("Emote"))
                {

                    elementToAdd = new EmoteElement();
                }

                if (ImGui.Selectable("Nested Ring"))
                {
                    elementToAdd = new NestedRingElement();
                }

                if (elementToAdd != null)
                {
                    if (Ring.Items.Count > 0 && _selectedIndex >= 0 && _selectedIndex < Ring.Items.Count - 1)
                    {
                        Ring.Items.Insert(_selectedIndex + 1, elementToAdd);
                        _selectedIndex++;
                    }
                    else
                    {
                        Ring.Items.Add(elementToAdd);
                        _selectedIndex = Ring.Items.Count - 1;
                    }

                    // This dispatches the "selected" Element window to be "displayed".
                    // They are using the `_selectedIndex` tied to the `Ring.Items`
                    // to determine which window to show.
                    ShowEditItemWindow();
                }

                ImGui.EndPopup();
            }
        }

        private void ShowEditItemWindow()
        {
            if (Ring == null || _selectedIndex < 0 || _selectedIndex >= Ring.Items.Count) return;

            RingElement element = Ring.Items[_selectedIndex];
            Plugin.ShowElementWindow(ItemWindowPos, Ring, element);
        }

        public override void OnClose()
        {
            Ring = null;
            _selectedIndex = -1;

            Settings.Save(Plugin.Settings);
        }

        private static string UserFriendlyString(string str, string? remove)
        {
            string? s = remove != null ? str.Replace(remove, "") : str;

            Regex? regex = new(@"
                    (?<=[A-Z])(?=[A-Z][a-z]) |
                    (?<=[^A-Z])(?=[A-Z]) |
                    (?<=[A-Za-z])(?=[^A-Za-z])",
                RegexOptions.IgnorePatternWhitespace);

            return regex.Replace(s, " ");
        }
    }
}
