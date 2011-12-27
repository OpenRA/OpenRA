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
using System.Collections.Generic;
using System.Linq;

namespace OpenRA
{
	public interface IInputHandler
	{
		void ModifierKeys( Modifiers mods );
		void OnKeyInput( KeyInput input );
		void OnMouseInput( MouseInput input );
	}

	public struct MouseInput
	{
		public MouseInputEvent Event;
		public MouseButton Button;
		public int2 Location;
		public Modifiers Modifiers;
		public int MultiTapCount;

		public MouseInput( MouseInputEvent ev, MouseButton button, int2 location, Modifiers mods, int multiTapCount )
		{
			this.Event = ev;
			this.Button = button;
			this.Location = location;
			this.Modifiers = mods;
			this.MultiTapCount = multiTapCount;
		}
	}

	public enum MouseInputEvent { Down, Move, Up };

	[Flags]
	public enum MouseButton
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middle = 4,
		WheelDown = 8,
		WheelUp = 16
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

	public enum KeyInputEvent { Down, Up };
	public struct KeyInput
	{
		public KeyInputEvent Event;
		public char UnicodeChar;
		public string KeyName;
		public Modifiers Modifiers;
		public int VirtKey;
		public int MultiTapCount;
	}
}
