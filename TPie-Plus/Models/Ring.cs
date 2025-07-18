using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Utility.Numerics;
using ImGuiNET;
using TPie_Plus.Config;
using TPie_Plus.Helpers;
using TPie_Plus.Models.Elements;

namespace TPie_Plus.Models
{
    public class Ring
    {
        public string Name;
        public float Rotation;
        public float Radius;
        public float ItemSize;

        public KeyBind KeyBind;
        private KeyBind? _tmpKeyBind; // used for nested rings

        public bool DrawLine = true;
        public bool DrawSelectionBackground = true;
        public bool ShowTooltips = false;
        public bool PreventActionOnClose = false;

        private Vector4 _color = Vector4.One;
        public Vector4 Color
        {
            get => _color;
            set
            {
                _color = value;
                _baseColor = ImGui.ColorConvertFloat4ToU32(value);
                _lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(value.X, value.Y, value.Z, 0.5f));
            }
        }

        private uint _baseColor;
        private uint _lineColor;

        public List<RingElement> Items;
        public int QuickActionIndex = -1;

        private RingElement? QuickActionElement
        {
            get
            {
                if (QuickActionIndex < 0 || QuickActionIndex >= Items.Count) { return null; }

                RingElement quickAction = Items[QuickActionIndex];
                if (!quickAction.IsValid()) { return null; }

                return quickAction;
            }
        }

        public bool Previewing { get; private set; } = false;

        public bool IsActive { get; private set; } = false;
        public bool HasInventoryItems { get; private set; } = false;

        private List<RingElement> _validItems = null!;
        private int _previousCount = 0;

        private Vector2? _center = null;
        private int _selectedIndex = -1;
        private double _selectionStartTime = -1;
        private bool _quickActionSelected = false;
        private bool _canExecuteAction = true;

        private AnimationState _animState = AnimationState.Closed;
        private bool _animating = false;
        private double _animEndTime = -1;
        private double _animProgress = 0;
        private double _angleOffset = 0;
        private float[] _itemsDistanceScales = null!;
        private float[] _itemsAlpha = null!;

        // public Ring(string name, Vector4 color, KeyBind keyBind, float radius, Vector2 itemSize)
        public Ring(string name, Vector4 color, KeyBind keyBind, float radius, float itemSize)
        {
            Name = name;
            Color = color;
            KeyBind = keyBind;
            Radius = radius;
            ItemSize = itemSize;

            Items = new List<RingElement>();
        }

        public void Preview(Vector2 position)
        {
            _center = position;
            SetAnimState(AnimationState.Opened);
            Previewing = true;
        }

        public void EndPreview()
        {
            SetAnimState(AnimationState.Closed);
            Previewing = false;
        }

