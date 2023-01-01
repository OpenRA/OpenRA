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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Radar
{
	public class AppearsOnRadarInfo : ConditionalTraitInfo
	{
		public readonly bool UseLocation = false;

		[Desc("Player relationships who can view this actor on radar.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new AppearsOnRadar(this); }
	}

	public class AppearsOnRadar : ConditionalTrait<AppearsOnRadarInfo>, IRadarSignature
	{
		IRadarColorModifier modifier;

		public AppearsOnRadar(AppearsOnRadarInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			modifier = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
		}

		public void PopulateRadarSignatureCells(Actor self, List<(CPos Cell, Color Color)> destinationBuffer)
		{
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (IsTraitDisabled || (viewer != null && !Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))))
				return;

			var color = Game.Settings.Game.UsePlayerStanceColors ? self.Owner.PlayerRelationshipColor(self) : self.Owner.Color;
			if (modifier != null)
				color = modifier.RadarColorOverride(self, color);

			if (Info.UseLocation)
			{
				destinationBuffer.Add((self.Location, color));
				return;
			}

			foreach (var cell in self.OccupiesSpace.OccupiedCells())
				destinationBuffer.Add((cell.Cell, color));
		}
	}
}
