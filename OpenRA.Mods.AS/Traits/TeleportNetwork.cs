#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	// TO-DO: Create a proper check for Types of TeleportNetwork and TeleportNetworkManager or lint rule.
	[Desc("This actor can teleport actors like Nydus canels in SC1. Assuming static object.")]
	public class TeleportNetworkInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Type of TeleportNetwork that pairs up, in order for it to work.")]
		public string Type;

		[Desc("Stances requirement that targeted TeleportNetwork has to meet in order to teleport units.")]
		public Stance ValidStances = Stance.Ally;

		public object Create(ActorInitializer init) { return new TeleportNetwork(init, this); }
	}

	// The teleport network canal does nothing. The actor teleports itself, upon entering.
	public class TeleportNetwork : INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged
	{
		public TeleportNetworkInfo Info;
		TeleportNetworkManager tnm;

		public TeleportNetwork(ActorInitializer init, TeleportNetworkInfo info)
		{
			this.Info = info;
		}

		void IncreaseTeleportNetworkCount(Actor self, Player owner)
		{
			// Assign itself as primary, when first one.
			if (tnm.Count == 0)
			{
				var pri = self.TraitOrDefault<TeleportNetworkPrimaryExit>();

				if (pri == null)
					return;

				pri.SetPrimary(self);
			}

			tnm.Count++;
		}

		void DecreaseTeleportNetworkCount(Actor self, Player owner)
		{
			tnm.Count--;

			if (self.IsPrimaryTeleportNetworkExit())
			{
				var actors = self.World.ActorsWithTrait<TeleportNetworkPrimaryExit>()
				.Where(a => a.Actor.Owner == self.Owner && a.Actor != self);

				if (!actors.Any())
					tnm.PrimaryActor = null;
				else
				{
					var pri = actors.First().Actor;
					pri.Trait<TeleportNetworkPrimaryExit>().SetPrimary(pri);
				}
			}
		}

		public void AddedToWorld(Actor self)
		{
			tnm = self.Owner.PlayerActor.TraitsImplementing<TeleportNetworkManager>().First(x => x.Type == this.Info.Type);
			IncreaseTeleportNetworkCount(self, self.Owner);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			DecreaseTeleportNetworkCount(self, oldOwner);
			IncreaseTeleportNetworkCount(self, newOwner);
		}

		public void RemovedFromWorld(Actor self) { DecreaseTeleportNetworkCount(self, self.Owner); }
	}
}
