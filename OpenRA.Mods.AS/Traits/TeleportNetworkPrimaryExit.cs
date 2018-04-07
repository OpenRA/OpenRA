#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification */
namespace OpenRA.Mods.AS.Traits
{
	static class TeleportNetworkPrimaryExitExts
	{
		public static bool IsValidTeleportNetworkUser(this Actor networkactor, Actor useractor)
		{
			var trait = networkactor.TraitOrDefault<TeleportNetwork>();
			if (trait == null)
				return false;

			var exit = networkactor.TraitOrDefault<TeleportNetworkPrimaryExit>();
			if (exit != null && exit.IsPrimary)
				return false;

			return networkactor.Owner.Stances[useractor.Owner].HasFlag(trait.Info.ValidStances);
		}

		public static bool IsPrimaryTeleportNetworkExit(this Actor networkactor)
		{
			var exit = networkactor.TraitOrDefault<TeleportNetworkPrimaryExit>();

			if (exit == null)
				return false;

			return exit.IsPrimary;
		}
	}

	[Desc("Used with TeleportNetwork trait for primary exit designation.")]
	public class TeleportNetworkPrimaryExitInfo : ITraitInfo, Requires<TeleportNetworkInfo>
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self while this is the primary building.")]
		public readonly string PrimaryCondition = "primary";

		[Desc("The speech notification to play when selecting a primary exit.")]
		public readonly string SelectionNotification = "PrimaryBuildingSelected";

		public object Create(ActorInitializer init) { return new TeleportNetworkPrimaryExit(init.Self, this); }
	}

	public class TeleportNetworkPrimaryExit : IIssueOrder, IResolveOrder, INotifyCreated
	{
		readonly TeleportNetworkPrimaryExitInfo info;
		readonly TeleportNetworkManager manager;
		ConditionManager conditionManager;
		int primaryToken = ConditionManager.InvalidConditionToken;

		public bool IsPrimary { get; private set; }

		public TeleportNetworkPrimaryExit(Actor self, TeleportNetworkPrimaryExitInfo info)
		{
			this.info = info;
			var trait = self.Info.TraitInfoOrDefault<TeleportNetworkInfo>();
			this.manager = self.Owner.PlayerActor.TraitsImplementing<TeleportNetworkManager>().Where(x => x.Type == trait.Type).First();
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("TeleportNetworkPrimaryExit", 1); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "TeleportNetworkPrimaryExit")
				return new Order(order.OrderID, self, false);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			// You can NEVER unselect a primary teleport network building, unlike primary productions buildings in RA1.
			if (order.OrderString == "TeleportNetworkPrimaryExit")
				SetPrimary(self);
		}

		public void RevokePrimary(Actor self)
		{
			this.IsPrimary = false;

			if (primaryToken != ConditionManager.InvalidConditionToken)
				primaryToken = conditionManager.RevokeCondition(self, primaryToken);
		}

		public void SetPrimary(Actor self)
		{
			this.IsPrimary = true;

			var pri = manager.PrimaryActor;
			if (pri != null)
				pri.Trait<TeleportNetworkPrimaryExit>().RevokePrimary(pri);

			manager.PrimaryActor = self;

			primaryToken = conditionManager.GrantCondition(self, info.PrimaryCondition);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SelectionNotification, self.Owner.Faction.InternalName);
		}
	}
}
