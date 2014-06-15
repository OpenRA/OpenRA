#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	public class BodyOrientationInfo : ITraitInfo, IBodyOrientationInfo
	{
		[Desc("Number of facings for gameplay calculations. -1 indiciates auto-detection from sequence")]
		public readonly int QuantizedFacings = -1;

		[Desc("Camera pitch for rotation calculations")]
		public readonly WAngle CameraPitch = WAngle.FromDegrees(40);
		public object Create(ActorInitializer init) { return new BodyOrientation(init.self, this); }
	}

	public class BodyOrientation : IBodyOrientation
	{
		[Sync] public int QuantizedFacings { get; private set; }
		BodyOrientationInfo info;

		public BodyOrientation(Actor self, BodyOrientationInfo info)
		{
			this.info = info;
			if (info.QuantizedFacings > 0)
				QuantizedFacings = info.QuantizedFacings;
		}

		public WAngle CameraPitch { get { return info.CameraPitch; } }

		public WVec LocalToWorld(WVec vec)
		{
			// RA's 2d perspective doesn't correspond to an orthonormal 3D
			// coordinate system, so fudge the y axis to make things look good
			return new WVec(vec.Y, -info.CameraPitch.Sin()*vec.X/1024, vec.Z);
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

		public void SetAutodetectedFacings(int facings)
		{
			if (info.QuantizedFacings < 0)
				QuantizedFacings = facings;
		}
	}
}
