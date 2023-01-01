#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ProvidesTechPrerequisiteInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Internal id for this tech level.")]
		public readonly string Id;

		[TranslationReference]
		[Desc("Name shown in the lobby options.")]
		public readonly string Name;

		[Desc("Prerequisites to grant when this tech level is active.")]
		public readonly string[] Prerequisites = Array.Empty<string>();

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info) { return Prerequisites; }

		public override object Create(ActorInitializer init) { return new ProvidesTechPrerequisite(this, init); }
	}

	public class ProvidesTechPrerequisite : ITechTreePrerequisite
	{
		readonly ProvidesTechPrerequisiteInfo info;
		readonly bool enabled;

		static readonly string[] NoPrerequisites = Array.Empty<string>();

		public string Name => info.Name;

		public IEnumerable<string> ProvidesPrerequisites => enabled ? info.Prerequisites : NoPrerequisites;

		public ProvidesTechPrerequisite(ProvidesTechPrerequisiteInfo info, ActorInitializer init)
		{
			this.info = info;
			var mapOptions = init.World.WorldActor.TraitOrDefault<MapOptions>();
			enabled = mapOptions != null && mapOptions.TechLevel == info.Id;
		}
	}
}
