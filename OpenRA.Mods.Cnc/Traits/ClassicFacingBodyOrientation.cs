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

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Fudge the coordinate system angles like the early games (for sprite sequences that use classic facing fudge).")]
	public class ClassicFacingBodyOrientationInfo : BodyOrientationInfo
	{
		public override WAngle QuantizeFacing(WAngle facing, int facings)
		{
			return Util.ClassicQuantizeFacing(facing, facings);
		}

		public override object Create(ActorInitializer init) { return new ClassicFacingBodyOrientation(init, this); }
	}

	public class ClassicFacingBodyOrientation : BodyOrientation
	{
		public ClassicFacingBodyOrientation(ActorInitializer init, ClassicFacingBodyOrientationInfo info)
			: base(init, info) { }
	}
}
