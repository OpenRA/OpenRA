#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be targeted.")]
	public class TargetableUnitInfo : UpgradableTraitInfo, ITargetableInfo
	{
		[Desc("Target type. Used for filtering (in)valid targets.")]
		public readonly string[] TargetTypes = { };
		public string[] GetTargetTypes() { return TargetTypes; }

		public bool RequiresForceFire = false;

		public override object Create(ActorInitializer init) { return new TargetableUnit(init.Self, this); }
	}

	public class TargetableUnit : UpgradableTrait<TargetableUnitInfo>, ITargetable
	{
		protected static readonly string[] None = new string[] { };
		protected Cloak cloak;

		public TargetableUnit(Actor self, TargetableUnitInfo info)
			: base(info)
		{
			cloak = self.TraitOrDefault<Cloak>();
		}

		public virtual bool TargetableBy(Actor self, Actor viewer)
		{
			if (IsTraitDisabled)
				return false;
			if (cloak == null || (!viewer.IsDead && viewer.HasTrait<IgnoresCloak>()))
				return true;

			return cloak.IsVisible(self, viewer.Owner);
		}

		public virtual string[] TargetTypes { get { return Info.TargetTypes; } }

		public virtual IEnumerable<WPos> TargetablePositions(Actor self)
		{
			yield return self.CenterPosition;
		}

		public bool RequiresForceFire { get { return Info.RequiresForceFire; } }
	}
}
