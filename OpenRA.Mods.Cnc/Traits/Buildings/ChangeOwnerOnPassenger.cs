#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class ChangeOwnerOnPassengerInfo : ChangeOwnerInfo, ITraitInfo, Requires<CargoInfo>
	{
		[Desc("The speech notification on enter first passenger on cargo.")]
		public readonly string EnterNotification = null;
		[Desc("The speech notification on exit last passenger on cargo")]
		public readonly string ExitNotification = null;

		public override object Create(ActorInitializer init) { return new ChangeOwnerOnPassenger(init.Self, this); }
	}

	public class ChangeOwnerOnPassenger : ChangeOwner, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly ChangeOwnerOnPassengerInfo info;
		readonly Cargo cargo;
		private readonly Player originalOwner;

    	public ChangeOwnerOnPassenger(Actor self, ChangeOwnerOnPassengerInfo info)
		{
			this.info = info;
			cargo = self.Trait<Cargo>();
		    originalOwner = self.Owner;
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			var newOwner = passenger.Owner;
			if (self.Owner == newOwner)
				return;
			NeedChangeOwner(self, passenger, newOwner);

			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", info.EnterNotification, newOwner.Faction.InternalName);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (cargo.PassengerCount > 0)
				return;

			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", info.ExitNotification, passenger.Owner.Faction.InternalName);
			NeedChangeOwner(self, passenger, originalOwner);
		}
	}
}