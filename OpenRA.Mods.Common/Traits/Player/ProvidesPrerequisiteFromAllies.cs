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
	[Desc("Provides a prerequisite based on the factions of allied players, if any.")]
	public class ProvidesPrerequisiteFromAlliesInfo : ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides."), FieldLoader.Require]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when this player is one of certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Only grant this prerequisite when the allied player is one of certain factions."), FieldLoader.Require]
		public readonly HashSet<string> AlliedFactions = new HashSet<string>();

		public object Create(ActorInitializer init) { return new ProvidesPrerequisiteFromAllies(init, this); }
	}

	public class ProvidesPrerequisiteFromAllies : ITechTreePrerequisite, INotifyCreated
	{
		readonly ProvidesPrerequisiteFromAlliesInfo info;
		readonly string prerequisite;

		bool enabled;

		public ProvidesPrerequisiteFromAllies(ActorInitializer init, ProvidesPrerequisiteFromAlliesInfo info)
		{
			this.info = info;
			prerequisite = info.Prerequisite;
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

		void INotifyCreated.Created(Actor self)
		{
			self.World.AddFrameEndTask(world =>
			{
				if (info.Factions.Count > 0 && !info.Factions.Contains(self.Owner.Faction.InternalName))
					return;

				enabled = self.World.Players.Any(p =>
					p != self.Owner
					&& self.Owner.Stances[p] == Stance.Ally
					&& info.AlliedFactions.Contains(p.Faction.InternalName));
			});
		}
	}
}
