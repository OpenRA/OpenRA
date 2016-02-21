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

using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting.Global
{
	[ScriptGlobal("Facing")]
	public class FacingGlobal : ScriptGlobal
	{
		public FacingGlobal(ScriptContext context)
			: base(context) { }

		public int North { get { return 0; } }
		public int NorthWest { get { return 32; } }
		public int West { get { return 64; } }
		public int SouthWest { get { return 96; } }
		public int South { get { return 128; } }
		public int SouthEast { get { return 160; } }
		public int East { get { return 192; } }
		public int NorthEast { get { return 224; } }
	}
}
