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
using OpenRA;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Resources")]
	public class ResourceProperties : ScriptPlayerProperties, Requires<PlayerResourcesInfo>
	{
		readonly PlayerResources pr;

		public ResourceProperties(Player player)
			: base(player)
		{
			pr = player.PlayerActor.Trait<PlayerResources>();
		}

		[Desc("The amount of harvestable resources held by the player.")]
		public int Resources
		{
			get { return pr.Ore; }
			set { pr.Ore = value.Clamp(0, pr.OreCapacity); }
		}

		[Desc("The maximum resource storage of the player.")]
		public int ResourceCapacity { get { return pr.OreCapacity; } }

		[Desc("The amount of cash held by the player.")]
		public int Cash
		{
			get { return pr.Cash; }
			set { pr.Cash = Math.Max(0, value); }
		}
	}
}