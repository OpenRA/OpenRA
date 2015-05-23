#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Handle demolitions from C4 explosives.")]
	public class DemolishableInfo : IDemolishableInfo, ITraitInfo
	{
		public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return true; }

		public object Create(ActorInitializer init) { return new Demolishable(); }
	}

	public class Demolishable : IDemolishable
	{
		public void Demolish(Actor self, Actor saboteur)
		{
			self.Kill(saboteur);
		}

		public bool IsValidTarget(Actor self, Actor saboteur)
		{
			return true;
		}
	}
}