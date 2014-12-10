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

namespace OpenRA.Mods.Common
{
	[Desc("The inaccuracy of this actor is multipled based on upgrade level.")]
	public class InaccuracyMultiplierInfo : UpgradeMultiplierTraitInfo, ITraitInfo
	{
		public InaccuracyMultiplierInfo()
			: base(new string[] { "inaccuracy" }, new int[] { 90, 80, 70, 50 }) { }

		public object Create(ActorInitializer init) { return new InaccuracyMultiplier(this); }
	}

	public class InaccuracyMultiplier : UpgradeMultiplierTrait, IInaccuracyModifier
	{
		public InaccuracyMultiplier(InaccuracyMultiplierInfo info)
			: base(info) { }

		public int GetInaccuracyModifier() { return GetModifier(); }
	}
}
