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
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be paradropped by a ParaDrop actor.")]
	public class ParachutableInfo : TraitInfo, Requires<IPositionableInfo>
	{
		[Desc("If we land on invalid terrain for my actor type should we be killed?")]
		public readonly bool KilledOnImpassableTerrain = true;

		[Desc("Types of damage that this trait causes to self when 'KilledOnImpassableTerrain' is true. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Image where Ground/WaterCorpseSequence is looked up.")]
		public readonly string Image = "explosion";

		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string GroundCorpseSequence = null;

		[PaletteReference]
		public readonly string GroundCorpsePalette = "effect";

		public readonly string GroundImpactSound = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string WaterCorpseSequence = null;

		[PaletteReference]
		public readonly string WaterCorpsePalette = "effect";

		[Desc("Terrain types on which to display WaterCorpseSequence.")]
		public readonly HashSet<string> WaterTerrainTypes = new HashSet<string> { "Water" };

		public readonly string WaterImpactSound = null;

		public readonly int FallRate = 13;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while parachuting.")]
		public readonly string ParachutingCondition = null;

		public override object Create(ActorInitializer init) { return new Parachutable(init.Self, this); }
	}

	public class Parachutable : INotifyParachute
	{
		readonly ParachutableInfo info;
		readonly IPositionable positionable;

		public Actor IgnoreActor;

		int parachutingToken = Actor.InvalidConditionToken;

		public Parachutable(Actor self, ParachutableInfo info)
		{
			this.info = info;
			positionable = self.Trait<IPositionable>();
		}

		public bool IsInAir { get; private set; }

		void INotifyParachute.OnParachute(Actor self)
		{
			IsInAir = true;

			if (parachutingToken == Actor.InvalidConditionToken)
				parachutingToken = self.GrantCondition(info.ParachutingCondition);

			self.NotifyBlocker(self.Location);
		}

		void INotifyParachute.OnLanded(Actor self)
		{
			IsInAir = false;

			if (parachutingToken != Actor.InvalidConditionToken)
				parachutingToken = self.RevokeCondition(parachutingToken);

			if (!info.KilledOnImpassableTerrain)
				return;

			var cell = self.Location;
			if (positionable.CanEnterCell(cell, self))
				return;

			if (IgnoreActor != null && !self.World.ActorMap.GetActorsAt(cell)
				.Any(a => a != IgnoreActor && a != self && self.World.Map.DistanceAboveTerrain(a.CenterPosition) == WDist.Zero))
				return;

			var onWater = info.WaterTerrainTypes.Contains(self.World.Map.GetTerrainInfo(cell).Type);
			var sound = onWater ? info.WaterImpactSound : info.GroundImpactSound;
			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);

			var sequence = onWater ? info.WaterCorpseSequence : info.GroundCorpseSequence;
			var palette = onWater ? info.WaterCorpsePalette : info.GroundCorpsePalette;
			if (!string.IsNullOrEmpty(info.Image) && !string.IsNullOrEmpty(sequence) && palette != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.OccupiesSpace.CenterPosition, w, info.Image, sequence, palette)));

			self.Kill(self, info.DamageTypes);
		}
	}
}
