#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class SpawnViceroidInfo : ITraitInfo
	{
		[ActorReference] public readonly string ViceroidActor = "vice";
		public readonly int Probability = 10;
		public readonly string Owner = "Creeps";
		public readonly string DeathType = "TiberiumDeath";

		public object Create(ActorInitializer init) { return new SpawnViceroid(this); }
	}

	class SpawnViceroid : INotifyKilled
	{
		readonly SpawnViceroidInfo info;

		public SpawnViceroid(SpawnViceroidInfo info) { this.info = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			if (!self.World.LobbyInfo.GlobalSettings.Creeps) return;
			if (self.World.SharedRandom.Next(100) > info.Probability) return;

			var warhead = e.Warhead as DamageWarhead;
			if (warhead == null || !warhead.DamageTypes.Contains(info.DeathType))
				return;

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.World.Players.First(p => p.InternalName == info.Owner))
				};

				var facing = self.TraitOrDefault<IFacing>();
				if (facing != null)
					td.Add(new FacingInit(facing.Facing));

				w.CreateActor(info.ViceroidActor, td);
			});
		}
	}
}
