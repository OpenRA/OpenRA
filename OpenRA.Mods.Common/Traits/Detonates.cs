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
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DetonatesInfo : ITraitInfo
	{
		[Desc("Measured in ticks.")]
		public readonly int ChargeDelay = 0;
		[SequenceReference]
		public readonly string ChargeSequence = null;
		public readonly string ChargeSound = null;

		[Desc("Measured in ticks.")]
		public readonly int DetonationDelay = 0;
		[WeaponReference]
		public readonly string DetonationWeapon = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly bool KillsSelf = true;

		public object Create(ActorInitializer init) { return new Detonates(init.Self, this); }
	}

	public class Detonates : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly DetonatesInfo info;
		public bool Deployed;

		public Detonates(Actor self, DetonatesInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new[] { "DetonateAttack" }, "DetonateAttack", 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
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

				if (info.KillsSelf)
					self.Kill(self);
			});
		}

		void FinishedCharge()
		{
			Sound.Play(info.ChargeSound, self.CenterPosition);

			foreach (var notify in self.TraitsImplementing<INotifyCharging>())
				notify.FinishedCharging(self);
		}

		void StartDetonationSequence()
		{
			if (Deployed)
				return;

			Deployed = true;

			foreach (var notify in self.TraitsImplementing<INotifyCharging>())
				notify.Charging(self, Target.FromActor(self));

			self.QueueActivity(new Wait(info.ChargeDelay, false));
			self.QueueActivity(new CallFunc(FinishedCharge));
			self.QueueActivity(new Wait(info.DetonationDelay, false));
			self.QueueActivity(new CallFunc(Detonate));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (Deployed)
				return;

			if (order.OrderString == "DetonateAttack")
			{
				var target = self.ResolveFrozenActorOrder(order, Color.Red);
				if (target.Type != TargetType.Actor)
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.QueueActivity(new CallFunc(() => self.SetTargetLine(target, Color.Red)));
				self.QueueActivity(new MoveAdjacentTo(self, target));
				self.QueueActivity(new CallFunc(StartDetonationSequence));
			}
			else if (order.OrderString == "Detonate")
			{
				self.CancelActivity();
				self.QueueActivity(new CallFunc(StartDetonationSequence));
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
			return info.Voice;
		}
	}
}