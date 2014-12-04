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
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Actor is disabled when this trait is enabled.")]
	public class DisableableInfo : ConditionalTraitInfo, ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Disableable(this); }
	}

	public class Disableable : ConditionalTrait<DisableableInfo>, IDisable, IDisableMove
	{
		public Disableable(DisableableInfo info)
			: base(info) { }

		// Disable the actor when this trait is enabled.
		public bool Disabled { get { return !IsTraitDisabled; } }
		public bool MoveDisabled(Actor self) { return !IsTraitDisabled; }
	}
}
