#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Resources")]
	public class ResourceProperties : ScriptPlayerProperties, Requires<PlayerResourcesInfo>
	{
		readonly PlayerResources pr;

		public ResourceProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			pr = player.PlayerActor.Trait<PlayerResources>();
		}

		[Desc("The amount of harvestable resources held by the player.")]
		public int Resources
		{
			get { return pr.Resources; }
			set { pr.Resources = value.Clamp(0, pr.ResourceCapacity); }
		}

		[Desc("The maximum resource storage of the player.")]
		public int ResourceCapacity { get { return pr.ResourceCapacity; } }

		[Desc("The amount of cash held by the player.")]
		public int Cash
		{
			get { return pr.Cash; }
			set { pr.Cash = Math.Max(0, value); }
		}
	}
}