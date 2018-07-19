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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Manages the contents of shared cargos like GLA Tunnel Networks")]
	public class SharedCargoManagerInfo : ITraitInfo
	{
		[Desc("Type of shared cargo")]
		public readonly string Type = "tunnel";

		[Desc("The maximum sum of Passenger.Weight that this actor can support.")]
		public readonly int MaxWeight = 0;

		public object Create(ActorInitializer init) { return new SharedCargoManager(init.Self, this); }
	}

	public class SharedCargoManager
	{
		public SharedCargoManagerInfo Info;
		public Stack<Actor> Cargo = new Stack<Actor>();
		public HashSet<Actor> Reserves = new HashSet<Actor>();

		public IEnumerable<Actor> Passengers { get { return Cargo; } }
		public int PassengerCount { get { return Cargo.Count; } }

		public int TotalWeight = 0;
		public int ReservedWeight = 0;

		public SharedCargoManager(Actor self, SharedCargoManagerInfo info)
		{
			Info = info;
		}

		public bool HasSpace(int weight) { return TotalWeight + ReservedWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty() { return Cargo.Count == 0; }

		public void Clear(AttackInfo e = null)
		{
			foreach (var passenger in Cargo)
				if (e != null)
					passenger.Kill(e.Attacker, e.Damage.DamageTypes);
				else
					passenger.Dispose();

			Cargo.Clear();
			TotalWeight = 0;
		}
	}
}
