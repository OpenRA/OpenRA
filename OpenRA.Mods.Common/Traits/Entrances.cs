#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Entrances passangers, infiltrators, etc..")]
	public class EntrancesInfo : ConditionalTraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Create a enterable position for each offset listed here (relative to CenterPosition).")]
		public readonly WVec[] EntranceOffsets = { WVec.Zero };

		[Desc("Create a enterable position at the center of each occupied cell. Stacks with EntranceOffsets.")]
		public readonly bool UseOccupiedCellsOffsets = false;

		public override object Create(ActorInitializer init) { return new Entrances(init.Self, this); }
	}

	public class Entrances : ConditionalTrait<EntrancesInfo>, IAccessiblePositions
	{
		BodyOrientation orientation;
		ITargetableCells targetableCells;

		public Entrances(Actor self, EntrancesInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			orientation = self.Trait<BodyOrientation>();
			targetableCells = self.TraitOrDefault<ITargetableCells>();

			base.Created(self);
		}

		public IEnumerable<WPos> TargetablePositions(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			if (Info.UseOccupiedCellsOffsets && targetableCells != null)
				foreach (var c in targetableCells.TargetableCells())
					yield return self.World.Map.CenterOfCell(c.First);

			foreach (var o in Info.EntranceOffsets)
			{
				var offset = orientation.LocalToWorld(o.Rotate(orientation.QuantizeOrientation(self, self.Orientation)));
				yield return self.CenterPosition + offset;
			}
		}
	}
}