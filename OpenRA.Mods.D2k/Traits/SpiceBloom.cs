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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Seeds resources by explosive eruptions after accumulation times.")]
	public class SpiceBloomInfo : ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>
	{
		[ActorReference]
		public readonly string SpawnActor = "spicebloom.spawnpoint";

		[SequenceReference]
		public readonly string[] GrowthSequences = { "grow1", "grow2", "grow3" };

		[Desc("The range of time (in ticks) that the spicebloom will take to respawn.")]
		public readonly int[] RespawnDelay = { 1500, 2500 };

		[Desc("The range of time (in ticks) that the spicebloom will take to grow.")]
		public readonly int[] GrowthDelay = { 1000, 3000 };

		public readonly string ResourceType = "Spice";

		[Desc("Spice blooms only grow on these terrain types.")]
		public readonly HashSet<string> GrowthTerrainTypes = new HashSet<string>();

		[Desc("The weapon to use for spice creation.")]
		[WeaponReference]
		public readonly string Weapon = null;

		[Desc("The amount of spice to expel.")]
		public readonly int[] Pieces = { 2, 12 };

		[Desc("The maximum distance in cells that spice may be expelled.")]
		public readonly int Range = 5;

		public object Create(ActorInitializer init) { return new SpiceBloom(init, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), GrowthSequences[0]));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	public class SpiceBloom : ITick, INotifyKilled
	{
		readonly Actor self;
		readonly SpiceBloomInfo info;
		readonly ResourceType resType;
		readonly ResourceLayer resLayer;
		readonly AnimationWithOffset anim;

		readonly int respawnTicks;
		readonly int growTicks;
		int ticks;

		public SpiceBloom(ActorInitializer init, SpiceBloomInfo info)
		{
			this.info = info;
			self = init.Self;

			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			resType = self.World.WorldActor.TraitsImplementing<ResourceType>().First(t => t.Info.Name == info.ResourceType);

			var render = self.Trait<RenderSprites>();
			anim = new AnimationWithOffset(new Animation(init.Self.World, render.GetImage(self)), null, () => self.IsDead);
			render.Add(anim);

			respawnTicks = self.World.SharedRandom.Next(info.RespawnDelay[0], info.RespawnDelay[1]);
			growTicks = self.World.SharedRandom.Next(info.GrowthDelay[0], info.GrowthDelay[1]);
			anim.Animation.Play(info.GrowthSequences[0]);
		}

		public void Tick(Actor self)
		{
			if (!self.World.Map.Contains(self.Location))
				return;

			if (info.GrowthTerrainTypes.Count > 0 && !info.GrowthTerrainTypes.Contains(self.World.Map.GetTerrainInfo(self.Location).Type))
				return;

			ticks++;

			if (ticks >= growTicks)
				self.Kill(self);
			else
			{
				var index = info.GrowthSequences.Length * ticks / growTicks;
				anim.Animation.Play(info.GrowthSequences[index]);
			}
		}

		void SeedResources(Actor self)
		{
			var pieces = self.World.SharedRandom.Next(info.Pieces[0], info.Pieces[1]) * ticks / growTicks;
			if (pieces < info.Pieces[0])
				pieces = info.Pieces[0];

			var cells = self.World.Map.FindTilesInAnnulus(self.Location, 1, info.Range);

			for (var i = 0; i < pieces; i++)
			{
				var cell = cells.SkipWhile(p => resLayer.GetResource(p) == resType && resLayer.IsFull(p)).Cast<CPos?>().RandomOrDefault(self.World.SharedRandom);
				if (cell == null)
					cell = cells.Random(self.World.SharedRandom);

				var args = new ProjectileArgs
				{
					Weapon = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()],
					Facing = 0,

					DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray(),

					InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray(),

					Source = self.CenterPosition,
					CurrentSource = () => self.CenterPosition,
					SourceActor = self,
					PassiveTarget = self.World.Map.CenterOfCell(cell.Value)
				};

				self.World.AddFrameEndTask(_ =>
				{
					if (args.Weapon.Projectile != null)
					{
						var projectile = args.Weapon.Projectile.Create(args);
						if (projectile != null)
							self.World.Add(projectile);

						if (args.Weapon.Report != null && args.Weapon.Report.Any())
							Game.Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
					}
				});
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!string.IsNullOrEmpty(info.Weapon))
				SeedResources(self);

			self.World.AddFrameEndTask(t => t.Add(new DelayedAction(respawnTicks, () =>
			{
				var td = new TypeDictionary
				{
					new ParentActorInit(self),
					new LocationInit(self.Location),
					new CenterPositionInit(self.CenterPosition),
					new OwnerInit(self.Owner),
					new FactionInit(self.Owner.Faction.InternalName),
					new SkipMakeAnimsInit()
				};
				self.World.CreateActor(info.SpawnActor, td);
			})));
		}
	}
}
