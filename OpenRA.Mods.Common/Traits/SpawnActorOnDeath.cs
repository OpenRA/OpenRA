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

using System.Linq;
using OpenRA.Mods.Common.Warheads;
using System;
using OpenRA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
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

		[Desc("Time (in frames) until spawn (0 means instant)")]
		public readonly int SpawnDelay = 0;

		public object Create(ActorInitializer init) { return new SpawnActorOnDeath(init, this); }
	}

	public class SpawnActorOnDeath : INotifyKilled
	{
		readonly SpawnActorOnDeathInfo info;
		readonly string faction;
		readonly bool enabled;
		string huskActor;

		public SpawnActorOnDeath(ActorInitializer init, SpawnActorOnDeathInfo info)
		{
			this.info = info;
			enabled = !info.RequiresLobbyCreeps || init.Self.World.WorldActor.Trait<MapCreeps>().Enabled;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!enabled)
				return;

			if (!self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > info.Probability)
				return;

			if (info.DeathType != null && !e.Damage.DamageTypes.Contains(info.DeathType))
				return;

			// Actor has been disposed by something else before its death (for example `Enter`).
			if (self.Disposed)
				return;

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

			foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
				modifier.ModifyDeathActorInit(self, td);

			huskActor = self.TraitsImplementing<IHuskModifier>()
				.Select(ihm => ihm.HuskActor(self))
				.FirstOrDefault(a => a != null);


			self.World.AddFrameEndTask(w =>
			{
				Action act = () => w.CreateActor(huskActor ?? info.Actor, td);
				if (info.SpawnDelay > 0)
					w.Add(new DelayedAction(info.SpawnDelay, act));
				else
					act();
			});
		}
	}
}
