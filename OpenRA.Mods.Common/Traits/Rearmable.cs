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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RearmableInfo : DockableInfo
	{
		[Desc("Name(s) of AmmoPool(s) that use this trait to rearm.")]
		public readonly HashSet<string> AmmoPools = new HashSet<string> { "primary" };

		[Desc("Rearm docking type")]
		public readonly BitSet<DockType> DockType = new BitSet<DockType>("rearm");

		public override object Create(ActorInitializer init) { return new Rearmable(init.Self, this); }
	}

	public class Rearmable : Dockable<RearmableInfo>, INotifyCreated, INotifyDockable
	{
		public Rearmable(Actor self, RearmableInfo info)
			: base(self, info) { }

		public AmmoPool[] RearmableAmmoPools { get; private set; }

		protected override BitSet<DockType> DockType() { return Info.DockType; }

		public override bool CanDock()
		{
			return !RearmableAmmoPools.All(p => p.HasFullAmmo);
		}

		void INotifyCreated.Created(Actor self)
		{
			RearmableAmmoPools = self.TraitsImplementing<AmmoPool>().Where(p => Info.AmmoPools.Contains(p.Info.Name)).ToArray();
		}

		void INotifyDockable.Docked(DockManager dockable, Dock dock)
		{
			// Reset the ReloadDelay to avoid any issues with early cancellation
			// from previous reload attempts (explicit order, host building died, etc).
			foreach (var pool in RearmableAmmoPools)
				pool.RemainingTicks = pool.Info.ReloadDelay;
		}

		void INotifyDockable.DockTick(DockManager dockable, Dock dock) { }
		void INotifyDockable.Undocked(DockManager dockable, Dock dock) { }

		public override bool TickDock(Dock dock)
		{
			var rearmComplete = true;
			foreach (var ammoPool in RearmableAmmoPools)
			{
				if (!ammoPool.HasFullAmmo)
				{
					if (--ammoPool.RemainingTicks <= 0)
					{
						ammoPool.RemainingTicks = ammoPool.Info.ReloadDelay;
						if (!string.IsNullOrEmpty(ammoPool.Info.RearmSound))
							Game.Sound.PlayToPlayer(SoundType.World, Self.Owner, ammoPool.Info.RearmSound, Self.CenterPosition);

						ammoPool.GiveAmmo(Self, ammoPool.Info.ReloadCount);
					}

					rearmComplete = false;
				}
			}

			if (rearmComplete)
				return true;

			return false;
		}
	}
}
