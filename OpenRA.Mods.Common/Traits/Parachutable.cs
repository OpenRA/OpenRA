#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be paradropped by a ParaDrop actor.")]
	public class ParachutableInfo : ITraitInfo, Requires<IPositionableInfo>
	{
		[Desc("If we land on invalid terrain for my actor type should we be killed?")]
		public readonly bool KilledOnImpassableTerrain = true;

		[Desc("Image where Ground/WaterCorpseSequence is looked up.")]
		public readonly string Image = "explosion";

		public readonly string GroundImpactSound = null;
		[SequenceReference("Image")] public readonly string GroundCorpseSequence = "corpse";
		[PaletteReference] public readonly string GroundCorpsePalette = "effect";

		public readonly string WaterImpactSound = null;
		[SequenceReference("Image")] public readonly string WaterCorpseSequence = null;
		[PaletteReference] public readonly string WaterCorpsePalette = "effect";

		[Desc("Terrain types on which to display WaterCorpseSequence.")]
		public readonly HashSet<string> WaterTerrainTypes = new HashSet<string> { "Water" };

		public readonly int FallRate = 13;

		[UpgradeGrantedReference]
		[Desc("Upgrade to grant to this actor when parachuting. Normally used to render the parachute using the WithParachute trait.")]
		public readonly string[] ParachuteUpgrade = { "parachute" };

		public object Create(ActorInitializer init) { return new Parachutable(init, this); }
	}

	class Parachutable : INotifyParachuteLanded
	{
		readonly Actor self;
		readonly ParachutableInfo info;
		readonly IPositionable positionable;

		public Parachutable(ActorInitializer init, ParachutableInfo info)
		{
			self = init.Self;
			this.info = info;
			positionable = self.Trait<IPositionable>();
		}

		void INotifyParachuteLanded.OnLanded(Actor ignore)
		{
			if (!info.KilledOnImpassableTerrain)
				return;

			var cell = self.Location;
			if (positionable.CanEnterCell(cell, self))
				return;

			if (ignore != null && self.World.ActorMap.GetActorsAt(cell).Any(a => a != ignore))
				return;

			var onWater = info.WaterTerrainTypes.Contains(self.World.Map.GetTerrainInfo(cell).Type);

			var sound = onWater ? info.WaterImpactSound : info.GroundImpactSound;
			Game.Sound.Play(sound, self.CenterPosition);

			var sequence = onWater ? info.WaterCorpseSequence : info.GroundCorpseSequence;
			var palette = onWater ? info.WaterCorpsePalette : info.GroundCorpsePalette;
			if (sequence != null && palette != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.OccupiesSpace.CenterPosition, w, info.Image, sequence, palette)));

			self.Kill(self);
		}
	}
}
