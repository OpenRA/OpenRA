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

		[Desc("Only grant this prerequisite for certain factions")]
		public readonly string[] Race = { };

		[Desc("Should the prerequisite remain enabled if the owner changes?")]
		public readonly bool Sticky = true;
		public object Create(ActorInitializer init) { return new ProvidesCustomPrerequisite(init, this); }
	}

	public class ProvidesCustomPrerequisite : ITechTreePrerequisite, INotifyOwnerChanged
	{
		ProvidesCustomPrerequisiteInfo info;
		bool enabled = true;

		public ProvidesCustomPrerequisite(ActorInitializer init, ProvidesCustomPrerequisiteInfo info)
		{
			this.info = info;

			if (info.Race.Any())
			{
				var race = init.self.Owner.Country.Race;
				if (init.Contains<RaceInit>())
					race = init.Get<RaceInit, string>();

				enabled = info.Race.Contains(race);
			}
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
			if (!info.Sticky && info.Race.Any())
				enabled = info.Race.Contains(self.Owner.Country.Race);
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
