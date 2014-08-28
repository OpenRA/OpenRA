#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Graphics;

//namespace OpenRA.Traits
namespace OpenRA.Mods.RA
{
	public class ProvidesSciencePrerequisiteInfo : ITraitInfo
	{
		[Desc("Describe which sciences are provided")]
		public string[] Sciences = { };

		public object Create(ActorInitializer init) { return new ProvidesSciencePrerequisite(init.self, this); }
	}

	public class ProvidesSciencePrerequisite
	{
		public readonly Player Owner;
		public string[] Sciences;

		public ProvidesSciencePrerequisite(Actor self, ProvidesSciencePrerequisiteInfo info)
		{
			Owner = self.Owner;
			Sciences = info.Sciences;
		}

		public void AddScience(Player owner, string science)
		{
			Array.Resize(ref Sciences, Sciences.Length + 1);
			Sciences[Sciences.Length - 1] = science;
			owner.PlayerActor.Trait<TechTree>().Update();
		}
	}
}
