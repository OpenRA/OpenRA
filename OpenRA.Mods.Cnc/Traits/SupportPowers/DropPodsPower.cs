#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class DropPodsPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Drop pod unit")]
		[ActorReference(new[] { typeof(AircraftInfo), typeof(FallsToEarthInfo) })]
		public readonly string[] UnitTypes = null;

		[Desc("Number of drop pods spawned.")]
		public readonly int2 Drops = new int2(5, 8);

		[Desc("Sets the approach direction.")]
		public readonly WAngle PodFacing = new WAngle(128);

		[Desc("Maximum offset from targetLocation")]
		public readonly int PodScatter = 3;

		[Desc("Effect sequence sprite image")]
		public readonly string EntryEffect = "podring";

		[Desc("Effect sequence to display in the air.")]
		[SequenceReference(nameof(EntryEffect))]
		public readonly string EntryEffectSequence = "idle";

		[PaletteReference]
		public readonly string EntryEffectPalette = "effect";

		[ActorReference]
		[Desc("Actor to spawn when the attack starts")]
		public readonly string CameraActor = null;

		[Desc("Number of ticks to keep the camera alive")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Which weapon to fire")]
		[WeaponReference]
		public readonly string Weapon = "Vulcan2";

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Apply the weapon impact this many ticks into the effect")]
		public readonly int WeaponDelay = 0;

		public override object Create(ActorInitializer init) { return new DropPodsPower(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var weaponToLower = (Weapon ?? string.Empty).ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weapon;

			base.RulesetLoaded(rules, ai);
		}
	}

	public class DropPodsPower : SupportPower
	{
		readonly DropPodsPowerInfo info;

		public DropPodsPower(Actor self, DropPodsPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			SendDropPods(self, order, info.PodFacing);
		}

		public void SendDropPods(Actor self, Order order, WAngle facing)
		{
			var actorInfo = self.World.Map.Rules.Actors[info.UnitTypes.First().ToLowerInvariant()];
			var aircraftInfo = actorInfo.TraitInfo<AircraftInfo>();
			var altitude = aircraftInfo.CruiseAltitude.Length;
			var approachRotation = WRot.FromYaw(facing);
			var fallsToEarthInfo = actorInfo.TraitInfo<FallsToEarthInfo>();
			var delta = new WVec(0, -altitude * aircraftInfo.Speed / fallsToEarthInfo.Velocity.Length, 0).Rotate(approachRotation);

			self.World.AddFrameEndTask(w =>
			{
				var target = order.Target.CenterPosition;
				var targetCell = self.World.Map.CellContaining(target);
				var podLocations = self.World.Map.FindTilesInCircle(targetCell, info.PodScatter)
					.Where(c => aircraftInfo.LandableTerrainTypes.Contains(w.Map.GetTerrainInfo(c).Type)
						&& !self.World.ActorMap.GetActorsAt(c).Any());

				if (!podLocations.Any())
					return;

				if (info.CameraActor != null)
				{
					var camera = w.CreateActor(info.CameraActor, new TypeDictionary
					{
						new LocationInit(targetCell),
						new OwnerInit(self.Owner),
					});

					camera.QueueActivity(new Wait(info.CameraRemoveDelay));
					camera.QueueActivity(new RemoveSelf());
				}

				PlayLaunchSounds();

				var drops = self.World.SharedRandom.Next(info.Drops.X, info.Drops.Y);
				for (var i = 0; i < drops; i++)
				{
					var unitType = info.UnitTypes.Random(self.World.SharedRandom);
					var podLocation = podLocations.Random(self.World.SharedRandom);
					var podTarget = Target.FromCell(w, podLocation);
					var location = self.World.Map.CenterOfCell(podLocation) - delta + new WVec(0, 0, altitude);

					var pod = w.CreateActor(false, unitType, new TypeDictionary
					{
						new CenterPositionInit(location),
						new OwnerInit(self.Owner),
						new FacingInit(facing)
					});

					var aircraft = pod.Trait<Aircraft>();
					if (!aircraft.CanLand(podLocation))
						pod.Dispose();
					else
					{
						w.Add(new DropPodImpact(self.Owner, info.WeaponInfo, w, location, podTarget, info.WeaponDelay,
							info.EntryEffect, info.EntryEffectSequence, info.EntryEffectPalette));
						w.Add(pod);
					}
				}
			});
		}
	}
}
