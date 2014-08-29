#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class SpawnViceroidInfo : ITraitInfo
	{
		[ActorReference] public readonly string ViceroidActor = "vice";
		public readonly int Probability = 10;
		public readonly string Owner = "Creeps";
		public readonly string DeathType = "6";

		public object Create(ActorInitializer init) { return new SpawnViceroid(this); }
	}

	class SpawnViceroid : INotifyKilled
	{
		readonly SpawnViceroidInfo spawnViceroidInfo;

		public SpawnViceroid(SpawnViceroidInfo info) { spawnViceroidInfo = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			if (e.Warhead == null || e.Warhead.DeathType != spawnViceroidInfo.DeathType) return;
			if (self.World.SharedRandom.Next(100) > spawnViceroidInfo.Probability) return;

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.World.Players.First(p => p.InternalName == spawnViceroidInfo.Owner))
				};

				var facing = self.TraitOrDefault<IFacing>();
				if (facing != null)
					td.Add(new FacingInit(facing.Facing));

				w.CreateActor(spawnViceroidInfo.ViceroidActor, td);
			});
		}
	}
}
