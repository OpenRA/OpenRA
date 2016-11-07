#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class SmokeParticleEmitterInfo : UpgradableTraitInfo
	{
		[Desc("Offset for the particle emitter.")]
		public readonly WVec[] Offset = { WVec.Zero };

		[Desc("Randomize particle gravity.")]
		public readonly WVec[] Gravity = { new WVec(0, 1, 0) };

		[Desc("How many particles should spawn.")]
		public readonly int[] SpawnFrequency = { 100, 150 };

		[Desc("Which image to use.")]
		public readonly string Image = "particles";

		[Desc("Which sequence to use.")]
		[SequenceReference("Image")] public readonly string Sequence = null;

		[Desc("Which palette to use.")]
		[PaletteReference] public readonly string Palette = null;

		public override object Create(ActorInitializer init) { return new SmokeParticleEmitter(init.Self, this); }
	}

	public class SmokeParticleEmitter : UpgradableTrait<SmokeParticleEmitterInfo>, ITick
	{
		readonly WPos spawnpos;
		readonly MersenneTwister random;

		int ticks;

		public SmokeParticleEmitter(Actor self, SmokeParticleEmitterInfo info)
			: base(info)
		{
			random = self.World.SharedRandom;

			var offset = Info.Offset.Length == 2
				? new WVec(random.Next(Info.Offset[0].X, Info.Offset[1].X), random.Next(Info.Offset[0].Y, Info.Offset[1].Y), random.Next(Info.Offset[0].Z, Info.Offset[1].Z))
				: Info.Offset[0];

			spawnpos = new WPos(self.CenterPosition.X + offset.X, self.CenterPosition.Y + offset.Y, self.CenterPosition.Z + offset.Z);
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = Info.SpawnFrequency.Length == 2 ? random.Next(Info.SpawnFrequency[0], Info.SpawnFrequency[1]) : Info.SpawnFrequency[0];

				var gravity = Info.Gravity.Length == 2
					? new WVec(random.Next(Info.Gravity[0].X, Info.Gravity[1].X), random.Next(Info.Gravity[0].Y, Info.Gravity[1].Y),
						random.Next(Info.Gravity[0].Z, Info.Gravity[1].Z))
					: Info.Gravity[0];

				self.World.AddFrameEndTask(w => w.Add(new SmokeParticle(spawnpos, gravity, w, Info.Image, Info.Sequence, Info.Palette, false, false)));
			}
		}
	}
}