        public bool Update()
        {
            HasInventoryItems = Items.FirstOrDefault(item => item is ItemElement) != null;
            _validItems = Items.Where(o => o.IsValid() && o != QuickActionElement).ToList();

            if (_previousCount != _validItems.Count)
            {
                SetAnimState(_animState);
                _previousCount = _validItems.Count;
            }

            if (Previewing)
            {
                return true;
            }

            KeyBind currentKeyBind = CurrentKeybind();

            // click to select in toggle mode
            if (!Previewing &&
                currentKeyBind.Toggle &&
                ImGui.GetIO().MouseClicked[0] &&
                ((_selectedIndex >= 0 && _selectedIndex < _validItems.Count) || _quickActionSelected))
            {
                _canExecuteAction = true;
                currentKeyBind.Deactivate();
            }

            if (!currentKeyBind.IsActive())
            {
                IsActive = false;
                return false;
            }

            _canExecuteAction = !KeyBind.Toggle || !PreventActionOnClose;

            IsActive = _validItems.Count > 0;
            return IsActive;
        }
        /// <summary>
        /// This handles drawing of the Ring elements (Background, Icons, Arrow, etc..)
        /// </summary>
        public void Draw(string id)
        {
            if (!Previewing && CheckNestedRingSelection())
            {
                return;
            }

            if (!Previewing && !IsActive)
            {
                if (_canExecuteAction)
                {
                    if (_animState == AnimationState.Opened &&
                        _center != null &&
                        _selectedIndex >= 0 &&
                        _validItems != null &&
                        _selectedIndex < _validItems.Count)
                    {
                        _validItems[_selectedIndex].ExecuteAction();
                    }
                    else if ((_animState == AnimationState.Opened || _animState == AnimationState.Opening) &&
                        _center != null &&
                        _quickActionSelected)
                    {
                        QuickActionElement?.ExecuteAction();
                    }
                }

                if (_animState != AnimationState.Closing && _animState != AnimationState.Closed)
                {
                    SetAnimState(AnimationState.Closing);
                }
            }

            Vector2 mousePos = ImGui.GetMousePos();

            // detect start
            if (!Previewing && IsActive && (_animState == AnimationState.Closed || _animState == AnimationState.Closing))
            {
                if (_animState == AnimationState.Closed)
                {
                    _center = Plugin.Settings.AppearAtCursor ?
                        mousePos :
                        ImGui.GetMainViewport().Size / 2f + Plugin.Settings.CenterPositionOffset;

                    // move cursor?
                    if (!Plugin.Settings.AppearAtCursor && Plugin.Settings.AutoCenterCursor)
                    {
                        CursorHelper.SetCursorPosition(_center.Value);
                    }
                }

                SetAnimState(AnimationState.Opening);
            }

            // animate
            UpdateAnimation();

            if (_animState == AnimationState.Closed) return;

            int count = _validItems?.Count ?? 0;
            Vector2 center = _center!.Value;
            Vector2 margin = new Vector2(400, 400);
            Vector2 radius = new Vector2(Radius);
            Vector2 pos = ValidatedPosition(center - radius - margin);
            Vector2 size = ValidatedSize(pos, radius * 2 + margin * 2);

            // create window
            ImGuiHelpers.ForceNextWindowMainViewport();

            ImGui.SetNextWindowPos(pos, Previewing ? ImGuiCond.Always : ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(radius * 2 + margin * 2, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
            if (Previewing || (!Plugin.Settings.EnableQuickSettings && !CurrentKeybind().Toggle))
            {
                flags |= ImGuiWindowFlags.NoInputs;
            }

            if (!ImGui.Begin($"TPie_{id}", flags))
            {
                ImGui.End();
                ImGui.PopStyleVar();
                return;
            }

            // quick settings
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right))
            {
                Plugin.ShowRingSettingsWindowInCursor(this);
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();


            // bg
            if (Plugin.Settings.DrawRingBackground)
            {
                IDalamudTextureWrap? bg = Plugin.RingBackground?.GetWrapOrDefault();
                if (bg != null)
                {
                    Vector2 bgSize = new Vector2(Radius * 1.3f);
                    uint c = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, (float)_animProgress));
                    drawList.AddImage(bg.ImGuiHandle, center - bgSize, center + bgSize, Vector2.Zero, Vector2.One, c);
                }
            }



            // elements
            float r = Radius - ItemSize;// This is the Radius Minus the Icon size for the Math
            double step = (Math.PI * 2) / count; // This is calculating the degrees to space each icon out.

            float distanceToCenter = (mousePos - center).Length();
            if (distanceToCenter > r)
            {
                mousePos = center + Vector2.Normalize(mousePos - center) * r;
            }

            Vector2[] itemPositions = new Vector2[count];
            float[] itemScales = new float[count];

            float minDistance = float.MaxValue;

            int previousSelection = _selectedIndex;
            _selectedIndex = -1;


            // This determines the Ring/Icon spacing around the circle.
            // Also determines if an Icon is moused over or not.
            #region Icon layout
            // center is the position the Ring opens at (Mouse Pos ? CenterScreen).
            double calcAngle = (-360 / count) * (Math.PI / 180f);
            // radius is the size of the Ring
            Vector2 startPos = new(0, Radius - ItemSize);

            if (_validItems != null)

                for (int idx = 0; idx < count; idx++)
                {
                    if (idx >= count) break;

                    double sinA = Math.Sin(calcAngle * idx);
                    double cosA = Math.Cos(calcAngle * idx);
                    Vector2 iPos = DrawHelper.RotationPoint(startPos, cosA, sinA);
                    // Plugin.Logger.Debug($"Icon {idx} Angle: {calcAngle * idx} |  PositionX/Y: {center + iPos}");
                    itemPositions[idx] = center + iPos;

                    if (_animState == AnimationState.Opened)
                    {
                        // TODO(@k8thekat): When mousing over the Icon we could subtract a flat value or % from the angle value.
                        // This is determining the Mouse Position in relation to Icons for which "Icon"
                        // to scale up.
                        // -- Also allowing us to scale the icon.
                        // We can calculate the mouse pos via Cos/Sin angles and determine which Icon we are near(or at)
                        // -- Since we have the Radius and the known angles of the Icons.
                        float distance = (itemPositions[idx] - mousePos).Length();
                        if (distance < minDistance)
                        {
                            bool selected = distance <= ItemSize * 2;
                            _selectedIndex = selected ? idx : _selectedIndex;
                            minDistance = selected ? distance : minDistance;
                        }
                        // This math here is determining the Icon scale.
                        itemScales[idx] = distance > 200 ? 1f : Math.Clamp(2f - (distance * 2f / 200), 1f, 2f);
                        // Plugin.Logger.Debug($"Icon Scale: {itemScales[index]}");
                    }
                    else
                    {
                        itemScales[idx] = 1f;
                    }

                    // bool selected = DrawSelectionBackground && !Previewing && _animState == AnimationState.Opened;
                    float scale = !Previewing && Plugin.Settings.AnimateIconSizes ? itemScales[idx] : 1f;

                    // This is dispatching the Draw event for the Ring and the Icons
                    _validItems[idx].Draw(itemPositions[idx], new Vector2(ItemSize, ItemSize), scale, idx == _selectedIndex, _baseColor, _itemsAlpha[idx], ShowTooltips, drawList);

                }
            #endregion

