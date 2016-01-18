#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be paradropped by a ParaDrop actor.")]
	class ParachutableInfo : ITraitInfo
	{
		[Desc("If we land on invalid terrain for my actor type should we be killed?")]
		public readonly bool KilledOnImpassableTerrain = true;

		[Desc("Group where Ground/WaterCorpseSequence is looked up.")]
		public readonly string CorpseSequenceCollection = "explosion";

		public readonly string GroundImpactSound = null;
		[SequenceReference("CorpseSequenceCollection")] public readonly string GroundCorpseSequence = "corpse";
		[PaletteReference] public readonly string GroundCorpsePalette = "effect";

		public readonly string WaterImpactSound = null;
		[SequenceReference("CorpseSequenceCollection")] public readonly string WaterCorpseSequence = null;
		[PaletteReference] public readonly string WaterCorpsePalette = "effect";

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

			positionable = self.TraitOrDefault<IPositionable>();
		}

		public void OnLanded()
		{
			if (!info.KilledOnImpassableTerrain)
				return;

			if (positionable.CanEnterCell(self.Location, self))
				return;

			var terrain = self.World.Map.GetTerrainInfo(self.Location);

			var sound = terrain.IsWater ? info.WaterImpactSound : info.GroundImpactSound;
			Game.Sound.Play(sound, self.CenterPosition);

			var sequence = terrain.IsWater ? info.WaterCorpseSequence : info.GroundCorpseSequence;
			var palette = terrain.IsWater ? info.WaterCorpsePalette : info.GroundCorpsePalette;
			if (sequence != null && palette != null)
				self.World.AddFrameEndTask(w => w.Add(new Explosion(w, self.OccupiesSpace.CenterPosition, sequence, palette)));

			self.Kill(self);
		}
	}
}
