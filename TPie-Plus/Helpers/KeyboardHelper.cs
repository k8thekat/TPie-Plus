﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TPie_Plus.Helpers
{
    internal class KeyboardHelper
    {
        #region singleton
        private KeyboardHelper()
        {
            _keyStates = new byte[256];
        }

        public static void Initialize() { Instance = new KeyboardHelper(); }

        public static KeyboardHelper Instance { get; private set; } = null!;

        ~KeyboardHelper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Instance = null!;
        }
        #endregion

        public void Update()
        {
            GetKeyboardState(_keyStates);
        }

        public bool IsKeyPressed(int key)
        {
            if (!IsGameFocused()) return false;

            if (key != (int)Keys.Back && !_supportedKeys.Contains((Keys)key))
            {
                return false;
            }

            return _keyStates[key] > 1;
        }

        public bool IsEscapePressed()
        {
            if (!IsGameFocused()) return false;

            return _keyStates[(int)Keys.Escape] > 1;
        }

        // public int GetKeyPressed()
        // {
        //     if (!IsGameFocused()) return 0;
        //     
        //     // They appear to be for looping by an increment of `i` or `1`
        //     // through all the entries of the _supportedKeys list.
        //     for (int i = 0; i < _supportedKeys.Count; i++)
        //     {
        //         // Using the index and Enum value from `Keys` to map an int value.
        //         // They check that key state, if the bit is > 1 it's down, otherwise it's up.
        //         int key = (int)_supportedKeys[i];
        //         // Going through the list of supported keys, they are checking if any of them are "pressed down"
        //         // and returning that specific key.
        //         if (_keyStates[key] > 1)
        //         {
        //             return key;
        //         }
        //     }
        //
        //     return 0;
        // }

        public List<int> GetKeysPressed()
        {
        List<int> keysPressed = [];
            if (!IsGameFocused()) return keysPressed;
            
            for (int i = 0; i < _supportedKeys.Count; i++)
            {
                // Using the index and Enum value from `Keys` to map an int value.
                // They check that key state, if the bit is > 1 it's down, otherwise it's up.
                int key = (int)_supportedKeys[i];
                // Going through the list of supported keys, they are checking if any of them are "pressed down"
                // and returning that specific key.
                if (_keyStates[key] > 1)
                {
                    keysPressed.Add(key);
                }
                
            }
            return keysPressed;
        }
        
        public bool IsKeysPressed(List<int> keys)
        {
            if (!IsGameFocused()) return false;
            
            foreach (int t in keys)
            {
                // If any of the key's currently pressed are *NOT* backspace or
                // any of our supported keys we return false, otherwise we are pressing supported keys.
                if (t == (int)Keys.Back)
                    return false;
                else if (_supportedKeys.Contains((Keys)t) == false)
                    return false;
                else if (_keyStates[t] <= 1)
                    return false;
                // Plugin.Logger.Info($"Cur Key: {t} | State: {_keyStates[t]}");
            }
            // Plugin.Logger.Info($"Return: {true}");
            return true;
        }

        private byte[] _keyStates;

        private static List<Keys> _supportedKeys = new List<Keys>()
        {
            Keys.MButton,
            Keys.XButton1,
            Keys.XButton2,
            Keys.CapsLock,
            Keys.Space,
            Keys.PageUp,
            Keys.Next,
            Keys.PageDown,
            Keys.End,
            Keys.Home,
            Keys.Left,
            Keys.Up,
            Keys.Right,
            Keys.Down,
            Keys.Insert,
            Keys.Delete,
            Keys.D0,
            Keys.D1,
            Keys.D2,
            Keys.D3,
            Keys.D4,
            Keys.D5,
            Keys.D6,
            Keys.D7,
            Keys.D8,
            Keys.D9,
            Keys.A,
            Keys.B,
            Keys.C,
            Keys.D,
            Keys.E,
            Keys.F,
            Keys.G,
            Keys.H,
            Keys.I,
            Keys.J,
            Keys.K,
            Keys.L,
            Keys.M,
            Keys.N,
            Keys.O,
            Keys.P,
            Keys.Q,
            Keys.R,
            Keys.S,
            Keys.T,
            Keys.U,
            Keys.V,
            Keys.W,
            Keys.X,
            Keys.Y,
            Keys.Z,
            Keys.NumPad0,
            Keys.NumPad1,
            Keys.NumPad2,
            Keys.NumPad3,
            Keys.NumPad4,
            Keys.NumPad5,
            Keys.NumPad6,
            Keys.NumPad7,
            Keys.NumPad8,
            Keys.NumPad9,
            Keys.Multiply,
            Keys.Add,
            Keys.Separator,
            Keys.Subtract,
            Keys.Decimal,
            Keys.Divide,
            Keys.F1,
            Keys.F2,
            Keys.F3,
            Keys.F4,
            Keys.F5,
            Keys.F6,
            Keys.F7,
            Keys.F8,
            Keys.F9,
            Keys.F10,
            Keys.F11,
            Keys.F12,
            Keys.F13,
            Keys.F14,
            Keys.F15,
            Keys.F16,
            Keys.F17,
            Keys.F18,
            Keys.F19,
            Keys.F20,
            Keys.F21,
            Keys.F22,
            Keys.F23,
            Keys.F24,
            Keys.OemSemicolon,
            Keys.Oem1,
            Keys.Oemplus,
            Keys.Oemcomma,
            Keys.OemMinus,
            Keys.OemPeriod,
            Keys.OemQuestion,
            Keys.Oem2,
            Keys.Oemtilde,
            Keys.Oem3,
            Keys.OemOpenBrackets,
            Keys.Oem4,
            Keys.OemPipe,
            Keys.Oem5,
            Keys.OemCloseBrackets,
            Keys.Oem6,
            Keys.OemQuotes,
            Keys.Oem7,
            Keys.Oem8,
            Keys.OemBackslash,
            Keys.Oem102,
            Keys.LControlKey,
            Keys.RControlKey,
            Keys.LShiftKey,
            Keys.RShiftKey,
            Keys.LMenu,
            Keys.RMenu
            
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetKeyboardState(byte[] keyStates);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private bool IsGameFocused()
        {
            var foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero) return false;

            GetWindowThreadProcessId(foregroundWindowHandle, out var activeProcessId);

            return activeProcessId == Environment.ProcessId;
        }
    }
}
