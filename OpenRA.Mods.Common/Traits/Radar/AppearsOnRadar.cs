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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Radar
{
	public class AppearsOnRadarInfo : ConditionalTraitInfo
	{
		public readonly bool UseLocation = false;

		[Desc("Player stances who can view this actor on radar.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		public override object Create(ActorInitializer init) { return new AppearsOnRadar(this); }
	}

	public class AppearsOnRadar : ConditionalTrait<AppearsOnRadarInfo>, IRadarSignature
	{
		static readonly IEnumerable<Pair<CPos, Color>> NoCells = Enumerable.Empty<Pair<CPos, Color>>();
		IRadarColorModifier modifier;

		Color currentColor = Color.Transparent;
		Func<Pair<CPos, SubCell>, Pair<CPos, Color>> cellToSignature;

		public AppearsOnRadar(AppearsOnRadarInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			modifier = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
		}

		public IEnumerable<Pair<CPos, Color>> RadarSignatureCells(Actor self)
		{
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (IsTraitDisabled || (viewer != null && !Info.ValidStances.HasStance(self.Owner.Stances[viewer])))
				return NoCells;

			var color = Game.Settings.Game.UsePlayerStanceColors ? self.Owner.PlayerStanceColor(self) : self.Owner.Color.RGB;
			if (modifier != null)
				color = modifier.RadarColorOverride(self, color);

			if (Info.UseLocation)
				return new[] { Pair.New(self.Location, color) };

			// PERF: Cache cellToSignature delegate to avoid allocations as color does not change often.
			if (currentColor != color)
			{
				currentColor = color;
				cellToSignature = c => Pair.New(c.First, color);
			}

			return self.OccupiesSpace.OccupiedCells().Select(cellToSignature);
		}
	}
}