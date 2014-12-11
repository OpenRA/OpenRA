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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ProvidesTechPrerequisiteInfo : ITraitInfo
	{
		public readonly string Name;
		public readonly string[] Prerequisites = {};

		public object Create(ActorInitializer init) { return new ProvidesTechPrerequisite(this, init); }
	}

	public class ProvidesTechPrerequisite : ITechTreePrerequisite
	{
		ProvidesTechPrerequisiteInfo info;
		bool enabled;
		static readonly string[] NoPrerequisites = new string[0];

		public string Name { get { return info.Name; } }

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				return enabled ? info.Prerequisites : NoPrerequisites;
			}
		}

		public ProvidesTechPrerequisite(ProvidesTechPrerequisiteInfo info, ActorInitializer init)
		{
			this.info = info;
			var tech = init.world.Map.Options.TechLevel ?? init.world.LobbyInfo.GlobalSettings.TechLevel;
			this.enabled = info.Name == tech;
		}
	}
}
