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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be targeted.")]
	public class TargetableInfo : ConditionalTraitInfo, ITargetableInfo
	{
		[Desc("Target type. Used for filtering (in)valid targets.")]
		public readonly BitSet<TargetableType> TargetTypes;
		public BitSet<TargetableType> GetTargetTypes() { return TargetTypes; }

		public bool RequiresForceFire = false;

		public override object Create(ActorInitializer init) { return new Targetable(init.Self, this); }
	}

	public class Targetable : ConditionalTrait<TargetableInfo>, ITargetable
	{
		protected static readonly string[] None = new string[] { };
		protected Cloak[] cloaks;

		public Targetable(Actor self, TargetableInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			cloaks = self.TraitsImplementing<Cloak>().ToArray();

			base.Created(self);
		}

		public virtual bool TargetableBy(Actor self, Actor viewer)
		{
			if (IsTraitDisabled)
				return false;

			if (!cloaks.Any() || (!viewer.IsDead && viewer.Info.HasTraitInfo<IgnoresCloakInfo>()))
				return true;

			return cloaks.All(c => c.IsTraitDisabled || c.IsVisible(self, viewer.Owner));
		}

		public virtual BitSet<TargetableType> TargetTypes { get { return Info.TargetTypes; } }

		public bool RequiresForceFire { get { return Info.RequiresForceFire; } }
	}
}
