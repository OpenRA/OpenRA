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

namespace OpenRA.Traits
{
	public class BodyOrientationInfo : ITraitInfo, IBodyOrientationInfo
	{
		[Desc("Number of facings for gameplay calculations. -1 indicates auto-detection from another trait")]
		public readonly int QuantizedFacings = -1;

		[Desc("Camera pitch for rotation calculations")]
		public readonly WAngle CameraPitch = WAngle.FromDegrees(40);

		public WVec LocalToWorld(WVec vec)
		{
			// RA's 2d perspective doesn't correspond to an orthonormal 3D
			// coordinate system, so fudge the y axis to make things look good
			return new WVec(vec.Y, -CameraPitch.Sin() * vec.X / 1024, vec.Z);
		}

		public WRot QuantizeOrientation(WRot orientation, int facings)
		{
			// Quantization disabled
			if (facings == 0)
				return orientation;

			// Map yaw to the closest facing
			var facing = Util.QuantizeFacing(orientation.Yaw.Angle / 4, facings) * (256 / facings);

			// Roll and pitch are always zero if yaw is quantized
			return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing));
		}

		public object Create(ActorInitializer init) { return new BodyOrientation(init.self, this); }
	}

	public class BodyOrientation : IBodyOrientation
	{
		readonly BodyOrientationInfo info;
		readonly Lazy<int> quantizedFacings;

		[Sync] public int QuantizedFacings { get { return quantizedFacings.Value; } }

		public BodyOrientation(Actor self, BodyOrientationInfo info)
		{
			this.info = info;

			quantizedFacings = Exts.Lazy(() =>
			{
				// Override value is set
				if (info.QuantizedFacings >= 0)
					return info.QuantizedFacings;

				var qboi = self.Info.Traits.GetOrDefault<IQuantizeBodyOrientationInfo>();
				if (qboi == null)
					throw new InvalidOperationException("Actor type '" + self.Info.Name + "' does not define a quantized body orientation.");

				return qboi.QuantizedBodyFacings(self.World.Map.SequenceProvider, self.Info);
			});
		}

		public WAngle CameraPitch { get { return info.CameraPitch; } }

		public WVec LocalToWorld(WVec vec)
		{
			return info.LocalToWorld(vec);
		}

		public WRot QuantizeOrientation(Actor self, WRot orientation)
		{
			return info.QuantizeOrientation(orientation, quantizedFacings.Value);
		}
	}
}
