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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	public class BotProvidesPrerequisiteInfo : ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = { };

		// Disable humans from building AI stuff by setting this to TRUE.
		// Don't set this to false, even though you can.
		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = true;
		public object Create(ActorInitializer init) { return new BotProvidesPrerequisite(init, this); }
	}

	// Only grants prerequisite when the player is a bot.
	public class BotProvidesPrerequisite : ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly BotProvidesPrerequisiteInfo info;
		readonly string prerequisite;

		bool enabled = true;

		public BotProvidesPrerequisite(ActorInitializer init, BotProvidesPrerequisiteInfo info)
		{
			this.info = info;
			prerequisite = info.Prerequisite;

			if (string.IsNullOrEmpty(prerequisite))
				prerequisite = init.Self.Info.Name;

			Update(init.Self.Owner, init.Self.Owner.IsBot);
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
				Update(newOwner, newOwner.IsBot);
		}

		void Update(Player owner, bool is_bot)
		{
			enabled = is_bot;

			if (info.RequiresPrerequisites.Any() && enabled)
				enabled = owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.RequiresPrerequisites);
		}
	}
}
