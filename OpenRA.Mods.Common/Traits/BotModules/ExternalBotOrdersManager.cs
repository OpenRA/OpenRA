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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows the player to issue the orders from actors with " + nameof(IssueOrderToBot) + ". etc")]
	public class ExternalBotOrdersManagerInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ExternalBotOrdersManager(init.Self, this); }
	}

	public class ExternalBotOrdersManager : ConditionalTrait<ExternalBotOrdersManagerInfo>, IBotTick
	{
		readonly Dictionary<Actor, (Order Order, int Chance)> directEntries = [];
		readonly Dictionary<Actor, IssueOrderToBotInfo> issueOrderToBotEntries = [];
		readonly World world;
		readonly Player player;

		public bool ManagerRunning { get; private set; }

		public ExternalBotOrdersManager(Actor self, ExternalBotOrdersManagerInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			ManagerRunning = false;
		}

		// Use for proactive targeting.
		public bool IsPreferredTarget(Actor self, Actor a,
			PlayerRelationship validRelationships, BitSet<TargetableType> validTargets, BitSet<TargetableType> invalidTargets)
		{
			if (a == self || a == null || a.IsDead || !a.IsInWorld || !validRelationships.HasRelationship(self.Owner.RelationshipWith(a.Owner)))
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && validTargets.Overlaps(targetTypes) && !invalidTargets.Overlaps(targetTypes);
		}

		public bool IsNotHiddenUnit(Actor self, Actor a)
		{
			var hasModifier = false;
			var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
			foreach (var v in visModifiers)
			{
				if (v.IsVisible(a, self.Owner))
					return true;

				hasModifier = true;
			}

			return !hasModifier;
		}

		public void AddEntry(Actor issuer, Order order, int chance)
		{
			directEntries[issuer] = (order, chance);
		}

		public void AddEntryFromIssueOrderToBot(Actor issuer, IssueOrderToBotInfo info)
		{
			issueOrderToBotEntries[issuer] = info;
		}

		public Order PhraseEntryFromIssueOrderToBot(Actor issuer, IssueOrderToBotInfo info)
		{
			if (issuer.IsDead || !issuer.IsInWorld || issuer.Owner != player || world.LocalRandom.Next(100) > info.OrderChance)
				return null;

			var subject = info.IsIssuerOwner ? issuer.Owner.PlayerActor : issuer;
			Order order = null;

			if (info.TargetRangeInCell > 0)
			{
				var validActors = world.FindActorsInCircle(issuer.CenterPosition, WDist.FromCells(info.TargetRangeInCell))
					.Where(a => IsPreferredTarget(issuer, a, info.ValidRelationships, info.ValidTargets, info.InvalidTargets) && IsNotHiddenUnit(issuer, a));

				Actor validActor = null;

				switch (info.TargetDistance)
				{
					case TargetDistance.Closest:
						validActor = validActors.ClosestToIgnoringPath(issuer);
						break;
					case TargetDistance.Furthest:
						validActor = validActors.OrderByDescending(a => (a.Location - issuer.Location).LengthSquared).FirstOrDefault();
						break;
					case TargetDistance.Random:
						validActor = validActors.RandomOrDefault(world.LocalRandom);
						break;
				}

				if (validActor == null)
					return null;

				var target = Target.Invalid;
				if (info.UseTargetLocation)
					order = new Order(info.OrderName, subject, Target.FromCell(world, validActor.Location), false);
				else
					order = new Order(info.OrderName, subject, Target.FromActor(validActor), false);
			}

			return order ??= new Order(info.OrderName, subject, false);
		}

		void IBotTick.BotTick(IBot bot)
		{
			// "ManagerRunning = true" means IBotTick is running, and the game is
			// 1. not a replay
			// 2. not saved game still loading
			// 3. the game running on the host where AI is enabled
			ManagerRunning = true;

			foreach (var entry in directEntries)
			{
				if (entry.Key.IsDead || !entry.Key.IsInWorld || entry.Key.Owner != player)
					continue;

				if (world.LocalRandom.Next(100) > entry.Value.Chance)
					continue;

				bot.QueueOrder(entry.Value.Order);
			}

			directEntries.Clear();

			foreach (var entry in issueOrderToBotEntries)
			{
				var order = PhraseEntryFromIssueOrderToBot(entry.Key, entry.Value);
				if (order != null)
					bot.QueueOrder(order);
			}

			issueOrderToBotEntries.Clear();
		}
	}
}
