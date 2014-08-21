#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Actor can be targeted.")]
	public class TargetableUnitInfo : ITraitInfo, ITargetableInfo
	{
		[Desc("Target type. Used for filtering (in)valid targets.")]
		public readonly string[] TargetTypes = { };
		public string[] GetTargetTypes() { return TargetTypes; }

		public bool RequiresForceFire = false;

		public virtual object Create(ActorInitializer init) { return new TargetableUnit(init.self, this); }
	}

	public class TargetableUnit : ITargetable
	{
		readonly TargetableUnitInfo info;
		protected Cloak cloak;

		public TargetableUnit(Actor self, TargetableUnitInfo info)
		{
			this.info = info;
			cloak = self.TraitOrDefault<Cloak>();
		}

		public virtual bool TargetableBy(Actor self, Actor viewer)
		{
			if (cloak == null || !cloak.Cloaked)
				return true;

			return cloak.IsVisible(self, viewer.Owner);
		}

		public virtual string[] TargetTypes { get { return info.TargetTypes; } }

		public virtual IEnumerable<WPos> TargetablePositions(Actor self)
		{
			yield return self.CenterPosition;
		}

		public bool RequiresForceFire { get { return info.RequiresForceFire; } }
	}
}
