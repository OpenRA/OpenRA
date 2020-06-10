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
	[ScriptGlobal("Angle")]
	public class AngleGlobal : ScriptGlobal
	{
		public AngleGlobal(ScriptContext context)
			: base(context) { }

		public WAngle North { get { return WAngle.Zero; } }
		public WAngle NorthWest { get { return new WAngle(128); } }
		public WAngle West { get { return new WAngle(256); } }
		public WAngle SouthWest { get { return new WAngle(384); } }
		public WAngle South { get { return new WAngle(512); } }
		public WAngle SouthEast { get { return new WAngle(640); } }
		public WAngle East { get { return new WAngle(768); } }
		public WAngle NorthEast { get { return new WAngle(896); } }

		[Desc("Create an arbitrary angle.")]
		public WAngle New(int a) { return new WAngle(a); }
	}
}
