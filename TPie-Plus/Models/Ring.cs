using System;
using System.Collections.Generic;
using System.Drawing;
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

            // create window
            ImGuiHelpers.ForceNextWindowMainViewport();

            ImGui.SetNextWindowPos(pos, Previewing ? ImGuiCond.Always : ImGuiCond.Appearing);
            // This controls the window size (imagine an invisible box around the ring)
            ImGui.SetNextWindowSize((radius * 1.25f) + (margin * 2), ImGuiCond.Always);
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
            // Radius is Ring.Radius
            float r = Radius - ItemSize;
            // center is the position the Ring opens at (Mouse Pos ? CenterScreen).
            float distanceToCenter = (mousePos - center).Length();
            _selectedIndex = -1;

            #region Icon layout
            // This determines the Ring/Icon spacing around the circle.
            // Also determines if an Icon is moused over or not.
            double offsetAngle = (double)360 / count;

            // This is in Radians.
            double calcAngle = offsetAngle * (Math.PI / 180f);

            // Radius is the size of the Ring
            Vector2 startPos = new Vector2(0, Radius - ItemSize);

            // This houses all the Icon position data in an array.
            Vector2[] itemPositions;
            itemPositions = IconLayout(calcAngle, count, startPos, center);

            // This houses scale of the Icons in an array.
            float[] itemScales = new float[count];

            if (_validItems != null)
            {


                // Mouse angle
                Vector2 direction = Vector2.Normalize(mousePos - center);
                // This is in Radians: Range=(+0 - (+3.14) || -0 - (-3.14)
                // This is the "angle" of the mouse in relation to the center. (from 9 o'clock position).
                double angle = Math.Atan2(direction.Y, direction.X) + Math.PI;

                // The *(180 / Math.PI) is to convert Radians to Degree's.
                // We + 90* because of the start point for calculating Radians is at the 9 o'clock position.
                // and the icons start at the 6 o'clock position.
                double mouseAngleDeg = ((double)angle * (180f / Math.PI)) + 90;

                // This would be the "rough index" into the Ring Items. 
                int mouseIdx = ((int)(mouseAngleDeg / offsetAngle) + count) % count;

                // This determines what Icon the mouse is closest to.
                // upperbound || lowerbound
                if (mouseAngleDeg > (mouseIdx * (int)offsetAngle) + (offsetAngle * .5f) - (offsetAngle * .20f) % 360f && mouseAngleDeg < (mouseIdx * (int)offsetAngle) + (offsetAngle * .5f) + (offsetAngle * .20f) % 360f)
                    mouseIdx = -1;

                else if (mouseAngleDeg > (mouseIdx * (int)offsetAngle) + (offsetAngle * .5f) + (offsetAngle * .21f) % 360f)
                    mouseIdx = (mouseIdx + 1) % count;

                #region BG Center Ring
                uint color = !Previewing && _selectedIndex >= 0 ? _baseColor : _lineColor;
                IDalamudTextureWrap? centerRing = Plugin.RingCenter?.GetWrapOrDefault();
                Vector2 ringSize = new Vector2(Radius * .27f);
                Vector2 arrowOffset = new Vector2(ringSize.X, 0); // Horizontal , Vertical
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
                            Vector2 arrowSize = new Vector2(ringSize.X * .6f, ringSize.Y * 1.0f);
                            direction = Vector2.Normalize(mousePos - center);
                            angle = Math.Atan2(direction.Y, direction.X);
                            DrawHelper.DrawRotation(center, arrowSize, angle, arrowOffset, drawList, arrowRing, color);
                        }

                    }
                }
                #endregion

                #region Quick Action
                // quick action
                if (QuickActionElement != null)
                {
                    if (_animState == AnimationState.Opened)
                    {

                        float scale = _animState != AnimationState.Opening && _animState != AnimationState.Closing && distanceToCenter > r * .50f ? 1f : Math.Clamp((Radius - distanceToCenter - (ItemSize * .25f)) * .007f, 1f, 2f);
                        // We are scaling the bounday box as the icon gets bigger/smaller relative to mouse distance to center.
                        Rectangle boundary = new Rectangle((int)(center.X - (ItemSize * scale) * .5f), (int)(center.Y - (ItemSize * scale) * .5f), (int)(ItemSize * scale), (int)(ItemSize * scale));
                        // _quickActionSelected = DrawSelectionBackground && !Previewing && boundary.Contains((int)mousePos.X, (int)mousePos.Y);
                        float alpha = _itemsAlpha.Length > 0 ? _itemsAlpha[0] : 1f;
                        // This prevents us from picking a Ring Item when inside our bounding box.
                        _quickActionSelected = boundary.Contains((int)mousePos.X, (int)mousePos.Y);
                        // Plugin.Logger.Debug($"Quick Action Selected: {_quickActionSelected}");
                        if (_quickActionSelected)
                        {
                            _quickActionSelected = true;
                            alpha = 1;
                            mouseIdx = -1;
                        }
                        uint selectionColor = alpha >= 1f ? _baseColor : 0;
                        QuickActionElement.Draw(center, new Vector2(ItemSize, ItemSize), scale, _quickActionSelected, selectionColor, alpha, ShowTooltips, drawList);
                    }

                }
                #endregion

                // If the mouse positon is further out. Just ignore the Ring.
                if (distanceToCenter > Radius * 1.25f)
                {
                    // Plugin.Logger.Debug($"Mouse outside Ring Boundary: {distanceToCenter > Radius * 1.25f}");
                    mouseIdx = -1;
                }

                // Selection handling
                _selectedIndex = mouseIdx;
                Plugin.Logger.Debug($"mouseIdx: {mouseIdx}");

                float iconRadius = (Radius * 1.25f) - Radius;
                if (mouseIdx != -1 && _animState == AnimationState.Opened)
                {
                    // calculate offsetPercent by mouse distance.
                    float distanceToIcon = (itemPositions[mouseIdx] - mousePos).Length();
                    float offsetPercent = distanceToIcon > iconRadius ? 0 : Math.Clamp(Math.Abs(iconRadius - distanceToIcon) * .003f, 0f, (count / Radius) * 9f);
                    // Adjusted the Icon spacing(offset) when moving a mouse towards an Icon.
                    AdjustedIconLayout(calcAngle, offsetPercent, mouseIdx, ref itemPositions, startPos, center);
                }

                for (int _ = 0; _ < count; _++)
                {
                    int idx = _;
                    // This controls the scale of the Icon when moving closer and further away with the mouse.
                    // This has the "mouse over" icon drawn last putting it on top.
                    if (mouseIdx != -1)
                        idx = (_ + mouseIdx + 1) % count;

                    itemScales[idx] = 1f;
                    if (idx == mouseIdx && _animState == AnimationState.Opened)
                    {
                        // This one is flipped to as we need the value to grow the closer we get.
                        float distanceToIcon = (itemPositions[mouseIdx] - mousePos).Length();
                        // scale up that icon specifically based upon the mouse distance to the center.
                        itemScales[idx] = distanceToIcon > iconRadius ? 1f : Math.Clamp(Math.Abs(iconRadius - distanceToIcon + (ItemSize * .5f)) * .025f, 1f, 2f);
                        // Plugin.Logger.Debug($"Scale: {itemScales[idx]} | disIcon: {distanceToIcon} | Status: {distanceToIcon > iconRadius} | math: {Math.Abs(iconRadius - distanceToIcon + (ItemSize * .5f)) * .025f}");
                    }

                    float scale = !Previewing && Plugin.Settings.AnimateIconSizes ? itemScales[idx] : 1f;
                    _validItems[idx].Draw(itemPositions[idx], new Vector2(ItemSize, ItemSize), scale, idx == mouseIdx, _baseColor, _itemsAlpha[idx], ShowTooltips, drawList);
                }



            }
            #endregion



            ImGui.End();
            ImGui.PopStyleVar();
        }

        /// <summary>
        /// Set's the Vector2 positions of each icon based upon the provided angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="count"></param>
        /// <param name="startPos"></param>
        /// <param name="centerPos"></param>
        /// <returns></returns>
        private Vector2[] IconLayout(double angle, int count, Vector2 startPos, Vector2 centerPos)
        {
            Vector2[] layout = new Vector2[count];

            for (int idx = 0; idx < count; idx++)
            {
                double sinA = Math.Sin(angle * idx);
                double cosA = Math.Cos(angle * idx);
                Vector2 iPos = DrawHelper.RotationPoint(startPos, cosA, sinA);

                layout[idx] = centerPos + iPos;
            }
            return layout;

        }

        /// <summary>
        /// Calculates the Icon spacing when moving the mouse near an Icon.
        /// </summary>
        /// <param name="angle">The original offset angle.</param>
        /// <param name="offsetPercent">The percentage of the angle to adjust the icons by.</param>
        /// <param name="curIdx">The current Icon/Item that is the mouse is near/over.</param>
        /// <param name="curLayout">The current Icon/Items layout Vector2 array.</param>
        /// <param name="startPos">The start position the Icon/Items layout uses.</param>
        /// <param name="centerPos">The center position of the ring.</param>
        private void AdjustedIconLayout(double angle, float offsetPercent, int curIdx, ref Vector2[] curLayout, Vector2 startPos, Vector2 centerPos)
        {
            double offsetAngle = angle * offsetPercent;

            int offsetCount = (int)(curLayout.Count() * .125);

            for (int idx = 1; idx <= offsetCount; idx++)
            {
                double curOffsetAngle = (offsetAngle / offsetCount) * (offsetCount - idx + 1);
                double sinAup = Math.Sin(angle * (curIdx + idx) + curOffsetAngle);
                double cosAup = Math.Cos(angle * (curIdx + idx) + curOffsetAngle);
                Vector2 iPosup = DrawHelper.RotationPoint(startPos, cosAup, sinAup);
                curLayout[(curIdx + idx) % curLayout.Count()] = centerPos + iPosup;

                double sinAdwn = Math.Sin(angle * (curIdx - idx) - curOffsetAngle);
                double cosAdwn = Math.Cos(angle * (curIdx - idx) - curOffsetAngle);
                Vector2 iPosdwn = DrawHelper.RotationPoint(startPos, cosAdwn, sinAdwn);
                curLayout[((curIdx - idx) + curLayout.Count()) % curLayout.Count()] = centerPos + iPosdwn;


            }

        }
        private Vector2 ValidatedPosition(Vector2 pos)
        {
            Vector2 screenSize = ImGui.GetMainViewport().Size;
            return new Vector2(Math.Max(0, pos.X), Math.Min(screenSize.Y, pos.Y));
        }

        // private Vector2 ValidatedSize(Vector2 pos, Vector2 size)
        // {
        //     Vector2 endPos = ValidatedPosition(pos + size);
        //     return endPos - pos;
        // }

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
