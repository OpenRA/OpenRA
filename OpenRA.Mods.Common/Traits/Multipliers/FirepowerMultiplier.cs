#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The firepower of this actor is multiplied based on upgrade level if specified.")]
	public class FirepowerMultiplierInfo : UpgradeMultiplierTraitInfo
	{
		public override object Create(ActorInitializer init) { return new FirepowerMultiplier(this, init.Self.Info.Name); }
	}

	public class FirepowerMultiplier : UpgradeMultiplierTrait, IFirepowerModifier
	{
		public FirepowerMultiplier(FirepowerMultiplierInfo info, string actorType)
			: base(info, "FirepowerMultiplier", actorType) { }

		public int GetFirepowerModifier() { return GetModifier(); }
	}
}
