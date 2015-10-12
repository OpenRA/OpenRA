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
	[Desc("Used to define weapon efficiency modifiers with different percentages per Type.")]
	public class ArmorInfo : UpgradableTraitInfo
	{
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new Armor(init.Self, this); }
	}

	public class Armor : UpgradableTrait<ArmorInfo>
	{
		public Armor(Actor self, ArmorInfo info)
			: base(info) { }
	}
}