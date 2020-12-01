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

using System;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class BodyOrientationInfo : TraitInfo
	{
		[Desc("Number of facings for gameplay calculations. -1 indicates auto-detection from another trait.")]
		public readonly int QuantizedFacings = -1;

		[Desc("Camera pitch for rotation calculations.")]
		public readonly WAngle CameraPitch = WAngle.FromDegrees(40);

		[Desc("Fudge the coordinate system angles to simulate non-top-down perspective in mods with square cells.")]
		public readonly bool UseClassicPerspectiveFudge = true;

		public WVec LocalToWorld(WVec vec)
		{
			// Rotate by 90 degrees
			if (!UseClassicPerspectiveFudge)
				return new WVec(vec.Y, -vec.X, vec.Z);

			// The 2d perspective of older games with square cells doesn't correspond to an orthonormal 3D
			// coordinate system, so fudge the y axis to make things look good
			return new WVec(vec.Y, -CameraPitch.Sin() * vec.X / 1024, vec.Z);
		}

		public WRot QuantizeOrientation(in WRot orientation, int facings)
		{
			// Quantization disabled
			if (facings == 0)
				return orientation;

			// Map yaw to the closest facing
			var facing = QuantizeFacing(orientation.Yaw, facings);

			// Roll and pitch are always zero if yaw is quantized
			return WRot.FromYaw(facing);
		}

		public virtual WAngle QuantizeFacing(WAngle facing, int facings)
		{
			return Util.QuantizeFacing(facing, facings);
		}

		public override object Create(ActorInitializer init) { return new BodyOrientation(init, this); }
	}

	public class BodyOrientation : ISync
	{
		readonly BodyOrientationInfo info;
		readonly Lazy<int> quantizedFacings;

		[Sync]
		public int QuantizedFacings { get { return quantizedFacings.Value; } }

		public BodyOrientation(ActorInitializer init, BodyOrientationInfo info)
		{
			this.info = info;
			var self = init.Self;
			var faction = init.GetValue<FactionInit, string>(self.Owner.Faction.InternalName);

			quantizedFacings = Exts.Lazy(() =>
			{
				// Override value is set
				if (info.QuantizedFacings >= 0)
					return info.QuantizedFacings;

				var qboi = self.Info.TraitInfoOrDefault<IQuantizeBodyOrientationInfo>();

				// If a sprite actor has neither custom QuantizedFacings nor a trait implementing IQuantizeBodyOrientationInfo, throw
				if (qboi == null)
				{
					if (self.Info.HasTraitInfo<WithSpriteBodyInfo>())
						throw new InvalidOperationException("Actor '" + self.Info.Name + "' has a sprite body but no facing quantization."
							+ " Either add the QuantizeFacingsFromSequence trait or set custom QuantizedFacings on BodyOrientation.");
					else
						throw new InvalidOperationException("Actor type '" + self.Info.Name + "' does not define a quantized body orientation.");
				}

				return qboi.QuantizedBodyFacings(self.Info, self.World.Map.Rules.Sequences, faction);
			});
		}

		public WAngle CameraPitch { get { return info.CameraPitch; } }

		public WVec LocalToWorld(WVec vec)
		{
			return info.LocalToWorld(vec);
		}

		public WRot QuantizeOrientation(Actor self, in WRot orientation)
		{
			return info.QuantizeOrientation(orientation, quantizedFacings.Value);
		}

		public WAngle QuantizeFacing(WAngle facing)
		{
			return info.QuantizeFacing(facing, quantizedFacings.Value);
		}

		public WAngle QuantizeFacing(WAngle facing, int facings)
		{
			return info.QuantizeFacing(facing, facings);
		}
	}
}
