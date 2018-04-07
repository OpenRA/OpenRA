#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class SmokeParticleEmitterInfo : ConditionalTraitInfo, ISmokeParticleInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("The duration of an individual particle. Two values mean actual lifetime will vary between them.")]
		public readonly int[] Duration;

		[Desc("Offset for the particle emitter.")]
		public readonly WVec[] Offset = { WVec.Zero };

		[Desc("Randomize particle gravity.")]
		public readonly WVec[] Gravity = { WVec.Zero };

		[Desc("How many particles should spawn.")]
		public readonly int[] SpawnFrequency = { 100, 150 };

		[Desc("Which image to use.")]
		public readonly string Image = "particles";

		[FieldLoader.Require]
		[Desc("Which sequence to use.")]
		[SequenceReference("Image")] public readonly string Sequence = null;

		[Desc("Which palette to use.")]
		[PaletteReference] public readonly string Palette = null;

		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml, if defined, as well.")]
		public readonly string Weapon = null;

		public WeaponInfo WeaponInfo { get; private set; }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (string.IsNullOrEmpty(Weapon))
				return;

			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
		}

		public override object Create(ActorInitializer init) { return new SmokeParticleEmitter(init.Self, this); }

		string ISmokeParticleInfo.Image
		{
			get { return Image; }
		}

		string ISmokeParticleInfo.Sequence
		{
			get { return Sequence; }
		}

		string ISmokeParticleInfo.Palette
		{
			get { return Palette; }
		}

		WVec[] ISmokeParticleInfo.Gravity
		{
			get { return Gravity; }
		}

		int[] ISmokeParticleInfo.Duration
		{
			get { return Duration; }
		}

		WeaponInfo ISmokeParticleInfo.Weapon
		{
			get { return WeaponInfo; }
		}
	}

	public class SmokeParticleEmitter : ConditionalTrait<SmokeParticleEmitterInfo>, ITick
	{
		readonly MersenneTwister random;
		readonly WVec offset;

		int ticks;

		public SmokeParticleEmitter(Actor self, SmokeParticleEmitterInfo info)
			: base(info)
		{
			random = self.World.SharedRandom;

			offset = Info.Offset.Length == 2
				? new WVec(random.Next(Info.Offset[0].X, Info.Offset[1].X), random.Next(Info.Offset[0].Y, Info.Offset[1].Y), random.Next(Info.Offset[0].Z, Info.Offset[1].Z))
				: Info.Offset[0];
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = Info.SpawnFrequency.Length == 2 ? random.Next(Info.SpawnFrequency[0], Info.SpawnFrequency[1]) : Info.SpawnFrequency[0];

				self.World.AddFrameEndTask(w => w.Add(new SmokeParticle(self, Info, self.CenterPosition + offset)));
			}
		}
	}
}
