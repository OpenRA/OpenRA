#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ProvidesCustomPrerequisiteInfo : ITraitInfo
	{
		[Desc("The prerequisite type that this provides")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites")]
		public readonly string[] RequiresPrerequisites = { };

		[Desc("Only grant this prerequisite for certain factions")]
		public readonly string[] Race = { };

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;
		public object Create(ActorInitializer init) { return new ProvidesCustomPrerequisite(init, this); }
	}

	public class ProvidesCustomPrerequisite : ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly ProvidesCustomPrerequisiteInfo info;

		bool enabled = true;

		public ProvidesCustomPrerequisite(ActorInitializer init, ProvidesCustomPrerequisiteInfo info)
		{
			this.info = info;

			var race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : init.self.Owner.Country.Race; 

			Update(init.self.Owner, race);
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!enabled)
					yield break;

				yield return info.Prerequisite;
			}
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (info.ResetOnOwnerChange)
				Update(newOwner, newOwner.Country.Race);
		}

		void Update(Player owner, string race)
		{
			enabled = true;

			if (info.Race.Any())
				enabled = info.Race.Contains(race);

			if (info.RequiresPrerequisites.Any() && enabled)
				enabled = owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.RequiresPrerequisites);
		}
	}

	// Allows maps / transformations to specify the race variant of an actor.
	public class RaceInit : IActorInit<string>
	{
		[FieldFromYamlKey] public readonly string Race;

		public RaceInit() { }
		public RaceInit(string race) { Race = race; }
		public string Value(World world) { return Race; }
	}
}
