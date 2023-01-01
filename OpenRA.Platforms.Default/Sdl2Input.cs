#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

		int2 EventPosition(Sdl2PlatformWindow device, int x, int y)
		{
			// On Windows and Linux (X11) events are given in surface coordinates
			// These must be scaled to our effective window coordinates
			// Round fractional components up to avoid rounding small deltas to 0
			if (Platform.CurrentPlatform != PlatformType.OSX && device.EffectiveWindowSize != device.SurfaceSize)
			{
				var s = 1 / device.EffectiveWindowScale;
				return new int2((int)(Math.Sign(x) / 2f + x * s), (int)(Math.Sign(x) / 2f + y * s));
			}

			// On macOS we must still account for the user-requested scale modifier
			if (Platform.CurrentPlatform == PlatformType.OSX && device.EffectiveWindowScale != device.NativeWindowScale)
			{
				var s = device.NativeWindowScale / device.EffectiveWindowScale;
				return new int2((int)(Math.Sign(x) / 2f + x * s), (int)(Math.Sign(x) / 2f + y * s));
			}

			return new int2(x, y);
		}

		public void PumpInput(Sdl2PlatformWindow device, IInputHandler inputHandler, int2? lockedMousePosition)
		{
			var mods = MakeModifiers((int)SDL.SDL_GetModState());
			inputHandler.ModifierKeys(mods);
			MouseInput? pendingMotion = null;

			while (SDL.SDL_PollEvent(out var e) != 0)
			{
				switch (e.type)
				{
					case SDL.SDL_EventType.SDL_QUIT:
						// On macOS, we'd like to restrict Cmd + Q from suddenly exiting the game.
						if (Platform.CurrentPlatform != PlatformType.OSX || !mods.HasModifier(Modifiers.Meta))
							Game.Exit();

						break;

					case SDL.SDL_EventType.SDL_WINDOWEVENT:
						{
							switch (e.window.windowEvent)
							{
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
									device.HasInputFocus = false;
									break;

								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
									device.HasInputFocus = true;
									break;

								// Triggered when moving between displays with different DPI settings
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
									device.WindowSizeChanged();
									break;

								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
									device.IsSuspended = true;
									break;

								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
								case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
									device.IsSuspended = false;
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

							var input = lockedMousePosition ?? new int2(e.button.x, e.button.y);
							var pos = EventPosition(device, input.X, input.Y);

							inputHandler.OnMouseInput(new MouseInput(
								MouseInputEvent.Down, button, pos, int2.Zero, mods,
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

							var input = lockedMousePosition ?? new int2(e.button.x, e.button.y);
							var pos = EventPosition(device, input.X, input.Y);

							inputHandler.OnMouseInput(new MouseInput(
								MouseInputEvent.Up, button, pos, int2.Zero, mods,
								MultiTapDetection.InfoFromMouse(e.button.button)));

							break;
						}

					case SDL.SDL_EventType.SDL_MOUSEMOTION:
						{
							var mousePos = new int2(e.motion.x, e.motion.y);
							var input = lockedMousePosition ?? mousePos;
							var pos = EventPosition(device, input.X, input.Y);

							var delta = lockedMousePosition == null
								? EventPosition(device, e.motion.xrel, e.motion.yrel)
								: mousePos - lockedMousePosition.Value;

							pendingMotion = new MouseInput(
								MouseInputEvent.Move, lastButtonBits, pos, delta, mods, 0);

							break;
						}

					case SDL.SDL_EventType.SDL_MOUSEWHEEL:
						{
							SDL.SDL_GetMouseState(out var x, out var y);

							var pos = EventPosition(device, x, y);
							inputHandler.OnMouseInput(new MouseInput(MouseInputEvent.Scroll, MouseButton.None, pos, new int2(0, e.wheel.y), mods, 0));

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
								MultiTapDetection.DetectFromKeyboard(keyCode, mods) :
								MultiTapDetection.InfoFromKeyboard(keyCode, mods);

							var keyEvent = new KeyInput
							{
								Event = type,
								Key = keyCode,
								Modifiers = mods,
								UnicodeChar = (char)e.key.keysym.sym,
								MultiTapCount = tapCount,
								IsRepeat = e.key.repeat != 0
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
				inputHandler.OnMouseInput(pendingMotion.Value);
		}
	}
}
