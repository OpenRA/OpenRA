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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The inaccuracy of this actor is multiplied based on upgrade level if specified.")]
	public class InaccuracyMultiplierInfo : UpgradeMultiplierTraitInfo
	{
		public override object Create(ActorInitializer init) { return new InaccuracyMultiplier(this, init.Self.Info.Name); }
	}

	public class InaccuracyMultiplier : UpgradeMultiplierTrait, IInaccuracyModifier
	{
		public InaccuracyMultiplier(InaccuracyMultiplierInfo info, string actorType)
			: base(info, "InaccuracyMultiplier", actorType) { }

		public int GetInaccuracyModifier() { return GetModifier(); }
	}
}
