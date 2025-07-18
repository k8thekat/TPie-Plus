﻿using Dalamud.Logging;
using System.Collections.Generic;
using TPie_Plus.Helpers;
using TPie_Plus.Models;
using TPie_Plus.Config;

namespace TPie_Plus
{
    public class RingsManager
    {
        private Ring? _activeRing = null;
        private int _activeRingIndex = 0;
        private bool _skipNextClosedCheck = false;

        private List<Ring> Rings => Plugin.Settings.Rings;

        public void ForceRing(Ring ring)
        {
            _activeRing = ring;
            _skipNextClosedCheck = true;
        }

        public void Update()
        {
            // Plugin.Logger.Debug("RingManager Update()");
            bool clearTmpKeyBinds = false;

            if (!_skipNextClosedCheck && _activeRing?.IsClosed() == true)
            {
                _activeRing = null;
                clearTmpKeyBinds = true;
            }
            _skipNextClosedCheck = false;

            if (_activeRing != null)
            {
                if (Plugin.Settings.EnableEscapeKeybind && 
                    _activeRing.KeyBind.Toggle &&
                    KeyboardHelper.Instance.IsEscapePressed())
                {
                    _activeRing.KeyBind.Deactivate();
                }
                else
                {
                    _activeRing?.Update();
                }
            }

            for (int i = 0; i < Rings.Count; i++)
            {
                if (clearTmpKeyBinds)
                {
                    Rings[i].SetTemporalKeybind(null);
                }

                if (_activeRing == null && Rings[i].Update())
                {
                    _activeRing = Rings[i];
                    _activeRingIndex = i;
                    break;
                }
                else if (Rings[i] != _activeRing)
                {
                    Rings[i].ForceClose();
                }
            }
        }

        public void Draw()
        {
            _activeRing?.Draw($"ring_{_activeRingIndex}");

            for (int i = 0; i < Rings.Count; i++)
            {
                if (_activeRingIndex == i) continue;
                if (!Rings[i].Previewing) continue;

                Rings[i].Draw($"ring_{i}");
            }
        }
    }
}
