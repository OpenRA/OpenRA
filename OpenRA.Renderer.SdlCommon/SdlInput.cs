#region Copyright & License Information
/*
* Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see COPYING.
*/
#endregion

using System.Collections.Generic;
using Tao.Sdl;

namespace OpenRA.Renderer.SdlCommon
{
	public class SdlInput
	{
		static readonly Dictionary<int, Keycode> KeyRemap = new Dictionary<int, Keycode>
		{
			{ Sdl.SDLK_BACKSPACE, Keycode.BACKSPACE },
			{ Sdl.SDLK_TAB, Keycode.TAB },
			{ Sdl.SDLK_CLEAR, Keycode.CLEAR },
			{ Sdl.SDLK_RETURN, Keycode.RETURN },
			{ Sdl.SDLK_PAUSE, Keycode.PAUSE },
			{ Sdl.SDLK_ESCAPE, Keycode.ESCAPE },
			{ Sdl.SDLK_SPACE, Keycode.SPACE },
			{ Sdl.SDLK_EXCLAIM, Keycode.EXCLAIM },
			{ Sdl.SDLK_QUOTEDBL, Keycode.QUOTEDBL },
			{ Sdl.SDLK_HASH, Keycode.HASH },
			{ Sdl.SDLK_DOLLAR, Keycode.DOLLAR },
			{ Sdl.SDLK_AMPERSAND, Keycode.AMPERSAND },
			{ Sdl.SDLK_QUOTE, Keycode.QUOTE },
			{ Sdl.SDLK_LEFTPAREN, Keycode.LEFTPAREN },
			{ Sdl.SDLK_RIGHTPAREN, Keycode.RIGHTPAREN },
			{ Sdl.SDLK_ASTERISK, Keycode.ASTERISK },
			{ Sdl.SDLK_PLUS, Keycode.PLUS },
			{ Sdl.SDLK_COMMA, Keycode.COMMA },
			{ Sdl.SDLK_MINUS, Keycode.MINUS },
			{ Sdl.SDLK_PERIOD, Keycode.PERIOD },
			{ Sdl.SDLK_SLASH, Keycode.SLASH },
			{ Sdl.SDLK_0, Keycode.NUMBER_0 },
			{ Sdl.SDLK_1, Keycode.NUMBER_1 },
			{ Sdl.SDLK_2, Keycode.NUMBER_2 },
			{ Sdl.SDLK_3, Keycode.NUMBER_3 },
			{ Sdl.SDLK_4, Keycode.NUMBER_4 },
			{ Sdl.SDLK_5, Keycode.NUMBER_5 },
			{ Sdl.SDLK_6, Keycode.NUMBER_6 },
			{ Sdl.SDLK_7, Keycode.NUMBER_7 },
			{ Sdl.SDLK_8, Keycode.NUMBER_8 },
			{ Sdl.SDLK_9, Keycode.NUMBER_9 },
			{ Sdl.SDLK_COLON, Keycode.COLON },
			{ Sdl.SDLK_SEMICOLON, Keycode.SEMICOLON },
			{ Sdl.SDLK_LESS, Keycode.LESS },
			{ Sdl.SDLK_EQUALS, Keycode.EQUALS },
			{ Sdl.SDLK_GREATER, Keycode.GREATER },
			{ Sdl.SDLK_QUESTION, Keycode.QUESTION },
			{ Sdl.SDLK_AT, Keycode.AT },
			{ Sdl.SDLK_LEFTBRACKET, Keycode.LEFTBRACKET },
			{ Sdl.SDLK_BACKSLASH, Keycode.BACKSLASH },
			{ Sdl.SDLK_RIGHTBRACKET, Keycode.RIGHTBRACKET },
			{ Sdl.SDLK_CARET, Keycode.CARET },
			{ Sdl.SDLK_UNDERSCORE, Keycode.UNDERSCORE },
			{ Sdl.SDLK_BACKQUOTE, Keycode.BACKQUOTE },
			{ Sdl.SDLK_a, Keycode.A },
			{ Sdl.SDLK_b, Keycode.B },
			{ Sdl.SDLK_c, Keycode.C },
			{ Sdl.SDLK_d, Keycode.D },
			{ Sdl.SDLK_e, Keycode.E },
			{ Sdl.SDLK_f, Keycode.F },
			{ Sdl.SDLK_g, Keycode.G },
			{ Sdl.SDLK_h, Keycode.H },
			{ Sdl.SDLK_i, Keycode.I },
			{ Sdl.SDLK_j, Keycode.J },
			{ Sdl.SDLK_k, Keycode.K },
			{ Sdl.SDLK_l, Keycode.L },
			{ Sdl.SDLK_m, Keycode.M },
			{ Sdl.SDLK_n, Keycode.N },
			{ Sdl.SDLK_o, Keycode.O },
			{ Sdl.SDLK_p, Keycode.P },
			{ Sdl.SDLK_q, Keycode.Q },
			{ Sdl.SDLK_r, Keycode.R },
			{ Sdl.SDLK_s, Keycode.S },
			{ Sdl.SDLK_t, Keycode.T },
			{ Sdl.SDLK_u, Keycode.U },
			{ Sdl.SDLK_v, Keycode.V },
			{ Sdl.SDLK_w, Keycode.W },
			{ Sdl.SDLK_x, Keycode.X },
			{ Sdl.SDLK_y, Keycode.Y },
			{ Sdl.SDLK_z, Keycode.Z },
			{ Sdl.SDLK_DELETE, Keycode.DELETE },
			{ Sdl.SDLK_KP0, Keycode.KP_0 },
			{ Sdl.SDLK_KP1, Keycode.KP_1 },
			{ Sdl.SDLK_KP2, Keycode.KP_2 },
			{ Sdl.SDLK_KP3, Keycode.KP_3 },
			{ Sdl.SDLK_KP4, Keycode.KP_4 },
			{ Sdl.SDLK_KP5, Keycode.KP_5 },
			{ Sdl.SDLK_KP6, Keycode.KP_6 },
			{ Sdl.SDLK_KP7, Keycode.KP_7 },
			{ Sdl.SDLK_KP8, Keycode.KP_8 },
			{ Sdl.SDLK_KP9, Keycode.KP_9 },
			{ Sdl.SDLK_KP_PERIOD, Keycode.KP_PERIOD },
			{ Sdl.SDLK_KP_DIVIDE, Keycode.KP_DIVIDE },
			{ Sdl.SDLK_KP_MULTIPLY, Keycode.KP_MULTIPLY },
			{ Sdl.SDLK_KP_MINUS, Keycode.KP_MINUS },
			{ Sdl.SDLK_KP_PLUS, Keycode.KP_PLUS },
			{ Sdl.SDLK_KP_ENTER, Keycode.KP_ENTER },
			{ Sdl.SDLK_KP_EQUALS, Keycode.KP_EQUALS },
			{ Sdl.SDLK_UP, Keycode.UP },
			{ Sdl.SDLK_DOWN, Keycode.DOWN },
			{ Sdl.SDLK_RIGHT, Keycode.RIGHT },
			{ Sdl.SDLK_LEFT, Keycode.LEFT },
			{ Sdl.SDLK_INSERT, Keycode.INSERT },
			{ Sdl.SDLK_HOME, Keycode.HOME },
			{ Sdl.SDLK_END, Keycode.END },
			{ Sdl.SDLK_PAGEUP, Keycode.PAGEUP },
			{ Sdl.SDLK_PAGEDOWN, Keycode.PAGEDOWN },
			{ Sdl.SDLK_F1, Keycode.F1 },
			{ Sdl.SDLK_F2, Keycode.F2 },
			{ Sdl.SDLK_F3, Keycode.F3 },
			{ Sdl.SDLK_F4, Keycode.F4 },
			{ Sdl.SDLK_F5, Keycode.F5 },
			{ Sdl.SDLK_F6, Keycode.F6 },
			{ Sdl.SDLK_F7, Keycode.F7 },
			{ Sdl.SDLK_F8, Keycode.F8 },
			{ Sdl.SDLK_F9, Keycode.F9 },
			{ Sdl.SDLK_F10, Keycode.F10 },
			{ Sdl.SDLK_F11, Keycode.F11 },
			{ Sdl.SDLK_F12, Keycode.F12 },
			{ Sdl.SDLK_F13, Keycode.F13 },
			{ Sdl.SDLK_F14, Keycode.F14 },
			{ Sdl.SDLK_F15, Keycode.F15 },
			{ Sdl.SDLK_NUMLOCK, Keycode.NUMLOCKCLEAR },
			{ Sdl.SDLK_CAPSLOCK, Keycode.CAPSLOCK },
			{ Sdl.SDLK_SCROLLOCK, Keycode.SCROLLLOCK },
			{ Sdl.SDLK_RSHIFT, Keycode.RSHIFT },
			{ Sdl.SDLK_LSHIFT, Keycode.LSHIFT },
			{ Sdl.SDLK_RCTRL, Keycode.RCTRL },
			{ Sdl.SDLK_LCTRL, Keycode.LCTRL },
			{ Sdl.SDLK_RALT, Keycode.RALT },
			{ Sdl.SDLK_LALT, Keycode.LALT },
			{ Sdl.SDLK_RMETA, Keycode.RGUI },
			{ Sdl.SDLK_LMETA, Keycode.LGUI },
			{ Sdl.SDLK_LSUPER, Keycode.LGUI },
			{ Sdl.SDLK_RSUPER, Keycode.RGUI },
			{ Sdl.SDLK_MODE, Keycode.MODE },
			{ Sdl.SDLK_HELP, Keycode.HELP },
			{ Sdl.SDLK_PRINT, Keycode.PRINTSCREEN },
			{ Sdl.SDLK_SYSREQ, Keycode.SYSREQ },
			{ Sdl.SDLK_MENU, Keycode.MENU },
			{ Sdl.SDLK_POWER, Keycode.POWER },
			{ Sdl.SDLK_UNDO, Keycode.UNDO },
		};


