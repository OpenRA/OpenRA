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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Reveals a decoration sprite to the indicated players when infiltrated.")]
	class InfiltrateForDecorationInfo : WithDecorationInfo
	{
		public override object Create(ActorInitializer init) { return new InfiltrateForDecoration(init.Self, this); }
	}

	class InfiltrateForDecoration : WithDecoration, INotifyInfiltrated
	{
		readonly HashSet<Player> infiltrators = new HashSet<Player>();

		public InfiltrateForDecoration(Actor self, InfiltrateForDecorationInfo info)
			: base(self, info) { }

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			infiltrators.Add(infiltrator.Owner);
		}

		protected override bool ShouldRender(Actor self)
		{
			return self.World.RenderPlayer == null || infiltrators.Any(i =>
				Info.ValidStances.HasStance(i.Stances[self.World.RenderPlayer]));
		}
	}
}
