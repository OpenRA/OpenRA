#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Provides prerequisites to bots.")]
	public class ProvidesAIPrerequisiteInfo : ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = { };

		[Desc("Only grant this prerequisite for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		public object Create(ActorInitializer init) { return new ProvidesAIPrerequisite(init, this); }
	}

	public class ProvidesAIPrerequisite : ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly ProvidesAIPrerequisiteInfo info;
		readonly string prerequisite;

		bool enabled = true;

		public ProvidesAIPrerequisite(ActorInitializer init, ProvidesAIPrerequisiteInfo info)
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
			Update(newOwner, newOwner.Faction.InternalName);
		}

		void Update(Player owner, string faction)
		{
			if (info.Factions.Any())
				enabled = info.Factions.Contains(faction);

			if (info.RequiresPrerequisites.Any() && owner.IsBot && enabled)
				enabled = owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.RequiresPrerequisites);
		}
	}
}
