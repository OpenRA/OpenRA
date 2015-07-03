#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	public class SpicebloomInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<HealthInfo>
	{
		[SequenceReference]
		public readonly string[] GrowthSequences = { "grow1", "grow2", "grow3" };

		[Desc("The range of time (in ticks) that the spicebloom will take to respawn.")]
		public readonly int[] RespawnTime = { 1500, 2500 };

		[Desc("The range of time (in ticks) that the spicebloom will take to grow.")]
		public readonly int[] GrowthTime = { 1000, 1500 };

		public readonly string ResourceType = "Spice";

		[Desc("The weapon to use as spice.")]
		[WeaponReference]
		public readonly string Weapon = "SpiceExplosion";

		[Desc("The amount of spice to expel.")]
		public readonly int Pieces = 7;

		[Desc("The maximum distance in cells that spice may be expelled.")]
		public readonly int Range = 5;

		public object Create(ActorInitializer init)
		{
			return new Spicebloom(init, this);
		}
	}

	public class Spicebloom : ITick, INotifyKilled
	{
		readonly Actor self;
		readonly SpicebloomInfo info;
		readonly ResourceType resType;
		readonly ResourceLayer resLayer;
		readonly Health health;
		readonly AnimationWithOffset anim;

		int counter;
		int respawnTime;
		int growTime;

		public Spicebloom(ActorInitializer init, SpicebloomInfo info)
		{
			this.info = info;
			self = init.Self;

			health = self.Trait<Health>();
			health.RemoveOnDeath = false;

			resType = self.World.WorldActor.TraitsImplementing<ResourceType>()
							.First(t => t.Info.Name == info.ResourceType);

			resLayer = self.World.WorldActor.Trait<ResourceLayer>();

			var render = self.Trait<RenderSprites>();

			anim = new AnimationWithOffset(new Animation(init.Self.World, render.GetImage(self)), null, () => self.IsDead);

			render.Add(anim);

			respawnTime = self.World.SharedRandom.Next(info.RespawnTime[0], info.RespawnTime[1]);
			growTime = self.World.SharedRandom.Next(info.GrowthTime[0], info.GrowthTime[1]);
			anim.Animation.Play(info.GrowthSequences[0]);
		}

		public void Tick(Actor self)
		{
			if (!self.IsDead)
			{
				counter++;

				if (counter >= growTime)
					self.Kill(self);
				else
				{
					var index = info.GrowthSequences.Length * counter / growTime;
					anim.Animation.Play(info.GrowthSequences[index]);
				}
			}
			else
			{
				counter++;

				if (counter >= respawnTime)
				{
					counter = 0;
					health.Resurrect(self, self);
					respawnTime = self.World.SharedRandom.Next(info.RespawnTime[0], info.RespawnTime[1]);
					growTime = self.World.SharedRandom.Next(info.GrowthTime[0], info.GrowthTime[1]);
					anim.Animation.Play(info.GrowthSequences[0]);
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			counter = 0;

			var wep = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];

			for (var i = 0; info.Pieces > i; i++)
			{
				var cells = OpenRA.Traits.Util.RandomWalk(self.Location, self.World.SharedRandom);
				var cell = cells.Take(info.Range).SkipWhile(p => resLayer.GetResource(p) == resType && resLayer.IsFull(p)).Cast<CPos?>().RandomOrDefault(self.World.SharedRandom);

				if (cell == null)
					cell = cells.Take(info.Range).Random(self.World.SharedRandom);

				var args = new ProjectileArgs
				{
					Weapon = wep,
					Facing = 0,

					DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray(),

					InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray(),

					Source = self.CenterPosition,
					SourceActor = self,
					PassiveTarget = self.World.Map.CenterOfCell(cell.Value)
				};

				self.World.AddFrameEndTask(x =>
				{
					if (args.Weapon.Projectile != null)
					{
						var projectile = args.Weapon.Projectile.Create(args);
						if (projectile != null)
							self.World.Add(projectile);

						if (args.Weapon.Report != null && args.Weapon.Report.Any())
							Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
					}
				});
			}
		}
	}
}
