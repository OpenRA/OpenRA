#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;

namespace OpenRA.Input
{
	public class DefaultInput
	{
		MouseButton lastButtonBits = (MouseButton)0;
		MouseInput? pendingMotion = null;
		int scrollDelta = 0;
		Modifiers modifier;
		KeyInput keyEvent = new KeyInput();

		// OpenTK to OpenRA mapping
		static readonly Dictionary<Key, Keycode> KeycodeToKey = new Dictionary<Key, Keycode>
		{
			{ Key.Enter, Keycode.RETURN },
			{ Key.Escape, Keycode.ESCAPE },
			{ Key.BackSpace, Keycode.BACKSPACE },
			{ Key.Tab, Keycode.TAB },
			{ Key.Space, Keycode.SPACE },
			{ Key.Quote, Keycode.QUOTE },
			{ Key.Plus, Keycode.PLUS },
			{ Key.Comma, Keycode.COMMA },
			{ Key.Minus, Keycode.MINUS },
			{ Key.Period, Keycode.PERIOD },
			{ Key.Slash, Keycode.SLASH },
			{ Key.Number0, Keycode.NUMBER_0 },
			{ Key.Number1, Keycode.NUMBER_1 },
			{ Key.Number2, Keycode.NUMBER_2 },
			{ Key.Number3, Keycode.NUMBER_3 },
			{ Key.Number4, Keycode.NUMBER_4 },
			{ Key.Number5, Keycode.NUMBER_5 },
			{ Key.Number6, Keycode.NUMBER_6 },
			{ Key.Number7, Keycode.NUMBER_7 },
			{ Key.Number8, Keycode.NUMBER_8 },
			{ Key.Number9, Keycode.NUMBER_9 },
			{ Key.Semicolon, Keycode.SEMICOLON },
			{ Key.BracketLeft, Keycode.LEFTBRACKET },
			{ Key.BackSlash, Keycode.BACKSLASH },
			{ Key.BracketRight, Keycode.RIGHTBRACKET },
			{ Key.A, Keycode.A },
			{ Key.B, Keycode.B },
			{ Key.C, Keycode.C },
			{ Key.D, Keycode.D },
			{ Key.E, Keycode.E },
			{ Key.F, Keycode.F },
			{ Key.G, Keycode.G },
			{ Key.H, Keycode.H },
			{ Key.I, Keycode.I },
			{ Key.J, Keycode.J },
			{ Key.K, Keycode.K },
			{ Key.L, Keycode.L },
			{ Key.M, Keycode.M },
			{ Key.N, Keycode.N },
			{ Key.O, Keycode.O },
			{ Key.P, Keycode.P },
			{ Key.Q, Keycode.Q },
			{ Key.R, Keycode.R },
			{ Key.S, Keycode.S },
			{ Key.T, Keycode.T },
			{ Key.U, Keycode.U },
			{ Key.V, Keycode.V },
			{ Key.W, Keycode.W },
			{ Key.X, Keycode.X },
			{ Key.Y, Keycode.Y },
			{ Key.Z, Keycode.Z },
			{ Key.CapsLock, Keycode.CAPSLOCK },
			{ Key.F1, Keycode.F1 },
			{ Key.F2, Keycode.F2 },
			{ Key.F3, Keycode.F3 },
			{ Key.F4, Keycode.F4 },
			{ Key.F5, Keycode.F5 },
			{ Key.F6, Keycode.F6 },
			{ Key.F7, Keycode.F7 },
			{ Key.F8, Keycode.F8 },
			{ Key.F9, Keycode.F9 },
			{ Key.F10, Keycode.F10 },
			{ Key.F11, Keycode.F11 },
			{ Key.F12, Keycode.F12 },
			{ Key.PrintScreen, Keycode.PRINTSCREEN },
			{ Key.ScrollLock, Keycode.SCROLLLOCK },
			{ Key.Pause, Keycode.PAUSE },
			{ Key.Insert, Keycode.INSERT },
			{ Key.Home, Keycode.HOME },
			{ Key.PageUp,Keycode.PAGEUP },
			{ Key.Delete, Keycode.DELETE},
			{ Key.End, Keycode.END },
			{ Key.PageDown, Keycode.PAGEDOWN },
			{ Key.Right, Keycode.RIGHT },
			{ Key.Left, Keycode.LEFT },
			{ Key.Down, Keycode.DOWN },
			{ Key.Up, Keycode.UP },
			{ Key.NumLock, Keycode.NUMLOCKCLEAR },
			{ Key.KeypadDivide, Keycode.KP_DIVIDE },
			{ Key.KeypadMultiply, Keycode.KP_MULTIPLY },
			{ Key.KeypadMinus, Keycode.KP_MINUS },
			{ Key.KeypadPlus, Keycode.KP_PLUS },
			{ Key.KeypadEnter, Keycode.KP_ENTER },
			{ Key.Keypad1, Keycode.KP_1 },
			{ Key.Keypad2, Keycode.KP_2 },
			{ Key.Keypad3, Keycode.KP_3 },
			{ Key.Keypad4, Keycode.KP_4 },
			{ Key.Keypad5, Keycode.KP_5},
			{ Key.Keypad6, Keycode.KP_6},
			{ Key.Keypad7, Keycode.KP_7},
			{ Key.Keypad8, Keycode.KP_8},
			{ Key.Keypad9, Keycode.KP_9},
			{ Key.Keypad0, Keycode.KP_0},
			{ Key.KeypadPeriod, Keycode.KP_PERIOD },
			{ Key.F13, Keycode.F13},
			{ Key.F14, Keycode.F14},
			{ Key.F15, Keycode.F15},
			{ Key.F16, Keycode.F16},
			{ Key.F17, Keycode.F17},
			{ Key.F18, Keycode.F18},
			{ Key.F19, Keycode.F19},
			{ Key.F20, Keycode.F20},
			{ Key.F21, Keycode.F21},
			{ Key.F22, Keycode.F22},
			{ Key.F23, Keycode.F23},
			{ Key.F24, Keycode.F24},
			{ Key.ControlLeft, Keycode.LCTRL},
			{ Key.ShiftLeft, Keycode.LSHIFT},
			{ Key.AltLeft, Keycode.LALT},
			{ Key.WinLeft, Keycode.LGUI},
			{ Key.ControlRight, Keycode.RCTRL},
			{ Key.ShiftRight, Keycode.RSHIFT},
			{ Key.AltRight, Keycode.RALT},
			{ Key.WinRight, Keycode.RGUI},
			{ Key.Unknown, Keycode.UNKNOWN },
		};

