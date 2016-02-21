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

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class InfiltrateProperties : ScriptActorProperties, Requires<InfiltratesInfo>
	{
		readonly InfiltratesInfo info;

		public InfiltrateProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			info = Self.Info.TraitInfo<InfiltratesInfo>();
		}

		[Desc("Infiltrate the target actor.")]
		public void Infiltrate(Actor target)
		{
			Self.QueueActivity(new Infiltrate(Self, target, info.EnterBehaviour, info.ValidStances, info.Notification));
		}
	}
}
