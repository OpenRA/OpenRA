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

using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting.Global
{
	[ScriptGlobal("Angle")]
	public class AngleGlobal : ScriptGlobal
	{
		public AngleGlobal(ScriptContext context)
			: base(context) { }

		public WAngle North => WAngle.Zero;
		public WAngle NorthWest => new WAngle(128);
		public WAngle West => new WAngle(256);
		public WAngle SouthWest => new WAngle(384);
		public WAngle South => new WAngle(512);
		public WAngle SouthEast => new WAngle(640);
		public WAngle East => new WAngle(768);
		public WAngle NorthEast => new WAngle(896);

		[Desc("Create an arbitrary angle.")]
		public WAngle New(int a) { return new WAngle(a); }
	}
}
