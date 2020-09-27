#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Display the time remaining until the next cash is given by actor's CashTrickler trait.")]
	class CashTricklerBarInfo : TraitInfo, Requires<CashTricklerInfo>
	{
		[Desc("Defines to which players the bar is to be shown.")]
		public readonly PlayerRelationship DisplayRelationships = PlayerRelationship.Ally;

		public readonly Color Color = Color.Magenta;

		public override object Create(ActorInitializer init) { return new CashTricklerBar(init.Self, this); }
	}

	class CashTricklerBar : ISelectionBar
	{
		readonly Actor self;
		readonly CashTricklerBarInfo info;
		readonly IEnumerable<CashTrickler> cashTricklers;

		public CashTricklerBar(Actor self, CashTricklerBarInfo info)
		{
			this.self = self;
			this.info = info;
			cashTricklers = self.TraitsImplementing<CashTrickler>().ToArray();
		}

		float ISelectionBar.GetValue()
		{
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !info.DisplayRelationships.HasStance(self.Owner.RelationshipWith(viewer)))
				return 0;

			var complete = cashTricklers.Min(ct => (float)ct.Ticks / ct.Info.Interval);
			return 1 - complete;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
