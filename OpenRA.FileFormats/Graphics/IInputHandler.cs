#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

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

		public MouseInput( MouseInputEvent ev, MouseButton button, int2 location, Modifiers mods )
		{
			this.Event = ev;
			this.Button = button;
			this.Location = location;
			this.Modifiers = mods;
		}
	}

	public enum MouseInputEvent { Down, Move, Up };

	[Flags]
	public enum MouseButton
	{
		None = (int)MouseButtons.None,
		Left = (int)MouseButtons.Left,
		Right = (int)MouseButtons.Right,
		Middle = (int)MouseButtons.Middle,
	}

	[Flags]
	public enum Modifiers
	{
		None = (int)Keys.None,
		Shift = (int)Keys.Shift,
		Alt = (int)Keys.Alt,
		Ctrl = (int)Keys.Control,
	}

	public enum KeyInputEvent { Down, Up };
	public struct KeyInput
	{
		public KeyInputEvent Event;
		public char KeyChar;
		public string KeyName;
		public Modifiers Modifiers;
		public int VirtKey;
	}
}
