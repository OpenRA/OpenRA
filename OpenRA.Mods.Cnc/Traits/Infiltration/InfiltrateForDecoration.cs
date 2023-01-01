#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Reveals a decoration sprite to the indicated players when infiltrated.")]
	class InfiltrateForDecorationInfo : WithDecorationInfo
	{
		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		public override object Create(ActorInitializer init) { return new InfiltrateForDecoration(init.Self, this); }
	}

	class InfiltrateForDecoration : WithDecoration, INotifyInfiltrated
	{
		readonly HashSet<Player> infiltrators = new HashSet<Player>();
		readonly InfiltrateForDecorationInfo info;

		public InfiltrateForDecoration(Actor self, InfiltrateForDecorationInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			infiltrators.Add(infiltrator.Owner);
		}

		protected override bool ShouldRender(Actor self)
		{
			return infiltrators.Any(i => Info.ValidRelationships.HasRelationship(i.RelationshipWith(self.World.RenderPlayer)));
		}
	}
}
