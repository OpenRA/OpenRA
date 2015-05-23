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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ProvidesCustomPrerequisiteInfo : ITraitInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = { };

		[Desc("Only grant this prerequisite for certain factions.")]
		public readonly string[] Race = { };

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;
		public object Create(ActorInitializer init) { return new ProvidesCustomPrerequisite(init, this); }
	}

	public class ProvidesCustomPrerequisite : ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly ProvidesCustomPrerequisiteInfo info;
		readonly string prerequisite;

		bool enabled = true;

		public ProvidesCustomPrerequisite(ActorInitializer init, ProvidesCustomPrerequisiteInfo info)
		{
			this.info = info;
			prerequisite = info.Prerequisite;

			if (string.IsNullOrEmpty(prerequisite))
				prerequisite = init.Self.Info.Name;

			var race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : init.Self.Owner.Country.Race;

			Update(init.Self.Owner, race);
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!enabled)
					yield break;

				yield return prerequisite;
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
}
