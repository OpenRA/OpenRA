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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns moving sprite effects.")]
	public class FloatingSpriteEmitterInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("The time between individual particle creation. Two values mean actual lifetime will vary between them.")]
		public readonly int[] Lifetime;

		[FieldLoader.Require]
		[Desc("The time in ticks until stop spawning. -1 means forever.")]
		public readonly int Duration = -1;

		[Desc("Randomised offset for the particle emitter.")]
		public readonly WVec[] Offset = { WVec.Zero };

		[Desc("Randomized particle forward movement.")]
		public readonly WDist[] Speed = { WDist.Zero };

		[Desc("Randomized particle gravity.")]
		public readonly WDist[] Gravity = { WDist.Zero };

		[Desc("Randomize particle facing.")]
		public readonly bool RandomFacing = true;

		[Desc("Randomize particle turnrate.")]
		public readonly int TurnRate = 0;

		[Desc("The rate at which particle movement properties are reset.")]
		public readonly int RandomRate = 4;

		[Desc("How many particles should spawn. Two values for a random range.")]
		public readonly int[] SpawnFrequency = { 1 };

		[Desc("Which image to use.")]
		public readonly string Image = "smoke";

		[Desc("Which sequence to use.")]
		[SequenceReference(nameof(Image))]
		public readonly string[] Sequences = { "particles" };

		[Desc("Which palette to use.")]
		[PaletteReference(nameof(IsPlayerPalette))]
		public readonly string Palette = "effect";

		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new FloatingSpriteEmitter(init.Self, this); }
	}

	public class FloatingSpriteEmitter : ConditionalTrait<FloatingSpriteEmitterInfo>, ITick
	{
		readonly WVec offset;

		IFacing facing;
		int ticks;
		int duration;

		public FloatingSpriteEmitter(Actor self, FloatingSpriteEmitterInfo info)
			: base(info)
		{
			offset = Util.RandomVector(self.World.SharedRandom, Info.Offset);
		}

		protected override void Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();

			base.Created(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);

			duration = Info.Duration;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Info.Duration > 0 && --duration < 0)
				return;

			if (--ticks < 0)
			{
				ticks = Util.RandomInRange(self.World.LocalRandom, Info.SpawnFrequency);

				var spawnFacing = (!Info.RandomFacing && facing != null) ? facing.Facing : WAngle.FromFacing(self.World.LocalRandom.Next(256));
				self.World.AddFrameEndTask(w => w.Add(new FloatingSprite(self, Info.Image, Info.Sequences, Info.Palette, Info.IsPlayerPalette,
					Info.Lifetime, Info.Speed, Info.Gravity, Info.TurnRate, Info.RandomRate, self.CenterPosition + offset, spawnFacing)));
			}
		}
	}
}
