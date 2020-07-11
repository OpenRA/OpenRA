#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting.Global
{
	[ScriptGlobal("Facing")]
	public class FacingGlobal : ScriptGlobal
	{
		public FacingGlobal(ScriptContext context)
			: base(context) { }

		void Deprecated()
		{
			Game.Debug("The Facing table is deprecated. Use Angle instead.");
		}

		public int North { get { Deprecated(); return 0; } }
		public int NorthWest { get { Deprecated(); return 32; } }
		public int West { get { Deprecated(); return 64; } }
		public int SouthWest { get { Deprecated(); return 96; } }
		public int South { get { Deprecated(); return 128; } }
		public int SouthEast { get { Deprecated(); return 160; } }
		public int East { get { Deprecated(); return 192; } }
		public int NorthEast { get { Deprecated(); return 224; } }
	}
}
