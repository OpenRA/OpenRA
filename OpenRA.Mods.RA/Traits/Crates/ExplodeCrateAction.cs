#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Fires a weapon at the location when collected.")]
	class ExplodeCrateActionInfo : CrateActionInfo
	{
		[Desc("The weapon to fire upon collection.")]
		[WeaponReference] public string Weapon = null;

		public override object Create(ActorInitializer init) { return new ExplodeCrateAction(init.self, this); }
	}

	class ExplodeCrateAction : CrateAction
	{
		readonly ExplodeCrateActionInfo info;

		public ExplodeCrateAction(Actor self, ExplodeCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor collector)
		{
			var weapon = collector.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];
			weapon.Impact(Target.FromPos(collector.CenterPosition), collector, Enumerable.Empty<int>());

			base.Activate(collector);
		}
	}
}
