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
using OpenRA.Graphics;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class BodyOrientationInfo : ITraitInfo, IBodyOrientationInfo
	{
		[Desc("Camera pitch for rotation calculations")]
		public readonly WAngle CameraPitch = WAngle.FromDegrees(40);
		public object Create(ActorInitializer init) { return new BodyOrientation(init.self, this); }
	}

	public class BodyOrientation : IBodyOrientation
	{
		[Sync] public int QuantizedFacings { get; set; }
		BodyOrientationInfo Info;

		public BodyOrientation(Actor self, BodyOrientationInfo info)
		{
			Info = info;
		}

		public WAngle CameraPitch { get { return Info.CameraPitch; } }

		public WVec LocalToWorld(WVec vec)
		{
			// RA's 2d perspective doesn't correspond to an orthonormal 3D
			// coordinate system, so fudge the y axis to make things look good
			return new WVec(vec.Y, -Info.CameraPitch.Sin()*vec.X/1024, vec.Z);
		}

		public WRot QuantizeOrientation(Actor self, WRot orientation)
		{
			// Quantization disabled
			if (QuantizedFacings == 0)
				return orientation;

			// Map yaw to the closest facing
			var facing = Util.QuantizeFacing(orientation.Yaw.Angle / 4, QuantizedFacings) * (256 / QuantizedFacings);

			// Roll and pitch are always zero if yaw is quantized
			return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing));
		}
	}
}
