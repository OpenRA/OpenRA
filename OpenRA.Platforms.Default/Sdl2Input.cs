#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;

namespace OpenRA.Platforms.Default
{
	class Sdl2Input
	{
		MouseButton lastButtonBits = MouseButton.None;

		public string GetClipboardText() { return SDL.SDL_GetClipboardText(); }
		public bool SetClipboardText(string text) { return SDL.SDL_SetClipboardText(text) == 0; }

		static MouseButton MakeButton(byte b)
		{
			return b == SDL.SDL_BUTTON_LEFT ? MouseButton.Left
				: b == SDL.SDL_BUTTON_RIGHT ? MouseButton.Right
				: b == SDL.SDL_BUTTON_MIDDLE ? MouseButton.Middle
				: 0;
		}

		static Modifiers MakeModifiers(int raw)
		{
			return ((raw & (int)SDL.SDL_Keymod.KMOD_ALT) != 0 ? Modifiers.Alt : 0)
				 | ((raw & (int)SDL.SDL_Keymod.KMOD_CTRL) != 0 ? Modifiers.Ctrl : 0)
				 | ((raw & (int)SDL.SDL_Keymod.KMOD_LGUI) != 0 ? Modifiers.Meta : 0)
				 | ((raw & (int)SDL.SDL_Keymod.KMOD_RGUI) != 0 ? Modifiers.Meta : 0)
				 | ((raw & (int)SDL.SDL_Keymod.KMOD_SHIFT) != 0 ? Modifiers.Shift : 0);
		}

		public void PumpInput(IInputHandler inputHandler)
		{
			var mods = MakeModifiers((int)SDL.SDL_GetModState());
			var scrollDelta = 0;
			inputHandler.ModifierKeys(mods);
			MouseInput? pendingMotion = null;

			SDL.SDL_Event e;
			while (SDL.SDL_PollEvent(out e) != 0)
			{
				switch (e.type)
				{
					case SDL.SDL_EventType.SDL_QUIT:
						Game.Exit();
						break;

					case SDL.SDL_EventType.SDL_WINDOWEVENT:
						{
							switch (e.window.windowEvent)
							{
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
									Game.HasInputFocus = false;
									break;

								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
									Game.HasInputFocus = true;
									break;
							}

							break;
						}

					case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
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
								MouseInputEvent.Down, button, scrollDelta, pos, mods,
								MultiTapDetection.DetectFromMouse(e.button.button, pos)));

							break;
						}

					case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
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
								MouseInputEvent.Up, button, scrollDelta, pos, mods,
								MultiTapDetection.InfoFromMouse(e.button.button)));

							break;
						}

					case SDL.SDL_EventType.SDL_MOUSEMOTION:
						{
							pendingMotion = new MouseInput(
								MouseInputEvent.Move, lastButtonBits, scrollDelta,
								new int2(e.motion.x, e.motion.y), mods, 0);

							break;
						}

					case SDL.SDL_EventType.SDL_MOUSEWHEEL:
						{
							int x, y;
							SDL.SDL_GetMouseState(out x, out y);
							scrollDelta = e.wheel.y;
							inputHandler.OnMouseInput(new MouseInput(MouseInputEvent.Scroll, MouseButton.None, scrollDelta, new int2(x, y), mods, 0));

							break;
						}

					case SDL.SDL_EventType.SDL_TEXTINPUT:
						{
							var rawBytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
							unsafe { Marshal.Copy((IntPtr)e.text.text, rawBytes, 0, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE); }
							inputHandler.OnTextInput(Encoding.UTF8.GetString(rawBytes, 0, Array.IndexOf(rawBytes, (byte)0)));
							break;
						}

					case SDL.SDL_EventType.SDL_KEYDOWN:
					case SDL.SDL_EventType.SDL_KEYUP:
						{
							var keyCode = (Keycode)e.key.keysym.sym;
							var type = e.type == SDL.SDL_EventType.SDL_KEYDOWN ?
								KeyInputEvent.Down : KeyInputEvent.Up;

							var tapCount = e.type == SDL.SDL_EventType.SDL_KEYDOWN ?
								MultiTapDetection.DetectFromKeyboard(keyCode) :
								MultiTapDetection.InfoFromKeyboard(keyCode);

							var keyEvent = new KeyInput
							{
								Event = type,
								Key = keyCode,
								Modifiers = mods,
								UnicodeChar = (char)e.key.keysym.sym,
								MultiTapCount = tapCount
							};

							// Special case workaround for windows users
							if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_F4 && mods.HasModifier(Modifiers.Alt) &&
								Platform.CurrentPlatform == PlatformType.Windows)
								Game.Exit();
							else
								inputHandler.OnKeyInput(keyEvent);

							break;
						}
				}
			}

			if (pendingMotion != null)
			{
				inputHandler.OnMouseInput(pendingMotion.Value);
				pendingMotion = null;
			}

			OpenGL.CheckGLError();
		}
	}
}
