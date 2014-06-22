#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ParachutableInfo : ITraitInfo
	{
		public readonly bool KilledOnImpassableTerrain = true;

		public readonly string GroundImpactSound = "squishy2.aud";
		public readonly string GroundCorpseSequence = "corpse";
		public readonly string GroundCorpsePalette = "effect";

		public readonly string WaterImpactSound = "splash9.aud";
		public readonly string WaterCorpseSequence = "small_splash";
		public readonly string WaterCorpsePalette = "effect";

		public readonly string ParachuteSprite = "parach";
		public readonly WVec ParachuteOffset = WVec.Zero;

		public object Create(ActorInitializer init) { return new Parachutable(init, this); }
	}

	class Parachutable : INotifyParachuteLanded
	{
		readonly Actor self;
		readonly ParachutableInfo info;
		readonly IPositionable positionable;

		public Parachutable(ActorInitializer init, ParachutableInfo info)
		{
			this.self = init.self;
			this.info = info;

			positionable = self.TraitOrDefault<IPositionable>();
		}

		public void OnLanded()
		{
			if (!info.KilledOnImpassableTerrain)
				return;

			if (positionable.CanEnterCell(self.Location))
				return;

			var terrain = self.World.Map.GetTerrainInfo(self.Location);

			var sound = terrain.IsWater ? info.WaterImpactSound : info.GroundImpactSound;
			Sound.Play(sound, self.CenterPosition);

			var sequence = terrain.IsWater ? info.WaterCorpseSequence : info.GroundCorpseSequence;
			var palette = terrain.IsWater ? info.WaterCorpsePalette : info.GroundCorpsePalette;
			self.World.AddFrameEndTask(w => w.Add(new Explosion(w, self.OccupiesSpace.CenterPosition, sequence, palette)));

			self.Kill(self);
		}
	}
}