		MouseButton lastButtonBits = (MouseButton)0;

		static bool IsValidInput(char c)
		{
			return char.IsLetter(c) || char.IsDigit(c) ||
				char.IsSymbol(c) || char.IsSeparator(c) ||
					char.IsPunctuation(c);
		}

		MouseButton MakeButton(byte b)
		{
			return b == Sdl.SDL_BUTTON_LEFT ? MouseButton.Left
				: b == Sdl.SDL_BUTTON_RIGHT ? MouseButton.Right
				: b == Sdl.SDL_BUTTON_MIDDLE ? MouseButton.Middle
				: b == Sdl.SDL_BUTTON_WHEELDOWN ? MouseButton.WheelDown
				: b == Sdl.SDL_BUTTON_WHEELUP ? MouseButton.WheelUp
				: 0;
		}

		Modifiers MakeModifiers(int raw)
		{
			return ((raw & Sdl.KMOD_ALT) != 0 ? Modifiers.Alt : 0)
				 | ((raw & Sdl.KMOD_CTRL) != 0 ? Modifiers.Ctrl : 0)
				 | ((raw & Sdl.KMOD_META) != 0 ? Modifiers.Meta : 0)
				 | ((raw & Sdl.KMOD_SHIFT) != 0 ? Modifiers.Shift : 0);
		}

