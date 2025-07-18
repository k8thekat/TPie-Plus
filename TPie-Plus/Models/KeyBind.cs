using Dalamud.Game.ClientState.Objects.SubKinds;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System;
using TPie_Plus.Helpers;

namespace TPie_Plus.Models
{
    public class KeyBind
    {
        public List<int> Key;
        // public int Key;
        // public bool Ctrl;
        // public bool Alt;
        // public bool Shift;

        public bool Toggle = false;

        public HashSet<uint> Jobs;
        public bool IsGlobal => Jobs.Count == 0 || Jobs.Count == JobsHelper.JobNames.Count;

        private bool _waitingForRelease;
        private bool _active;

        private DateTime _keyTime = DateTime.MinValue; // Set's the DateTIme as close to `0` as possible.

        public KeyBind(List<int> key) //, bool ctrl = false, bool alt = false, bool shift = false)
        {
            // Ctrl = ctrl;
            // Alt = alt;
            // Shift = shift;
            Key = key;
            Jobs = new HashSet<uint>();
        }

        public override string ToString()
        {
            // string ctrl = Ctrl ? "Ctrl + " : "";
            // string alt = Alt ? "Alt + " : "";
            // string shift = Shift ? "Shift + " : "";
            // string key = ((Keys)Key).ToString();
            string key = "";
            // Key = [162, 65] (LCRTL + A) Count = 2
            // key = "LCTRL + "
            // key = "LCTRL + A"
            for (int i = 0; i < Key.Count; i++)
            {
                key = string.Concat(key, ((Keys)Key[i]).ToString());
                
                if (i + 1 < Key.Count)
                    key = key + " + ";
            }

            return key;
            // return ctrl + alt + shift + key;
        }

        public string Description()
        {
            string toggleStringPrefix = Toggle ? "[" : "";
            string toggleStringSufix = Toggle ? "]" : "";
            string jobsString = "";

            if (Jobs.Count > 0)
            {
                List<string> sortedJobs = Jobs.Select(jobId => JobsHelper.JobNames[jobId]).ToList();
                sortedJobs.Sort();
                jobsString = " (" + string.Join(", ", sortedJobs.ToArray()) + ")";
            }

            return toggleStringPrefix + ToString() + toggleStringSufix + jobsString;
        }

        public bool IsActive()
        {
            if (ChatHelper.IsInputTextActive() == true || ImGui.GetIO().WantTextInput)
            {
                return Toggle ? _active : false;
            }

            ImGuiIOPtr io = ImGui.GetIO();
            
            // bool ctrl <- is a var
            // ? is a true/false check, with the `:` being the seperator of cases.
            // (if true return) : (if false return)
            // bool ctrl = Ctrl ? io.KeyCtrl : !io.KeyCtrl;
            // bool alt = Alt ? io.KeyAlt : !io.KeyAlt;
            // bool shift = Shift ? io.KeyShift : !io.KeyShift;
            // ------------------------------------------------
            // This is what is validating if the keybind is pressed.
            bool key = KeyboardHelper.Instance?.IsKeysPressed(Key) == true;
            // bool key = KeyboardHelper.Instance?.IsKeyPressed(Key) == true;
            
            // bool active = ctrl && alt && shift && key;
            bool active = key;

            // check job
            IPlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player != null && Jobs.Count > 0)
            {
                active &= Jobs.Contains(player.ClassJob.RowId);
            }

            // block keybind for the game?
            if (active && !Plugin.Settings.KeybindPassthrough)
            {
                try
                {
                    foreach (int t in Key)
                        Plugin.KeyState[t] = false;
                    // Plugin.KeyState[Key] = false;
                }
                catch { }
            }

            if (Toggle)
            {
                if (active && !_waitingForRelease)
                {
                    _active = !_active;
                    _waitingForRelease = true;
                }
                else if (!active)
                {
                    _waitingForRelease = false;
                }

                return _active;
            }

            return active;
        }

        public void Deactivate()
        {
            _active = false;
            _waitingForRelease = false;
        }

        public bool Draw(string id, float width)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            string dispKey = ToString();
            // var timeout = Task.Run(KeyboardHelper.GetKeyPressed());
            // var kBhelper = KeyboardHelper.Instance;
            
            ImGui.PushItemWidth(width);
            ImGui.InputText($"##{id}_Keybind", ref dispKey, 200, ImGuiInputTextFlags.ReadOnly);
            DrawHelper.SetTooltip("Hold the keys for ~1 sec, Use Backspace to clear.");

            if (ImGui.IsItemActive())
            {
                // Checking if `backspace` key has been pressed.
                if (KeyboardHelper.Instance?.IsKeyPressed((int)Keys.Back) == true)
                {
                    Reset();
                }
                else
                {
                    // {class}.Instance? is a parameter checking if the class exists or not. Think in terms of it returning `None` thus using the field after `??`
                    // Otherwise set `keyPressed` to `0`.
                    // int keyPressed = KeyboardHelper.Instance?.GetKeyPressed() ?? 0;
                    // if (keyPressed > 0)
                    // {
                    //     Ctrl = io.KeyCtrl;
                    //     Alt = io.KeyAlt;
                    //     Shift = io.KeyShift;
                    //     Key = keyPressed;
                    //     return true;
                    // }
                    List<int> keyPressed = KeyboardHelper.Instance?.GetKeysPressed() ?? [];
                    // Cant invert this check as we only want to exit with return true when multiple keys are pressed.
                    if (keyPressed.Count != 0)
                    {
                        // First key press will update _keyTime
                        if (_keyTime == DateTime.MinValue)
                            _keyTime = DateTime.Now;
                        // After .3 seconds we commit the currently held key(s).
                        else if (DateTime.Now.Subtract(_keyTime).TotalMilliseconds > 150)
                        {
                            // Plugin.Logger.Info($"Keys Pressed: {string.Join(",", keyPressed)}");
                            Key = keyPressed;
                            _keyTime = DateTime.MinValue;
                            return true;
                        }
                    }
                    else
                        // Resets our _keyTime in case we let go of the key(s).
                        _keyTime = DateTime.MinValue;
                    
                }
            }

            return false;
        }

        public void Reset()
        {
            Key = [0];
            // Key = 0;
            // Ctrl = false;
            // Alt = false;
            // Shift = false;
        }
        
        // public bool Equals(KeyBind bind)
        // {
        //     return Key == bind.Key &&
        //            Ctrl == bind.Ctrl &&
        //            Alt == bind.Alt &&
        //            Shift == bind.Shift;

        public bool Equals(KeyBind bind)
        {
            Plugin.Logger.Info($"Cur Ring: {string.Join(",", Key)} | Inc Ring: {string.Join(",", bind.Key)}");

            // return false;
            return Key.SequenceEqual(bind.Key);
        }
        // Ctrl == bind.Ctrl &&
        // Alt == bind.Alt &&
        // Shift == bind.Shift;
    }
}
    
// }
// }
// }
