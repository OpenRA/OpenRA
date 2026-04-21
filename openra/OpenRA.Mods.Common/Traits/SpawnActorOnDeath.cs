#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum OwnerType { Victim, Killer, InternalName }

	[Desc("Spawn another actor immediately upon death.")]
	public class SpawnActorOnDeathInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn on death.")]
		public readonly string Actor = null;

		[Desc("Probability the actor spawns.")]
		public readonly int Probability = 100;

		[Desc("Owner of the spawned actor. Allowed keywords:" +
			"'Victim', 'Killer' and 'InternalName'. " +
			"Falls back to 'InternalName' if 'Victim' is used " +
			"and the victim is defeated (see 'SpawnAfterDefeat').")]
		public readonly OwnerType OwnerType = OwnerType.Victim;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		[Desc("Changes the effective (displayed) owner of the spawned actor to the old owner (victim).")]
		public readonly bool EffectiveOwnerFromOwner = false;

		[Desc("DeathType that triggers the actor spawn. " +
			"Leave empty to spawn an actor ignoring the DeathTypes.")]
		public readonly string DeathType = null;

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("Should an actor only be spawned when the 'Creeps' setting is true?")]
		public readonly bool RequiresLobbyCreeps = false;

		[Desc("Offset of the spawned actor relative to the dying actor's position.",
			"Warning: Spawning an actor outside the parent actor's footprint/influence might",
			"lead to unexpected behaviour.")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Should an actor spawn after the player has been defeated (e.g. after surrendering)?")]
		public readonly bool SpawnAfterDefeat = true;

		public override object Create(ActorInitializer init) { return new SpawnActorOnDeath(init, this); }
	}

	public class SpawnActorOnDeath : ConditionalTrait<SpawnActorOnDeathInfo>, INotifyKilled, INotifyRemovedFromWorld
	{
		readonly string faction;
		readonly bool enabled;

		Player attackingPlayer;

		public SpawnActorOnDeath(ActorInitializer init, SpawnActorOnDeathInfo info)
			: base(info)
		{
			enabled = !info.RequiresLobbyCreeps || init.Self.World.WorldActor.Trait<MapCreeps>().Enabled;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!enabled || IsTraitDisabled || !self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > Info.Probability)
				return;

			if (Info.DeathType != null && !e.Damage.DamageTypes.Contains(Info.DeathType))
				return;

			attackingPlayer = e.Attacker.Owner;
		}

		// Don't add the new actor to the world before all RemovedFromWorld callbacks have run
		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (attackingPlayer == null)
				return;

			var defeated = self.Owner.WinState == WinState.Lost;
			if (defeated && !Info.SpawnAfterDefeat)
				return;

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + Info.Offset),
				new CenterPositionInit(self.CenterPosition),
				new FactionInit(faction)
			};

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised)
				td.Add(new EffectiveOwnerInit(self.EffectiveOwner.Owner));
			else if (Info.EffectiveOwnerFromOwner)
				td.Add(new EffectiveOwnerInit(self.Owner));

			if (Info.OwnerType == OwnerType.Victim)
			{
				// Fall back to InternalOwner if the Victim was defeated,
				// but only if InternalOwner is defined
				if (!defeated || string.IsNullOrEmpty(Info.InternalOwner))
					td.Add(new OwnerInit(self.Owner));
				else
				{
					td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.InternalOwner)));
					if (!td.Contains<EffectiveOwnerInit>())
						td.Add(new EffectiveOwnerInit(self.Owner));
				}
			}
			else if (Info.OwnerType == OwnerType.Killer)
				td.Add(new OwnerInit(attackingPlayer));
			else
				td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.InternalOwner)));

			if (Info.SkipMakeAnimations)
				td.Add(new SkipMakeAnimsInit());

			foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
				modifier.ModifyDeathActorInit(self, td);

			var huskActor = self.TraitsImplementing<IHuskModifier>()
				.Select(ihm => ihm.HuskActor(self))
				.FirstOrDefault(a => a != null);

			self.World.AddFrameEndTask(w => w.CreateActor(huskActor ?? Info.Actor, td));
		}
	}
}
