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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actors possessing this trait should define the GainsExperience trait. When the prerequisites are fulfilled, ",
		"this trait grants a level-up to newly spawned actors.")]
	public class ProducibleWithLevelInfo : TraitInfo, Requires<GainsExperienceInfo>
	{
		public readonly string[] Prerequisites = Array.Empty<string>();

		[Desc("Number of levels to give to the actor on creation.")]
		public readonly int InitialLevels = 1;

		[Desc("Should the level-up animation be suppressed when actor is created?")]
		public readonly bool SuppressLevelupAnimation = true;

		public override object Create(ActorInitializer init) { return new ProducibleWithLevel(this); }
	}

	public class ProducibleWithLevel : INotifyCreated
	{
		readonly ProducibleWithLevelInfo info;

		public ProducibleWithLevel(ProducibleWithLevelInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (!self.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.Prerequisites))
				return;

			var ge = self.Trait<GainsExperience>();
			if (!ge.CanGainLevel)
				return;

			ge.GiveLevels(info.InitialLevels, info.SuppressLevelupAnimation);
		}
	}
}
