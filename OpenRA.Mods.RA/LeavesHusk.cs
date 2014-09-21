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
using OpenRA.Mods.RA.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class LeavesHuskInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string HuskActor = null;

		public object Create(ActorInitializer init) { return new LeavesHusk(init, this); }
	}

	public class LeavesHusk : INotifyKilled
	{
		readonly LeavesHuskInfo info;
		readonly string race;

		public LeavesHusk(ActorInitializer init, LeavesHuskInfo info)
		{
			this.info = info;

			race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : init.self.Owner.Country.Race;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary
				{
					new ParentActorInit(self),
					new LocationInit(self.Location),
					new CenterPositionInit(self.CenterPosition),
					new OwnerInit(self.Owner),
					new RaceInit(race),
					new SkipMakeAnimsInit()
				};

				// Allows the husk to drag to its final position
				var mobile = self.TraitOrDefault<Mobile>();
				if (mobile != null)
				{
					if (!mobile.CanEnterCell(self.Location, self, false)) return;
					td.Add(new HuskSpeedInit(mobile.MovementSpeedForCell(self, self.Location)));
				}

				var facing = self.TraitOrDefault<IFacing>();
				if (facing != null)
					td.Add(new FacingInit(facing.Facing));

				// TODO: This will only take the first turret if there are multiple
				// This isn't a problem with the current units, but may be a problem for mods
				var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault();
				if (turreted != null)
					td.Add(new TurretFacingInit(turreted.turretFacing));

				var chronoshiftable = self.TraitOrDefault<Chronoshiftable>();
				if (chronoshiftable != null && chronoshiftable.ReturnTicks > 0)
				{
					td.Add(new ChronoshiftOriginInit(chronoshiftable.Origin));
					td.Add(new ChronoshiftReturnInit(chronoshiftable.ReturnTicks));
				}

				var huskActor = self.TraitsImplementing<IHuskModifier>()
					.Select(ihm => ihm.HuskActor(self))
					.FirstOrDefault(a => a != null);

				w.CreateActor(huskActor ?? info.HuskActor, td);
			});
		}
	}
}