            #region BG Center Ring
            uint color = !Previewing && _selectedIndex >= 0 ? _baseColor : _lineColor;
            IDalamudTextureWrap? centerRing = Plugin.RingCenter?.GetWrapOrDefault();
            Vector2 ringSize = new(Radius * .27f);
            Vector2 arrowOffset = new(ringSize.X, 0); // Horizontal , Vertical
            if (centerRing != null)
            {
                drawList.AddImage(centerRing.ImGuiHandle, center - ringSize, center + ringSize, Vector2.Zero, Vector2.One, color);
            }
            #endregion
            #region Arrow
            // Arrow
            if (DrawLine)
            {
                if (_animState == AnimationState.Opened)
                {

                    IDalamudTextureWrap? arrowRing = Plugin.RingArrow?.GetWrapOrDefault();
                    if (arrowRing != null)
                    {
                        Vector2 arrowSize = new(ringSize.X * .6f, ringSize.Y * 1.0f);
                        Vector2 direction = Vector2.Normalize(mousePos - center);
                        double angle = Math.Atan2(direction.Y, direction.X);
                        DrawHelper.DrawRotation(center, arrowSize, angle, arrowOffset, drawList, arrowRing, color);
                    }

                }
            }
            #endregion
            #region Quick Action
            // quick action
            if (QuickActionElement != null)
            {
                // _quickActionSelected = DrawSelectionBackground && _selectedIndex == -1 && !Previewing && distanceToCenter <= ItemSize.Y * 2;
                _quickActionSelected = DrawSelectionBackground && _selectedIndex == -1 && !Previewing && distanceToCenter <= ItemSize * 2;
                float alpha = _itemsAlpha.Length > 0 ? _itemsAlpha[0] : 1f;
                uint selectionColor = alpha >= 1f ? _baseColor : 0;
                float scale = !Previewing && Plugin.Settings.AnimateIconSizes && itemScales.Length > 0 && _quickActionSelected ? 2f : 1f;

                QuickActionElement.Draw(center, new Vector2(ItemSize, ItemSize), scale, _quickActionSelected, selectionColor, alpha, ShowTooltips, drawList);
            }

