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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Seeds resources by explosive eruptions after accumulation times.")]
	public class SpiceBloomInfo : TraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		public readonly string[] GrowthSequences = { "grow1", "grow2", "grow3" };

		[SequenceReference]
		public readonly string SpurtSequence = "spurt";

		[Desc("The range of time (in ticks) that the spicebloom will take to grow until it blows up.")]
		public readonly int[] Lifetime = { 1000, 3000 };

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

		public override object Create(ActorInitializer init) { return new SpiceBloom(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), GrowthSequences[0]));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	public class SpiceBloom : ITick, INotifyKilled
	{
		readonly SpiceBloomInfo info;
		readonly IResourceLayer resourceLayer;
		readonly Animation body;
		readonly Animation spurt;
		readonly int growTicks;

		int ticks;
		int bodyFrame = 0;
		bool showSpurt = true;

		public SpiceBloom(Actor self, SpiceBloomInfo info)
		{
			this.info = info;

			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();

			var rs = self.Trait<RenderSprites>();
			body = new Animation(self.World, rs.GetImage(self));
			rs.Add(new AnimationWithOffset(body, null, () => self.IsDead));

			growTicks = self.World.SharedRandom.Next(info.Lifetime[0], info.Lifetime[1]);
			body.Play(info.GrowthSequences[0]);

			spurt = new Animation(self.World, rs.GetImage(self));
			rs.Add(new AnimationWithOffset(spurt, null, () => !showSpurt));
			spurt.PlayThen(info.SpurtSequence, () => showSpurt = false);
		}

		void ITick.Tick(Actor self)
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
				var newBodyFrame = info.GrowthSequences.Length * ticks / growTicks;
				if (newBodyFrame != bodyFrame)
				{
					bodyFrame = newBodyFrame;
					body.Play(info.GrowthSequences[bodyFrame]);

					showSpurt = true;
					spurt.PlayThen(info.SpurtSequence, () => showSpurt = false);
				}
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
				var cell = cells
					.SkipWhile(p => resourceLayer.GetResource(p).Type == info.ResourceType && !resourceLayer.CanAddResource(info.ResourceType, p))
					.Cast<CPos?>()
					.RandomOrDefault(self.World.SharedRandom);

				if (cell == null)
					cell = cells.Random(self.World.SharedRandom);

				var args = new ProjectileArgs
				{
					Weapon = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()],
					Facing = WAngle.Zero,
					CurrentMuzzleFacing = () => WAngle.Zero,

					DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray(),

					InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray(),

					RangeModifiers = self.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray(),

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

						if (args.Weapon.Report != null && args.Weapon.Report.Length > 0)
							Game.Sound.Play(SoundType.World, args.Weapon.Report, self.World, self.CenterPosition);
					}
				});
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!string.IsNullOrEmpty(info.Weapon))
				SeedResources(self);
		}
	}
}
