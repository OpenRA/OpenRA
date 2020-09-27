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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class CreatesShroudInfo : AffectsShroudInfo
	{
		[Desc("Relationship the watching player needs to see the generated shroud.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new CreatesShroud(init.Self, this); }
	}

	public class CreatesShroud : AffectsShroud
	{
		readonly CreatesShroudInfo info;
		IEnumerable<int> rangeModifiers;

		public CreatesShroud(Actor self, CreatesShroudInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			rangeModifiers = self.TraitsImplementing<ICreatesShroudModifier>().ToArray().Select(x => x.GetCreatesShroudModifier());
		}

		protected override void AddCellsToPlayerShroud(Actor self, Player p, PPos[] uv)
		{
			if (!info.ValidRelationships.HasStance(self.Owner.RelationshipWith(p)))
				return;

			p.Shroud.AddSource(this, Shroud.SourceType.Shroud, uv);
		}

		protected override void RemoveCellsFromPlayerShroud(Actor self, Player p) { p.Shroud.RemoveSource(this); }

		public override WDist Range
		{
			get
			{
				if (CachedTraitDisabled)
					return WDist.Zero;

				var range = Util.ApplyPercentageModifiers(Info.Range.Length, rangeModifiers);
				return new WDist(range);
			}
		}
	}
}
