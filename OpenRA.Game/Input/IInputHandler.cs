#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA
{
	public interface IInputHandler
	{
		void ModifierKeys(Modifiers mods);
		void OnKeyInput(KeyInput input);
		void OnMouseInput(MouseInput input);
		void OnTextInput(string text);
	}

	public enum MouseInputEvent { Down, Move, Up, Scroll }
	public struct MouseInput
	{
		public MouseInputEvent Event;
		public MouseButton Button;
		public int ScrollDelta;
		public int2 Location;
		public Modifiers Modifiers;
		public int MultiTapCount;

		public MouseInput(MouseInputEvent ev, MouseButton button, int scrollDelta, int2 location, Modifiers mods, int multiTapCount)
		{
			Event = ev;
			Button = button;
			ScrollDelta = scrollDelta;
			Location = location;
			Modifiers = mods;
			MultiTapCount = multiTapCount;
		}
	}

	[Flags]
	public enum MouseButton
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middle = 4
	}

	[Flags]
	public enum Modifiers
	{
		None = 0,
		Shift = 1,
		Alt = 2,
		Ctrl = 4,
		Meta = 8,
	}

	public enum KeyInputEvent { Down, Up }
	public struct KeyInput
	{
		public KeyInputEvent Event;
		public Keycode Key;
		public Modifiers Modifiers;
		public int MultiTapCount;
		public char UnicodeChar;
		public bool IsRepeat;
	}
}
