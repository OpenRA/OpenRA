#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be carried by actors with the `Carryall` trait.")]
	public class CarryableInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference]
		[Desc("The upgrades to grant to self while waiting or being carried.")]
		public readonly string[] CarryableUpgrades = { };

		[Desc("Carryall attachment point relative to body.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public virtual object Create(ActorInitializer init) { return new Carryable(init.Self, this); }
	}

	public class Carryable
	{
		readonly CarryableInfo info;
		readonly UpgradeManager upgradeManager;

		public Actor Carrier { get; private set; }
		public bool Reserved { get { return state != State.Free; } }
		public CPos? Destination { get; protected set; }
		public bool WantsTransport { get { return Destination != null; } }

		protected enum State { Free, Reserved, Locked }
		protected State state = State.Free;
		protected bool attached;

		public Carryable(Actor self, CarryableInfo info)
		{
			this.info = info;
			upgradeManager = self.Trait<UpgradeManager>();
		}

		public virtual void Attached(Actor self)
		{
			if (attached)
				return;

			attached = true;
			foreach (var u in info.CarryableUpgrades)
				upgradeManager.GrantUpgrade(self, u, this);
		}

		// This gets called by carrier after we touched down
		public virtual void Detached(Actor self)
		{
			if (!attached)
				return;

			attached = false;
			foreach (var u in info.CarryableUpgrades)
				upgradeManager.RevokeUpgrade(self, u, this);
		}

		public virtual bool Reserve(Actor self, Actor carrier)
		{
			if (Reserved)
				return false;

			state = State.Reserved;
			Carrier = carrier;
			return true;
		}

		public virtual void UnReserve(Actor self)
		{
			state = State.Free;
			Carrier = null;
		}

		// Prepare for transport pickup
		public virtual bool LockForPickup(Actor self, Actor carrier)
		{
			if (state == State.Locked)
				return false;

			state = State.Locked;
			Carrier = carrier;
			self.QueueActivity(false, new WaitFor(() => state != State.Locked, false));
			return true;
		}
	}
}
