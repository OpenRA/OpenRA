#region Copyright & License Information
/*
* Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see COPYING.
*/
#endregion

using System;
using System.IO;
using Tao.Sdl;

namespace OpenRA.Renderer.SdlCommon
{
	public class SdlInput
	{
		MouseButton lastButtonBits = (MouseButton)0;

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
					OpenRA.Game.Exit();
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
					{
						var keyName = Sdl.SDL_GetKeyName(e.key.keysym.sym);

						var keyEvent = new KeyInput
						{
							Event = KeyInputEvent.Down,
							Key = (Keycode)e.key.keysym.sym,
							Modifiers = mods,
							UnicodeChar = (char)e.key.keysym.unicode,
							MultiTapCount = MultiTapDetection.DetectFromKeyboard(keyName),
							KeyName = Sdl.SDL_GetKeyName(e.key.keysym.sym),
							VirtKey = e.key.keysym.sym
						};

						// Special case workaround for windows users
						if (e.key.keysym.sym == Sdl.SDLK_F4 && mods.HasModifier(Modifiers.Alt) &&
						    Platform.CurrentPlatform == PlatformType.Windows)
						{
							OpenRA.Game.Exit();
						}
						else
							inputHandler.OnKeyInput(keyEvent);

						break;
					}

				case Sdl.SDL_KEYUP:
					{
						var keyName = Sdl.SDL_GetKeyName(e.key.keysym.sym);
						var keyEvent = new KeyInput
						{
							Event = KeyInputEvent.Up,
							Key = (Keycode)e.key.keysym.sym,
							Modifiers = mods,
							UnicodeChar = (char)e.key.keysym.unicode,
							MultiTapCount = MultiTapDetection.InfoFromKeyboard(keyName),
							KeyName = Sdl.SDL_GetKeyName(e.key.keysym.sym),
							VirtKey = e.key.keysym.sym
						};

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

			ErrorHandler.CheckGlError();
		}
	}
}