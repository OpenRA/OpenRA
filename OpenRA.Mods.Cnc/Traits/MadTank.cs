#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class MadTankInfo : TraitInfo, IRulesetLoaded, Requires<ExplodesInfo>, Requires<WithFacingSpriteBodyInfo>
	{
		[SequenceReference]
		public readonly string ThumpSequence = "piston";

		public readonly int ThumpInterval = 8;

		[WeaponReference]
		public readonly string ThumpDamageWeapon = "MADTankThump";

		[Desc("Measured in ticks.")]
		public readonly int ChargeDelay = 96;

		public readonly string ChargeSound = "madchrg2.aud";

		[Desc("Measured in ticks.")]
		public readonly int DetonationDelay = 42;

		public readonly string DetonationSound = "madexplo.aud";

		[WeaponReference]
		public readonly string DetonationWeapon = "MADTankDetonate";

		[ActorReference]
		public readonly string DriverActor = "e1";

		[VoiceReference]
		public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while deployed.")]
		public readonly string DeployedCondition = null;

		public WeaponInfo ThumpDamageWeaponInfo { get; private set; }

		public WeaponInfo DetonationWeaponInfo { get; private set; }

		[Desc("Types of damage that this trait causes to self while self-destructing. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new MadTank(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var thumpDamageWeaponToLower = (ThumpDamageWeapon ?? string.Empty).ToLowerInvariant();
			var detonationWeaponToLower = (DetonationWeapon ?? string.Empty).ToLowerInvariant();

			if (!rules.Weapons.TryGetValue(thumpDamageWeaponToLower, out var thumpDamageWeapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(thumpDamageWeaponToLower));

			if (!rules.Weapons.TryGetValue(detonationWeaponToLower, out var detonationWeapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(detonationWeaponToLower));

			ThumpDamageWeaponInfo = thumpDamageWeapon;
			DetonationWeaponInfo = detonationWeapon;
		}
	}

	class MadTank : IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		readonly MadTankInfo info;

		public MadTank(Actor self, MadTankInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new BitSet<TargetableType>("DetonateAttack"), "DetonateAttack", 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "DetonateAttack" && order.OrderID != "Detonate")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("Detonate", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return true; }

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DetonateAttack" && order.OrderString != "Detonate")
				return null;

			return info.Voice;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DetonateAttack")
			{
				self.QueueActivity(order.Queued, new DetonationSequence(self, this, order.Target));
				self.ShowTargetLines();
			}
			else if (order.OrderString == "Detonate")
				self.QueueActivity(order.Queued, new DetonationSequence(self, this));
		}

		class DetonationSequence : Activity
		{
			readonly Actor self;
			readonly MadTank mad;
			readonly IMove move;
			readonly WithFacingSpriteBody wfsb;
			readonly ScreenShaker screenShaker;
			readonly bool assignTargetOnFirstRun;

			int ticks;
			bool initiated;
			Target target;

			public DetonationSequence(Actor self, MadTank mad)
				: this(self, mad, Target.Invalid)
			{
				assignTargetOnFirstRun = true;
			}

			public DetonationSequence(Actor self, MadTank mad, in Target target)
			{
				this.self = self;
				this.mad = mad;
				this.target = target;

				move = self.Trait<IMove>();
				wfsb = self.Trait<WithFacingSpriteBody>();
				screenShaker = self.World.WorldActor.Trait<ScreenShaker>();
			}

			protected override void OnFirstRun(Actor self)
			{
				if (assignTargetOnFirstRun)
					target = Target.FromCell(self.World, self.Location);
			}

			public override bool Tick(Actor self)
			{
				if (IsCanceling)
					return true;

				if (target.Type != TargetType.Invalid && !move.CanEnterTargetNow(self, target))
				{
					QueueChild(new MoveAdjacentTo(self, target, targetLineColor: Color.Red));
					return false;
				}

				if (!initiated)
				{
					// If the target has died while we were moving, we should abort detonation.
					if (target.Type == TargetType.Invalid)
						return true;

					self.GrantCondition(mad.info.DeployedCondition);

					self.World.AddFrameEndTask(w => EjectDriver());
					if (mad.info.ThumpSequence != null)
						wfsb.PlayCustomAnimationRepeating(self, mad.info.ThumpSequence);

					IsInterruptible = false;
					initiated = true;
				}

				if (++ticks % mad.info.ThumpInterval == 0)
				{
					if (mad.info.ThumpDamageWeapon != null)
					{
						// Use .FromPos since this weapon needs to affect more than just the MadTank actor
						mad.info.ThumpDamageWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
					}
				}

				if (ticks == mad.info.ChargeDelay)
					Game.Sound.Play(SoundType.World, mad.info.ChargeSound, self.CenterPosition);

				return ticks == mad.info.ChargeDelay + mad.info.DetonationDelay;
			}

			protected override void OnLastRun(Actor self)
			{
				if (!initiated)
					return;

				Game.Sound.Play(SoundType.World, mad.info.DetonationSound, self.CenterPosition);

				self.World.AddFrameEndTask(w =>
				{
					if (mad.info.DetonationWeapon != null)
					{
						// Use .FromPos since this actor is killed. Cannot use Target.FromActor
						mad.info.DetonationWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
					}

					self.Kill(self, mad.info.DamageTypes);
				});
			}

			public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
			{
				yield return new TargetLineNode(target, Color.Crimson);
			}

			void EjectDriver()
			{
				var driver = self.World.CreateActor(mad.info.DriverActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.Owner)
				});
				driver.TraitOrDefault<Mobile>()?.Nudge(driver);
			}
		}
	}
}
