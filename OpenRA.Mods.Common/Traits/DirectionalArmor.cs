#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to define weapon efficiency modifiers with different percentages per Type.")]
	public class DirectionalArmorInfo : UpgradableTraitInfo, Requires<HealthInfo>
	{
		[Desc("The cones defined for the direction of impact, in standard position.")]
		public readonly WAngle[] Directions = { WAngle.FromDegrees(45), WAngle.FromDegrees(135), WAngle.FromDegrees(225), WAngle.FromDegrees(315) };

		[Desc("The corresponding armors to the cones.")]
		public readonly string[] Armors = { "None", "Heavy", "Heavy", "Heavy" };

		public override object Create(ActorInitializer init) { return new DirectionalArmor(init.Self, this); }
	}

	public class DirectionalArmor : UpgradableTrait<DirectionalArmorInfo>
	{
		public DirectionalArmor(Actor self, DirectionalArmorInfo info) 	: base(info)
		{
			if (info.Directions.Length < info.Armors.Length)
				throw new System.Exception("The number of directions is less than the amount of armors.");

		}

		public string GetArmor(Actor self, Actor firedBy, WPos impact)
		{
			var healthInfo = self.Info.TraitInfo<HealthInfo>();

			var vec = impact - self.CenterPosition;

			// If the impact position is within any actor's HitShape, we have a direct hit.
			if ((vec).LengthSquared <= healthInfo.Shape.DistanceFromEdge(impact, self).LengthSquared)
				vec = firedBy.CenterPosition - self.CenterPosition;

			var angle = WAngle.ArcTan(vec.Y, vec.X) - self.Orientation.Yaw;

			for (var i = 0; i < Info.Directions.Length; i++)
			{
				if (Between(angle, Info.Directions[i], Info.Directions[i + 1 == Info.Directions.Length ? 0 : i + 1]))
					return Info.Armors[i];
			}

			throw new System.Exception("This should not be possible.");
		}

		int GetRelativeAngle(WAngle rot, WAngle wht)
		{
			return (wht - rot).Angle;
		}

		bool Between(WAngle compare, WAngle a, WAngle b)
		{
			// If the cone crosses zero.
			if (b.Angle < a.Angle)
				return a.Angle <= compare.Angle && compare.Angle <= b.Angle + 1024;
			
			return a.Angle <= compare.Angle && compare.Angle <= b.Angle;
		}
	}
}