		public static Keycode GetKeycode(Key key)
		{
			Keycode code;
			if (!KeycodeToKey.TryGetValue(key, out code))
				return Keycode.UNKNOWN;

			return code;
		}

		static MouseButton ConvertButton(OpenTK.Input.MouseButton button)
		{
			switch(button)
			{
				case OpenTK.Input.MouseButton.Left: return MouseButton.Left;
				case OpenTK.Input.MouseButton.Middle: return MouseButton.Middle;
				case OpenTK.Input.MouseButton.Right: return MouseButton.Right;
				default: return MouseButton.None;
			}
		}

		static Modifiers ConvertModifiers(KeyModifiers modifier)
		{
			switch(modifier)
			{
				case KeyModifiers.Alt: return Modifiers.Alt;
				case KeyModifiers.Shift: return Modifiers.Shift;
				case KeyModifiers.Control: return Modifiers.Ctrl;
				default: return Modifiers.None;
			}
		}

		public DefaultInput(GameWindow window)
		{
			window.Closing += (object sender, System.ComponentModel.CancelEventArgs e) =>
			{
				Game.Exit();
			};

			window.MouseLeave += (object sender, EventArgs e) => 
			{
				Game.HasInputFocus = false;
			};

			window.MouseEnter += (object sender, EventArgs e) => 
			{
				Game.HasInputFocus = true;
			};

			window.MouseDown += (object sender, MouseButtonEventArgs e) =>
			{
				PumpInput(window);

				var button = ConvertButton(e.Button);
				lastButtonBits |= button;
				var position = new int2(e.Position.X, e.Position.Y);
				var tabs = MultiTapDetection.DetectFromMouse(button, position);
				Game.InputHandler.OnMouseInput(new MouseInput(MouseInputEvent.Down, button, scrollDelta, position, modifier, tabs));
			};

			window.MouseUp += (object sender, MouseButtonEventArgs e) =>
			{
				PumpInput(window);

				var button = ConvertButton(e.Button);
				lastButtonBits &= ~button;
				var position = new int2(e.Position.X, e.Position.Y);
				var tabs = MultiTapDetection.InfoFromMouse(button);
				Game.InputHandler.OnMouseInput(new MouseInput(MouseInputEvent.Up, button, scrollDelta, position, modifier, tabs));
			};

			window.MouseMove += (object sender, MouseMoveEventArgs e) =>
			{
				var position = new int2(e.Position.X, e.Position.Y);
				pendingMotion = new MouseInput(MouseInputEvent.Move, lastButtonBits, scrollDelta, position, modifier, 0);
			};

			window.MouseWheel += (object sender, MouseWheelEventArgs e) => 
			{
				scrollDelta = e.Delta;
				var position = new int2(e.Position.X, e.Position.Y);
				Game.InputHandler.OnMouseInput(new MouseInput(MouseInputEvent.Scroll, MouseButton.None, scrollDelta, position, modifier, 0));
			};

			window.KeyDown += (object sender, KeyboardKeyEventArgs e) =>
			{
				if (e.Key == Key.WinLeft || e.Key == Key.WinRight)
					keyEvent.Modifiers = Modifiers.Meta;
				else
					keyEvent.Modifiers = modifier = ConvertModifiers(e.Modifiers);
				keyEvent.Event = KeyInputEvent.Down;
				var keyCode = GetKeycode(e.Key);
				keyEvent.Key = keyCode;
				keyEvent.MultiTapCount = MultiTapDetection.DetectFromKeyboard(keyCode);
				Game.InputHandler.OnKeyInput(keyEvent);
			};

			window.KeyUp += (object sender, KeyboardKeyEventArgs e) =>
			{
				keyEvent.Modifiers = modifier = ConvertModifiers(e.Modifiers);
				keyEvent.Event = KeyInputEvent.Up;
				var keyCode = GetKeycode(e.Key);
				keyEvent.Key = keyCode;
				keyEvent.MultiTapCount = MultiTapDetection.InfoFromKeyboard(keyCode);
				Game.InputHandler.OnKeyInput(keyEvent);
			};

			window.KeyPress += (object sender, KeyPressEventArgs e) =>
			{
				keyEvent.UnicodeChar = e.KeyChar;
				Game.InputHandler.OnTextInput(e.KeyChar.ToString());
			};
		}

		public void PumpInput(GameWindow window)
		{
			if (pendingMotion != null && Game.InputHandler != null)
			{
				Game.InputHandler.OnMouseInput(pendingMotion.Value);
				pendingMotion = null;
			}
		}
	}
}

