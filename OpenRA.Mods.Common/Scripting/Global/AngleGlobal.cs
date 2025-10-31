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

		[Desc("0/1024 units = 0/360 degrees")]
		public WAngle North => WAngle.Zero;
		[Desc("128 units = 315 degrees")]
		public WAngle NorthWest => new(128);
		[Desc("256 units = 270 degrees")]
		public WAngle West => new(256);
		[Desc("384 units = 225 degrees")]
		public WAngle SouthWest => new(384);
		[Desc("512 units = 180 degrees")]
		public WAngle South => new(512);
		[Desc("640 units = 135 degrees")]
		public WAngle SouthEast => new(640);
		[Desc("768 units = 90 degrees")]
		public WAngle East => new(768);
		[Desc("896 units = 45 degrees")]
		public WAngle NorthEast => new(896);

		[Desc("Create an arbitrary angle. 1024 units = 360 degrees. North is 0. " +
			"Units increase *counter* clockwise. Comparison given to degrees increasing clockwise.")]
		public WAngle New(int a) { return new WAngle(a); }
	}
}
