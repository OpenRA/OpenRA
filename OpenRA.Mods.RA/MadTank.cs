#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.Common.Orders;

namespace OpenRA.Mods.RA
{
	class MadTankInfo : ITraitInfo, Requires<ExplodesInfo>, Requires<RenderUnitInfo>
	{
		public readonly string ThumpSequence = "piston";
		public readonly int ThumpInterval = 8;
		[WeaponReference]
		public readonly string ThumpDamageWeapon = "MADTankThump";
		public readonly int ThumpShakeIntensity = 3;
		public readonly float2 ThumpShakeMultiplier = new float2(1, 0);
		public readonly int ThumpShakeTime = 10;

		public readonly int ChargeDelay = 96;
		public readonly string ChargeSound = "madchrg2.aud";

		public readonly int DetonationDelay = 42;
		public readonly string DetonationSound = "madexplo.aud";
		[WeaponReference]
		public readonly string DetonationWeapon = "MADTankDetonate";

		[ActorReference]
		public readonly string DriverActor = "e1";

		[Desc("Acceptable stances of target's owner.")]
		public readonly Stance TargetPlayers = Stance.Enemy | Stance.Neutral;

		public object Create(ActorInitializer init) { return new MadTank(init.self, this); }
	}

	class MadTank : IIssueOrder, IResolveOrder, IOrderVoice, ITick, IPreventsTeleport
	{
		readonly Actor self;
		readonly MadTankInfo info;
		readonly RenderUnit renderUnit;
		readonly ScreenShaker screenShaker;
		bool deployed;
		int tick;

		public MadTank(Actor self, MadTankInfo info)
		{
			this.self = self;
			this.info = info;
			renderUnit = self.Trait<RenderUnit>();
			screenShaker = self.World.WorldActor.Trait<ScreenShaker>();
		}

		public void Tick(Actor self)
		{
			if (!deployed)
				return;

			if (++tick >= info.ThumpInterval)
			{
				if (info.ThumpDamageWeapon != null)
				{
					var weapon = self.World.Map.Rules.Weapons[info.ThumpDamageWeapon.ToLowerInvariant()];
					// Use .FromPos since this weapon needs to affect more than just the MadTank actor
					weapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}
				screenShaker.AddEffect(info.ThumpShakeTime, self.CenterPosition, info.ThumpShakeIntensity, info.ThumpShakeMultiplier);
				tick = 0;
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new[] { "DetonateAttack" }, "DetonateAttack", 5, "attack", info.TargetPlayers) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DetonateAttack" && order.OrderID != "Detonate")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return "Attack";
		}

		void Detonate()
		{
			self.World.AddFrameEndTask(w =>
			{
				if (info.DetonationWeapon != null)
				{
					var weapon = self.World.Map.Rules.Weapons[info.DetonationWeapon.ToLowerInvariant()];
					// Use .FromPos since this actor is killed. Cannot use Target.FromActor
					weapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}
				self.Kill(self);
			});
		}

		void EjectDriver()
		{
			var driver = self.World.CreateActor(info.DriverActor.ToLowerInvariant(), new TypeDictionary
			{
				new LocationInit(self.Location),
				new OwnerInit(self.Owner)
			});
			var driverMobile = driver.TraitOrDefault<Mobile>();
			if (driverMobile != null)
				driverMobile.Nudge(driver, driver, true);
		}

		public bool PreventsTeleport(Actor self) { return deployed; }

		void StartDetonationSequence()
		{
			if (deployed)
				return;

			self.World.AddFrameEndTask(w => EjectDriver());
			if (info.ThumpSequence != null)
				renderUnit.PlayCustomAnimRepeating(self, info.ThumpSequence);
			deployed = true;
			self.QueueActivity(new Wait(info.ChargeDelay, false));
			self.QueueActivity(new CallFunc(() => Sound.Play(info.ChargeSound, self.CenterPosition)));
			self.QueueActivity(new Wait(info.DetonationDelay, false));
			self.QueueActivity(new CallFunc(() => Sound.Play(info.DetonationSound, self.CenterPosition)));
			self.QueueActivity(new CallFunc(Detonate));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DetonateAttack")
			{
				var target = self.ResolveFrozenActorOrder(order, Color.Red);
				if (target.Type != TargetType.Actor)
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.SetTargetLine(target, Color.Red);
				self.QueueActivity(new MoveAdjacentTo(self, target));
				self.QueueActivity(new CallFunc(StartDetonationSequence));
			}

			else if (order.OrderString == "Detonate")
			{
				self.CancelActivity();
				self.QueueActivity(new CallFunc(StartDetonationSequence));
			}
		}
	}
}
