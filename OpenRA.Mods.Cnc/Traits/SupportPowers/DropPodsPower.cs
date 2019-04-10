#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class DropPodsPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[Desc("DropPod Unit")]
		[ActorReference(typeof(AircraftInfo))]
		public readonly string UnitType = "badr";

		[Desc("Notification to play when spawning drop pods.")]
		public readonly string DropPodsAvailableNotification = null;

		[ActorReference(typeof(PassengerInfo))]
		[Desc("Troops to be delivered.  They will each get their own pod.")]
		public readonly string[] DropItems = { };

		[Desc("Integer determining the drop pod's facing if it moves.")]
		public readonly int PodFacing = 32;

		[Desc("Integer determining maximum offset of drop pod drop from targetLocation")]
		public readonly int PodScatter = 3;

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowImpassableCells = false;

		[Desc("Effect sequence sprite image")]
		public readonly string Effect = "explosion";

		[Desc("Effect sequence to display")]
		[SequenceReference("Effect")] public readonly string EffectSequence = "piffs";

		[PaletteReference] public readonly string EffectPalette = "effect";

		[Desc("Which weapon to fire"), WeaponReference]
		public readonly string Weapon = "Vulcan2";

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Apply the weapon impact this many ticks into the effect")]
		public readonly int WeaponDelay = 0;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		public override object Create(ActorInitializer init) { return new DropPodsPower(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			WeaponInfo weapon;
			var weaponToLower = (Weapon ?? string.Empty).ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weapon;

			base.RulesetLoaded(rules, ai);
		}
	}

	public class DropPodsPower : SupportPower
	{
		readonly DropPodsPowerInfo info;
		public DropPodsPower(Actor self, DropPodsPowerInfo info) : base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			SendDropPods(self, order, info.PodFacing);
		}

		public Actor[] SendDropPods(Actor self, Order order, int podFacing)
		{
			var units = new List<Actor>();
			var info = Info as DropPodsPowerInfo;

			var utLower = info.UnitType.ToLowerInvariant();
			ActorInfo unitType;
			if (!self.World.Map.Rules.Actors.TryGetValue(utLower, out unitType))
				throw new YamlException("Actors ruleset does not include the entry '{0}'".F(utLower));

			var altitude = unitType.TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var approachRotation = WRot.FromFacing(podFacing);
			var delta = new WVec(0, -altitude, 0).Rotate(approachRotation);

			foreach (var p in info.DropItems)
			{
				var unit = self.World.CreateActor(false, p.ToLowerInvariant(),
					new TypeDictionary { new OwnerInit(self.Owner) });

				units.Add(unit);
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					info.DropPodsAvailableNotification, self.Owner.Faction.InternalName);

				var target = order.Target.CenterPosition;
				var targetCell = self.World.Map.CellContaining(target);
				var PodLocations = self.World.Map.FindTilesInCircle(targetCell, info.PodScatter).Shuffle(self.World.SharedRandom);

				using (var pe = PodLocations.GetEnumerator())
					foreach (var u in units)
					{
						CPos podDropCellPos = pe.Current;
						w.Add(new IonCannon(self.Owner, info.WeaponInfo, w, self.World.Map.CenterOfCell(podDropCellPos), Target.FromCell(w, podDropCellPos),
						info.Effect, info.EffectSequence, info.EffectPalette, info.WeaponDelay));

						var a = w.CreateActor(info.UnitType, new TypeDictionary
						{
							new CenterPositionInit(self.World.Map.CenterOfCell(podDropCellPos) - delta + new WVec(0, 0, altitude)),
							new OwnerInit(self.Owner),
							new FacingInit(podFacing)
						});

						var cargo = a.Trait<Cargo>();
						cargo.Load(a, u);

						a.QueueActivity(new Land(a, Target.FromCell(a.World, podDropCellPos)));
						a.QueueActivity(new UnloadCargo(a, true));
						a.QueueActivity(new CallFunc(() => a.Kill(a)));
						pe.MoveNext();
					}
			});

			return units.ToArray();
		}
	}
}
