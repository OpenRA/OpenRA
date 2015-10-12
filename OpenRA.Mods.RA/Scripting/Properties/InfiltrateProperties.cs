﻿#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class InfiltrateProperties : ScriptActorProperties
	{
		public InfiltrateProperties(ScriptContext context, Actor self)
			: base(context, self)
		{ }

		[Desc("Infiltrate the target actor.")]
		public void Infiltrate(Actor target)
		{
			Self.QueueActivity(new Infiltrate(Self, target));
		}
	}
}
