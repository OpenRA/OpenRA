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
using Silk.NET.SDL;

namespace OpenRA.Platforms.Default
{
	sealed class Sdl2Input
	{
		readonly Sdl sdl;

		MouseButton lastButtonBits = MouseButton.None;

		public static string GetClipboardText(Sdl sdl) { return sdl.GetClipboardTextS(); }
		public static bool SetClipboardText(Sdl sdl, string text) { return sdl.SetClipboardText(text) == 0; }

		static MouseButton MakeButton(byte b)
		{
			return b == Sdl.ButtonLeft ? MouseButton.Left
				: b == Sdl.ButtonRight ? MouseButton.Right
				: b == Sdl.ButtonMiddle ? MouseButton.Middle
				: 0;
		}

		static Modifiers MakeModifiers(int raw)
		{
			return ((raw & (int)Keymod.Alt) != 0 ? Modifiers.Alt : 0)
				 | ((raw & (int)Keymod.Ctrl) != 0 ? Modifiers.Ctrl : 0)
				 | ((raw & (int)Keymod.Lgui) != 0 ? Modifiers.Meta : 0)
				 | ((raw & (int)Keymod.Rgui) != 0 ? Modifiers.Meta : 0)
				 | ((raw & (int)Keymod.Shift) != 0 ? Modifiers.Shift : 0);
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

		public Sdl2Input(Sdl sdl)
		{
			this.sdl = sdl;
		}

		public void PumpInput(Sdl2PlatformWindow device, IInputHandler inputHandler, int2? lockedMousePosition)
		{
			var mods = MakeModifiers((int)sdl.GetModState());
			inputHandler.ModifierKeys(mods);
			MouseInput? pendingMotion = null;
			var e = default(Event);

			while (sdl.PollEvent(ref e) != 0)
			{
				switch ((EventType)e.Type)
				{
					case EventType.Quit:
						// On macOS, we'd like to restrict Cmd + Q from suddenly exiting the game.
						if (Platform.CurrentPlatform != PlatformType.OSX || !mods.HasModifier(Modifiers.Meta))
							Game.Exit();

						break;

					case EventType.Windowevent:
					{
						switch ((WindowEventID)e.Window.WindowID)
						{
							case WindowEventID.FocusLost:
								device.HasInputFocus = false;
								break;

							case WindowEventID.FocusGained:
								device.HasInputFocus = true;
								break;

							// Triggered when moving between displays with different DPI settings
							case WindowEventID.SizeChanged:
								device.WindowSizeChanged();
								break;

							case WindowEventID.Hidden:
							case WindowEventID.Minimized:
								device.IsSuspended = true;
								break;

							case WindowEventID.Exposed:
							case WindowEventID.Shown:
							case WindowEventID.Maximized:
							case WindowEventID.Restored:
								device.IsSuspended = false;
								break;
						}

						break;
					}

					case EventType.Mousebuttondown:
					case EventType.Mousebuttonup:
					{
						// Mouse 1, Mouse 2 and Mouse 3 are handled as mouse inputs
						// Mouse 4 and Mouse 5 are treated as (pseudo) keyboard inputs
						if (e.Button.Button == Sdl.ButtonLeft ||
							e.Button.Button == Sdl.ButtonMiddle ||
							e.Button.Button == Sdl.ButtonRight)
						{
							if (pendingMotion != null)
							{
								inputHandler.OnMouseInput(pendingMotion.Value);
								pendingMotion = null;
							}

							var button = MakeButton(e.Button.Button);

							if ((EventType)e.Type == EventType.Mousebuttondown)
								lastButtonBits |= button;
							else
								lastButtonBits &= ~button;

							var input = lockedMousePosition ?? new int2(e.Button.X, e.Button.Y);
							var pos = EventPosition(device, input.X, input.Y);

							if ((EventType)e.Type == EventType.Mousebuttondown)
								inputHandler.OnMouseInput(new MouseInput(
									MouseInputEvent.Down, button, pos, int2.Zero, mods,
									MultiTapDetection.DetectFromMouse(e.Button.Button, pos)));
							else
								inputHandler.OnMouseInput(new MouseInput(
									MouseInputEvent.Up, button, pos, int2.Zero, mods,
									MultiTapDetection.InfoFromMouse(e.Button.Button)));
						}

						if (e.Button.Button == Sdl.ButtonX1 ||
							e.Button.Button == Sdl.ButtonX2)
						{
							Keycode keyCode;

							if (e.Button.Button == Sdl.ButtonX1)
								keyCode = Keycode.MOUSE4;
							else
								keyCode = Keycode.MOUSE5;

							var type = (EventType)e.Type == EventType.Mousebuttondown ?
								KeyInputEvent.Down : KeyInputEvent.Up;

							var tapCount = (EventType)e.Type == EventType.Mousebuttondown ?
								MultiTapDetection.DetectFromKeyboard(keyCode, mods) :
								MultiTapDetection.InfoFromKeyboard(keyCode, mods);

							var keyEvent = new KeyInput
							{
								Event = type,
								Key = keyCode,
								Modifiers = mods,
								UnicodeChar = '?',
								MultiTapCount = tapCount,
								IsRepeat = e.Key.Repeat != 0
							};
							inputHandler.OnKeyInput(keyEvent);
						}

						break;
					}

					case EventType.Mousemotion:
					{
						var mousePos = new int2(e.Motion.X, e.Motion.Y);
						var input = lockedMousePosition ?? mousePos;
						var pos = EventPosition(device, input.X, input.Y);

						var delta = lockedMousePosition == null
							? EventPosition(device, e.Motion.Xrel, e.Motion.Yrel)
							: mousePos - lockedMousePosition.Value;

						pendingMotion = new MouseInput(
							MouseInputEvent.Move, lastButtonBits, pos, delta, mods, 0);

						break;
					}

					case EventType.Mousewheel:
					{
						var x = 0;
						var y = 0;
						sdl.GetMouseState(ref x, ref y);

						var pos = EventPosition(device, x, y);
						inputHandler.OnMouseInput(new MouseInput(MouseInputEvent.Scroll, MouseButton.None, pos, new int2(0, e.Wheel.Y), mods, 0));

						break;
					}

					case EventType.Textinput:
					{
						var rawBytes = new byte[Sdl.TextinputeventTextSize];
						unsafe { Marshal.Copy((IntPtr)e.Text.Text, rawBytes, 0, Sdl.TextinputeventTextSize); }
						inputHandler.OnTextInput(Encoding.UTF8.GetString(rawBytes, 0, rawBytes.IndexOf((byte)0)));
						break;
					}

					case EventType.Keydown:
					case EventType.Keyup:
					{
						var keyCode = (Keycode)e.Key.Keysym.Sym;
						var type = (EventType)e.Type == EventType.Keydown ?
							KeyInputEvent.Down : KeyInputEvent.Up;

						var tapCount = (EventType)e.Type == EventType.Keydown ?
							MultiTapDetection.DetectFromKeyboard(keyCode, mods) :
							MultiTapDetection.InfoFromKeyboard(keyCode, mods);

						var keyEvent = new KeyInput
						{
							Event = type,
							Key = keyCode,
							Modifiers = mods,
							UnicodeChar = (char)e.Key.Keysym.Sym,
							MultiTapCount = tapCount,
							IsRepeat = e.Key.Repeat != 0
						};

						// Special case workaround for windows users
						if (e.Key.Keysym.Sym == (int)Keycode.F4 && mods.HasModifier(Modifiers.Alt) &&
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
