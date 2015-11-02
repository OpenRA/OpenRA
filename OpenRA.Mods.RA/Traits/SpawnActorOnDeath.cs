#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public enum OwnerType { Victim, Killer, InternalName }

	[Desc("Spawn another actor immediately upon death.")]
	public class SpawnActorOnDeathInfo : ITraitInfo
	{
		[ActorReference, FieldLoader.Require]
		[Desc("Actor to spawn on death.")]
		public readonly string Actor = null;

		[Desc("Probability the actor spawns.")]
		public readonly int Probability = 100;

		[Desc("Owner of the spawned actor. Allowed keywords:" +
			"'Victim', 'Killer' and 'InternalName'.")]
		public readonly OwnerType OwnerType = OwnerType.Victim;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = null;

		[Desc("DeathType that triggers the actor spawn. " +
			"Leave empty to spawn an actor ignoring the DeathTypes.")]
		public readonly string DeathType = null;

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("Should an actor only be spawned when the 'Creeps' setting is true?")]
		public readonly bool RequiresLobbyCreeps = false;

		public object Create(ActorInitializer init) { return new SpawnActorOnDeath(init, this); }
	}

	public class SpawnActorOnDeath : INotifyKilled
	{
		readonly SpawnActorOnDeathInfo info;
		readonly string faction;

		public SpawnActorOnDeath(ActorInitializer init, SpawnActorOnDeathInfo info)
		{
			this.info = info;

			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (info.RequiresLobbyCreeps && !self.World.LobbyInfo.GlobalSettings.Creeps)
				return;

			if (!self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > info.Probability)
				return;

			var warhead = e.Warhead as DamageWarhead;
			if (info.DeathType != null && (warhead == null || !warhead.DamageTypes.Contains(info.DeathType)))
				return;

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary
				{
					new ParentActorInit(self),
					new LocationInit(self.Location),
					new CenterPositionInit(self.CenterPosition),
					new FactionInit(faction)
				};

				if (info.OwnerType == OwnerType.Victim)
					td.Add(new OwnerInit(self.Owner));
				else if (info.OwnerType == OwnerType.Killer)
					td.Add(new OwnerInit(e.Attacker.Owner));
				else
					td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == info.InternalOwner)));

				if (info.SkipMakeAnimations)
					td.Add(new SkipMakeAnimsInit());

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

				var turreted = self.TraitsImplementing<Turreted>();
				if (turreted.Any())
				{
					var turretFacings = new Dictionary<string, int>();
					foreach (var t in turreted)
						turretFacings.Add(t.Name, t.TurretFacing);
					td.Add(new TurretFacingsInit(turretFacings));
				}

				// TODO: untie this and move to Mods.Common
				var chronoshiftable = self.TraitOrDefault<Chronoshiftable>();
				if (chronoshiftable != null && chronoshiftable.ReturnTicks > 0)
				{
					td.Add(new ChronoshiftOriginInit(chronoshiftable.Origin));
					td.Add(new ChronoshiftReturnInit(chronoshiftable.ReturnTicks));
				}

				var huskActor = self.TraitsImplementing<IHuskModifier>()
					.Select(ihm => ihm.HuskActor(self))
					.FirstOrDefault(a => a != null);

				w.CreateActor(huskActor ?? info.Actor, td);
			});
		}
	}
}
