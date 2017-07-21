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

using System.Linq;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum OwnerType { Victim, Killer, InternalName }

	[Desc("Spawn another actor immediately upon death.")]
	public class SpawnActorOnDeathInfo : ConditionalTraitInfo
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

		[Desc("Offset of the spawned actor relative to the dying actor's position.",
			"Warning: Spawning an actor outside the parent actor's footprint/influence might",
			"lead to unexpected behaviour.")]
		public readonly CVec Offset = CVec.Zero;

		public override object Create(ActorInitializer init) { return new SpawnActorOnDeath(init, this); }
	}

	public class SpawnActorOnDeath : ConditionalTrait<SpawnActorOnDeathInfo>, INotifyKilled
	{
		readonly string faction;
		readonly bool enabled;

		public SpawnActorOnDeath(ActorInitializer init, SpawnActorOnDeathInfo info)
			: base(info)
		{
			enabled = !info.RequiresLobbyCreeps || init.Self.World.WorldActor.Trait<MapCreeps>().Enabled;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!enabled || IsTraitDisabled)
				return;

			if (!self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > Info.Probability)
				return;

			if (Info.DeathType != null && !e.Damage.DamageTypes.Contains(Info.DeathType))
				return;

			self.World.AddFrameEndTask(w =>
			{
				// Actor has been disposed by something else before its death (for example `Enter`).
				if (self.Disposed)
					return;

				var td = new TypeDictionary
				{
					new ParentActorInit(self),
					new LocationInit(self.Location + Info.Offset),
					new CenterPositionInit(self.CenterPosition),
					new FactionInit(faction)
				};

				if (Info.OwnerType == OwnerType.Victim)
					td.Add(new OwnerInit(self.Owner));
				else if (Info.OwnerType == OwnerType.Killer)
					td.Add(new OwnerInit(e.Attacker.Owner));
				else
					td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.InternalOwner)));

				if (Info.SkipMakeAnimations)
					td.Add(new SkipMakeAnimsInit());

				foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
					modifier.ModifyDeathActorInit(self, td);

				var huskActor = self.TraitsImplementing<IHuskModifier>()
					.Select(ihm => ihm.HuskActor(self))
					.FirstOrDefault(a => a != null);

				w.CreateActor(huskActor ?? Info.Actor, td);
			});
		}
	}
}
