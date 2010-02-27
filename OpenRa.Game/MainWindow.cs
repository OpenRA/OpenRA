#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Windows.Forms;

namespace OpenRA
{
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

	public struct MouseInput
	{
		public MouseInputEvent Event;
		public int2 Location;
		public MouseButton Button;
		public Modifiers Modifiers;
	}

	public enum MouseInputEvent { Down, Move, Up };
}
