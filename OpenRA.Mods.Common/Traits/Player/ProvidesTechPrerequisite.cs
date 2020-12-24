#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ProvidesTechPrerequisiteInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Internal id for this tech level.")]
		public readonly string Id;

		[Desc("Name shown in the lobby options.")]
		public readonly string Name;

		[Desc("Prerequisites to grant when this tech level is active.")]
		public readonly string[] Prerequisites = { };

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info) { return Prerequisites; }

		public override object Create(ActorInitializer init) { return new ProvidesTechPrerequisite(this, init); }
	}

	public class ProvidesTechPrerequisite : ITechTreePrerequisite
	{
		readonly ProvidesTechPrerequisiteInfo info;
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
			var mapOptions = init.World.WorldActor.TraitOrDefault<MapOptions>();
			enabled = mapOptions != null && mapOptions.TechLevel == info.Id;
		}
	}
}