		public void PumpInput(IInputHandler inputHandler)
		{
			Game.HasInputFocus = 0 != (Sdl.SDL_GetAppState() & Sdl.SDL_APPINPUTFOCUS);

			var mods = MakeModifiers(Sdl.SDL_GetModState());
			inputHandler.ModifierKeys(mods);
			MouseInput? pendingMotion = null;

			Sdl.SDL_Event e;
			while (Sdl.SDL_PollEvent(out e) != 0)
			{
				switch (e.type)
				{
				case Sdl.SDL_QUIT:
					Game.Exit();
					break;

				case Sdl.SDL_MOUSEBUTTONDOWN:
					{
						if (pendingMotion != null)
						{
							inputHandler.OnMouseInput(pendingMotion.Value);
							pendingMotion = null;
						}

						var button = MakeButton(e.button.button);
						lastButtonBits |= button;

						var pos = new int2(e.button.x, e.button.y);

						inputHandler.OnMouseInput(new MouseInput(
							MouseInputEvent.Down, button, pos, mods,
							MultiTapDetection.DetectFromMouse(e.button.button, pos)));

						break;
					}

				case Sdl.SDL_MOUSEBUTTONUP:
					{
						if (pendingMotion != null)
						{
							inputHandler.OnMouseInput(pendingMotion.Value);
							pendingMotion = null;
						}

						var button = MakeButton(e.button.button);
						lastButtonBits &= ~button;

						var pos = new int2(e.button.x, e.button.y);
						inputHandler.OnMouseInput(new MouseInput(
							MouseInputEvent.Up, button, pos, mods,
							MultiTapDetection.InfoFromMouse(e.button.button)));

						break;
					} 

				case Sdl.SDL_MOUSEMOTION:
					{
						pendingMotion = new MouseInput(
							MouseInputEvent.Move, lastButtonBits,
							new int2(e.motion.x, e.motion.y), mods, 0);

						break;
					} 

				case Sdl.SDL_KEYDOWN:
				case Sdl.SDL_KEYUP:
					{
						// Drop unknown keys
						Keycode keyCode;
						if (!KeyRemap.TryGetValue(e.key.keysym.sym, out keyCode))
						{
							// Try parsing it as text
							var c = (char)e.key.keysym.unicode;
							if (IsValidInput(c))
								inputHandler.OnTextInput(c.ToString());

							break;
						}

						var type = e.type == Sdl.SDL_KEYDOWN ?
							KeyInputEvent.Down : KeyInputEvent.Up;

						var tapCount = e.type == Sdl.SDL_KEYDOWN ?
							MultiTapDetection.DetectFromKeyboard(keyCode) :
							MultiTapDetection.InfoFromKeyboard(keyCode);

						var keyEvent = new KeyInput
						{
							Event = type,
							Key = keyCode,
							Modifiers = mods,
							UnicodeChar = (char)e.key.keysym.unicode,
							MultiTapCount = tapCount
						};

						// Special case workaround for windows users
						if (e.key.keysym.sym == Sdl.SDLK_F4 && mods.HasModifier(Modifiers.Alt) &&
						    Platform.CurrentPlatform == PlatformType.Windows)
						{
							Game.Exit();
						}
						else
							inputHandler.OnKeyInput(keyEvent);

						if (IsValidInput(keyEvent.UnicodeChar))
							inputHandler.OnTextInput(keyEvent.UnicodeChar.ToString());

						break;
					}
				}
			}

			if (pendingMotion != null)
			{
				inputHandler.OnMouseInput(pendingMotion.Value);
				pendingMotion = null;
			}

			ErrorHandler.CheckGlError();
		}
	}
}