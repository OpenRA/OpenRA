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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ProvidesPrerequisiteInfo : ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = { };

		[Desc("Only grant this prerequisite for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;
		public object Create(ActorInitializer init) { return new ProvidesPrerequisite(init, this); }
	}

	public class ProvidesPrerequisite : ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly ProvidesPrerequisiteInfo info;
		readonly string prerequisite;

		bool enabled = true;

		public ProvidesPrerequisite(ActorInitializer init, ProvidesPrerequisiteInfo info)
		{
			this.info = info;
			prerequisite = info.Prerequisite;

			if (string.IsNullOrEmpty(prerequisite))
				prerequisite = init.Self.Info.Name;

			var faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;

			Update(init.Self.Owner, faction);
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
				Update(newOwner, newOwner.Faction.InternalName);
		}

		void Update(Player owner, string faction)
		{
			enabled = true;

			if (info.Factions.Any())
				enabled = info.Factions.Contains(faction);

			if (info.RequiresPrerequisites.Any() && enabled)
				enabled = owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.RequiresPrerequisites);
		}
	}
}
