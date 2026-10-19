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
using OpenRA.Primitives;
using SDL2;

namespace OpenRA.Platforms.Default
{
	sealed class Sdl2Input
	{
		MouseButton lastButtonBits = MouseButton.None;
		int2 lastMotionPos;
		int2 lastMotionDelta;

		public static string GetClipboardText() { return SDL.SDL_GetClipboardText(); }
		public static bool SetClipboardText(string text) { return SDL.SDL_SetClipboardText(text) == 0; }

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

		static int2 EventPosition(Sdl2PlatformWindow device, int x, int y)
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

		static bool TryGetWindowExit(int2 pos, int2 delta, Size size, out int2 exit)
		{
			exit = pos;

			// Already outside (X11 case) or no known direction: nothing to do
			if (pos.X < 0 || pos.Y < 0 || pos.X >= size.Width || pos.Y >= size.Height || delta == int2.Zero)
				return false;

			var tx = delta.X < 0 ? (float)-pos.X / delta.X
			: delta.X > 0 ? (float)(size.Width - 1 - pos.X) / delta.X
			: float.PositiveInfinity;

			var ty = delta.Y < 0 ? (float)-pos.Y / delta.Y
			: delta.Y > 0 ? (float)(size.Height - 1 - pos.Y) / delta.Y
			: float.PositiveInfinity;

			var t = Math.Min(tx, ty);
			var x = Math.Clamp((int)Math.Round(pos.X + delta.X * t), 0, size.Width - 1);
			var y = Math.Clamp((int)Math.Round(pos.Y + delta.Y * t), 0, size.Height - 1);

			// Put the exit axis just beyond the edge
			if (tx <= ty)
				x = delta.X < 0 ? -1 : size.Width;
			else
				y = delta.Y < 0 ? -1 : size.Height;

			exit = new int2(x, y);
			return true;
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

							case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
								lastMotionDelta = int2.Zero;
								break;

							case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
							{
								// Wayland (and thus XWayland) does not report where the pointer left the window,
								// so the last position we know can still be well inside it. Estimate the exit edge
								// from the last motion direction so that edge scrolling keeps working, as on X11.
								if (lockedMousePosition == null && lastButtonBits == MouseButton.None &&
									TryGetWindowExit(lastMotionPos, lastMotionDelta, device.EffectiveWindowSize, out var exitPos))
								{
									if (pendingMotion != null)
									{
										inputHandler.OnMouseInput(pendingMotion.Value);
										pendingMotion = null;
									}

									inputHandler.OnMouseInput(new MouseInput(
										MouseInputEvent.Move, MouseButton.None, exitPos, int2.Zero, mods, 0));

									lastMotionDelta = int2.Zero;
								}

								break;
							}
						}

						break;
					}

					case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
					case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
					{
						// Mouse 1, Mouse 2 and Mouse 3 are handled as mouse inputs
						// Mouse 4 and Mouse 5 are treated as (pseudo) keyboard inputs
						if (e.button.button == SDL.SDL_BUTTON_LEFT ||
							e.button.button == SDL.SDL_BUTTON_MIDDLE ||
							e.button.button == SDL.SDL_BUTTON_RIGHT)
						{
							if (pendingMotion != null)
							{
								inputHandler.OnMouseInput(pendingMotion.Value);
								pendingMotion = null;
							}

							var button = MakeButton(e.button.button);

							if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
								lastButtonBits |= button;
							else
								lastButtonBits &= ~button;

							var input = lockedMousePosition ?? new int2(e.button.x, e.button.y);
							var pos = EventPosition(device, input.X, input.Y);

							if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
								inputHandler.OnMouseInput(new MouseInput(
									MouseInputEvent.Down, button, pos, int2.Zero, mods,
									MultiTapDetection.DetectFromMouse(e.button.button, pos)));
							else
								inputHandler.OnMouseInput(new MouseInput(
									MouseInputEvent.Up, button, pos, int2.Zero, mods,
									MultiTapDetection.InfoFromMouse(e.button.button)));
						}

						if (e.button.button == SDL.SDL_BUTTON_X1 ||
							e.button.button == SDL.SDL_BUTTON_X2)
						{
							Keycode keyCode;

							if (e.button.button == SDL.SDL_BUTTON_X1)
								keyCode = Keycode.MOUSE4;
							else
								keyCode = Keycode.MOUSE5;

							var type = e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN ?
								KeyInputEvent.Down : KeyInputEvent.Up;

							var tapCount = e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN ?
								MultiTapDetection.DetectFromKeyboard(keyCode, mods) :
								MultiTapDetection.InfoFromKeyboard(keyCode, mods);

							var keyEvent = new KeyInput
							{
								Event = type,
								Key = keyCode,
								Modifiers = mods,
								UnicodeChar = '?',
								MultiTapCount = tapCount,
								IsRepeat = e.key.repeat != 0
							};
							inputHandler.OnKeyInput(keyEvent);
						}

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

						if (lockedMousePosition == null)
						{
							lastMotionPos = pos;
							if (delta != int2.Zero)
								lastMotionDelta = delta;
						}

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
						inputHandler.OnTextInput(Encoding.UTF8.GetString(rawBytes, 0, rawBytes.IndexOf((byte)0)));
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
