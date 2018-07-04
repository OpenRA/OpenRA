#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public enum AppearanceType { CenterPosition, Location, OccupiedCells, EntireFootprint }

	public class AppearsOnRadarInfo : ConditionalTraitInfo
	{
		[Desc("Specifies position type to use for radar footprint.")]
		public readonly AppearanceType AppearanceType = AppearanceType.OccupiedCells;

		[Desc("Player stances who can view this actor on radar.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Specifies RGB values (in hex) that should be added or subtracted from base radar color.")]
		public readonly Color ColorModifier = Color.Black;

		[Desc("Specifies whether ColorModifier should be subtracted instead of added to base radar color.")]
		public readonly bool SubtractColorModifier = false;

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

		Color ModifyRadarColor(Color color)
		{
			if (Info.ColorModifier == Color.Black)
				return color;

			if (Info.SubtractColorModifier)
				return Color.FromArgb(
					(color.R - Info.ColorModifier.R).Clamp(0, 255),
					(color.G - Info.ColorModifier.G).Clamp(0, 255),
					(color.B - Info.ColorModifier.B).Clamp(0, 255));

			return Color.FromArgb(
				(color.R + Info.ColorModifier.R).Clamp(0, 255),
				(color.G + Info.ColorModifier.G).Clamp(0, 255),
				(color.B + Info.ColorModifier.B).Clamp(0, 255));
		}

		void IRadarSignature.PopulateRadarSignatureCells(Actor self, List<Pair<CPos, Color>> destinationBuffer)
		{
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (IsTraitDisabled || (viewer != null && !Info.ValidStances.HasStance(self.Owner.Stances[viewer])))
				return;

			var color = Game.Settings.Game.UsePlayerStanceColors ? self.Owner.PlayerStanceColor(self) : self.Owner.Color.RGB;
			if (modifier != null)
				color = modifier.RadarColorOverride(self, color);

			color = ModifyRadarColor(color);

			if (Info.AppearanceType == AppearanceType.Location)
				destinationBuffer.Add(Pair.New(self.Location, color));
			else if (Info.AppearanceType == AppearanceType.CenterPosition)
				destinationBuffer.Add(Pair.New(self.World.Map.CellContaining(self.CenterPosition), color));
			else if (Info.AppearanceType == AppearanceType.OccupiedCells)
				foreach (var cell in self.OccupiesSpace.OccupiedCells())
					destinationBuffer.Add(Pair.New(cell.First, color));
			else
			{
				// If actor has the Building trait, use FrozenUnderFogTiles (which encompasses entire footprint),
				// else fall back to self.Location.
				var building = self.TraitOrDefault<Building>();
				if (building != null)
					foreach (var tile in building.Info.FrozenUnderFogTiles(self.Location))
						destinationBuffer.Add(Pair.New(tile, color));
				else
					destinationBuffer.Add(Pair.New(self.Location, color));
			}
		}
	}
}