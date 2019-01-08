#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class CreatesShroudInfo : AffectsShroudInfo
	{
		[Desc("Stance the watching player needs to see the generated shroud.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		public override object Create(ActorInitializer init) { return new CreatesShroud(init.Self, this); }
	}

	public class CreatesShroud : AffectsShroud
	{
		readonly CreatesShroudInfo info;
		ICreatesShroudModifier[] rangeModifiers;

		public CreatesShroud(Actor self, CreatesShroudInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			rangeModifiers = self.TraitsImplementing<ICreatesShroudModifier>().ToArray();
		}

		protected override void AddCellsToPlayerShroud(Actor self, Player p, PPos[] uv)
		{
			if (!info.ValidStances.HasStance(p.Stances[self.Owner]))
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

				var revealsShroudModifier = rangeModifiers.Select(x => x.GetCreatesShroudModifier());
				var range = Util.ApplyPercentageModifiers(Info.Range.Length, revealsShroudModifier);
				return new WDist(range);
			}
		}
	}
}
