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

		[GrantedConditionReference]
		[Desc("The condition to grant to self while parachuting.")]
		public readonly string ParachutingCondition = null;

		public object Create(ActorInitializer init) { return new Parachutable(init.Self, this); }
	}

	class Parachutable : INotifyCreated, INotifyParachute
	{
		readonly ParachutableInfo info;
		readonly IPositionable positionable;

		ConditionManager conditionManager;
		int parachutingToken = ConditionManager.InvalidConditionToken;

		public Parachutable(Actor self, ParachutableInfo info)
		{
			this.info = info;
			positionable = self.Trait<IPositionable>();
		}

		public bool IsInAir { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyParachute.OnParachute(Actor self)
		{
			IsInAir = true;

			if (conditionManager != null && parachutingToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.ParachutingCondition))
				parachutingToken = conditionManager.GrantCondition(self, info.ParachutingCondition);
		}

		void INotifyParachute.OnLanded(Actor self, Actor ignore)
		{
			IsInAir = false;

			if (parachutingToken != ConditionManager.InvalidConditionToken)
				parachutingToken = conditionManager.RevokeCondition(self, parachutingToken);

			if (!info.KilledOnImpassableTerrain)
				return;

			var cell = self.Location;
			if (positionable.CanEnterCell(cell, self))
				return;

			if (ignore != null && self.World.ActorMap.GetActorsAt(cell).Any(a => a != ignore))
				return;

			var onWater = info.WaterTerrainTypes.Contains(self.World.Map.GetTerrainInfo(cell).Type);

			var sound = onWater ? info.WaterImpactSound : info.GroundImpactSound;
			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);

			var sequence = onWater ? info.WaterCorpseSequence : info.GroundCorpseSequence;
			var palette = onWater ? info.WaterCorpsePalette : info.GroundCorpsePalette;
			if (sequence != null && palette != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.OccupiesSpace.CenterPosition, w, info.Image, sequence, palette)));

			self.Kill(self);
		}
	}
}
