#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ScaredyCatInfo : ITraitInfo
	{
		public readonly int MoveRadius = 2;

		public object Create(ActorInitializer init) { return new ScaredyCat(init.self, this); }
	}

	class ScaredyCat : INotifyIdle, INotifyDamage
	{
		readonly ScaredyCatInfo Info;
		public bool Panicked = false;
		public ScaredyCat(Actor self, ScaredyCatInfo info)
		{
			Info = info;
		}

		public void TickIdle(Actor self)
		{
			if (!Panicked)
				return;

			var target = Util.SubPxVector[self.World.SharedRandom.Next(255)]* Info.MoveRadius / 1024 + self.Location;
			self.Trait<Mobile>().ResolveOrder(self, new Order("Move", self, false) { TargetLocation = target });
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			Panicked = true;
		}
	}

	class RenderInfantryPanicInfo : RenderInfantryInfo, Requires<ScaredyCatInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderInfantryPanic(init.self, this); }
	}

	class RenderInfantryPanic : RenderInfantry
	{
		readonly ScaredyCat sc;
		bool wasPanic;

		public RenderInfantryPanic(Actor self, RenderInfantryPanicInfo info)
			: base(self, info)
		{
			sc = self.Trait<ScaredyCat>();
		}

		protected override string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			var prefix = sc != null && sc.Panicked ? "panic-" : "";

			if (anim.HasSequence(prefix + baseSequence))
				return prefix + baseSequence;
			else
				return baseSequence;
		}

		protected override bool AllowIdleAnimation(Actor self)
		{
			return base.AllowIdleAnimation(self) && !sc.Panicked;
		}

		public override void Tick (Actor self)
		{
			if (wasPanic != sc.Panicked)
				dirty = true;

			wasPanic = sc.Panicked;
			base.Tick(self);
		}
	}
}