            if (previousSelection != _selectedIndex && _selectedIndex >= 0)
            {
                _selectionStartTime = ImGui.GetTime();
            }
            #endregion
            ImGui.End();
            ImGui.PopStyleVar();
        }

        private Vector2 ValidatedPosition(Vector2 pos)
        {
            Vector2 screenSize = ImGui.GetMainViewport().Size;
            return new Vector2(Math.Max(0, pos.X), Math.Min(screenSize.Y, pos.Y));
        }

        private Vector2 ValidatedSize(Vector2 pos, Vector2 size)
        {
            Vector2 endPos = ValidatedPosition(pos + size);
            return endPos - pos;
        }

        private bool CheckNestedRingSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _validItems.Count || _selectionStartTime == -1)
            {
                return false;
            }

            NestedRingElement? nestedRing = _validItems[_selectedIndex] as NestedRingElement;
            if (nestedRing == null || nestedRing.GetRing() == null)
            {
                return false;
            }

            if (!nestedRing.ClickToActivate)
            {
                double now = ImGui.GetTime();
                if (now - _selectionStartTime < nestedRing.ActivationTime)
                {
                    return false;
                }
            }
            else
            {
                if (!ImGui.GetIO().MouseClicked[0])
                {
                    return false;
                }
            }

            Ring? ring = nestedRing.GetRing();
            if (ring != null)
            {
                ring.SetTemporalKeybind(CurrentKeybind());
                Plugin.RingsManager?.ForceRing(ring);
                _selectionStartTime = -1;

                if (nestedRing.KeepCenter && _center.HasValue)
                {
                    CursorHelper.SetCursorPosition(_center.Value);
                }
            }

            return true;
        }

        #region keybind
        public void SetTemporalKeybind(KeyBind? keybind)
        {
            _tmpKeyBind = keybind;
        }

        private KeyBind CurrentKeybind()
        {
            // Plugin.Logger.Info($"Temp Keybind: {_tmpKeyBind} | Cur Keybind: {KeyBind} | Get One: {_tmpKeyBind ?? KeyBind}");
            return _tmpKeyBind ?? KeyBind;
        }
        #endregion

        #region anim

        public bool IsClosed()
        {
            return _animState == AnimationState.Closed;
        }

        public void ForceClose()
        {
            if (!Previewing && _animState != AnimationState.Closed && _animState != AnimationState.Closing)
            {
                SetAnimState(AnimationState.Closed);
            }
        }

        private void SetAnimState(AnimationState state)
        {
            float animDuration = Plugin.Settings.AnimationDuration;
            if (Plugin.Settings.AnimationType == RingAnimationType.None || animDuration == 0)
            {
                if (state == AnimationState.Opening) { state = AnimationState.Opened; }
                else if (state == AnimationState.Closing) { state = AnimationState.Closed; }
            }

            _animState = state;

            int count = _validItems?.Count ?? 0;
            _itemsDistanceScales = new float[count];
            _itemsAlpha = new float[count];

            // opened
            if (state == AnimationState.Opened || state == AnimationState.Closed)
            {
                _animEndTime = -1;
                _animProgress = state == AnimationState.Opened ? 1 : 0;
                _animating = false;
                _angleOffset = 0;

                for (int i = 0; i < count; i++)
                {
                    _itemsDistanceScales[i] = state == AnimationState.Opened ? 1f : 0f;
                    _itemsAlpha[i] = state == AnimationState.Opened ? 1f : 0f;
                }

                if (state == AnimationState.Closed)
                {
                    _center = null;
                }
                return;
            }


            double p = state == AnimationState.Opening ? 1 - _animProgress : _animProgress;
            _animEndTime = ImGui.GetTime() + (animDuration * p);

            _animating = true;
        }

        private void UpdateAnimation()
        {
            if (!_animating) { return; }

            double now = ImGui.GetTime();
            int count = _validItems?.Count ?? 0;

            float animDuration = Plugin.Settings.AnimationDuration;
            if (now > _animEndTime)
            {
                _animProgress = _animState == AnimationState.Opening ? 1 : 0;
            }
            else
            {
                _animProgress = Math.Min(1, (_animEndTime - now) / animDuration);
                if (_animState == AnimationState.Opening)
                {
                    _animProgress = 1 - _animProgress;
                }
            }

            RingAnimationType type = Plugin.Settings.AnimationType;

            // spiral
            if (type == RingAnimationType.Spiral)
            {
                _angleOffset = -1.6f * (1 - _animProgress);

                for (int i = 0; i < count; i++)
                {
                    _itemsDistanceScales[i] = (float)_animProgress;
                    _itemsAlpha[i] = (float)_animProgress;
                }
            }

            // sequential
            else if (type == RingAnimationType.Sequential)
            {
                _angleOffset = 0;

                for (int i = 0; i < count; i++)
                {
                    float start = i * (1f / count);
                    float end = (i + 1) * (1f / count);
                    float duration = end - start;

                    float p = 0;
                    if (_animProgress > start && _animProgress <= end)
                    {
                        p = ((float)_animProgress - start) / duration;
                    }
                    else
                    {
                        p = _animProgress < start ? 0f : 1f;
                    }

                    _itemsDistanceScales[i] = p;
                    _itemsAlpha[i] = p;
                }
            }

            // fade
            else if (type == RingAnimationType.Fade)
            {
                _angleOffset = 0;

                for (int i = 0; i < count; i++)
                {
                    _itemsDistanceScales[i] = 1f;
                    _itemsAlpha[i] = (float)_animProgress;
                }
            }

            if (now > _animEndTime)
            {
                AnimationState state = _animState == AnimationState.Opening ? AnimationState.Opened : AnimationState.Closed;
                SetAnimState(state);
            }
        }

        private enum AnimationState
        {
            Opening = 0,
            Opened = 1,
            Closing = 2,
            Closed = 3
        }
        #endregion
    }
}
