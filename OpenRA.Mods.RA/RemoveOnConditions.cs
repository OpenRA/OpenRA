#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.RA
{
	[Desc("Destroys the actor after a specified number of ticks if all conditions are met.")]
	class RemoveOnConditionsInfo : ITraitInfo
	{
		[Desc("Prerequisites required before removal")]
		public readonly string[] Prerequisites = {};

		[Desc("Delay until it starts checking if you have the prerequisites", "0 = Removal attempted on AddedToWorld")]
		public readonly int Delay = 0;

		[Desc("Should the trait kill instead of destroy?")]
		public readonly bool KillInstead = false;

		public object Create(ActorInitializer init) { return new RemoveOnConditions(init.self, this); }
	}

	class RemoveOnConditions : INotifyAddedToWorld, ITechTreeElement
	{
		readonly RemoveOnConditionsInfo info;
		readonly Actor self;

		public RemoveOnConditions(Actor self, RemoveOnConditionsInfo info)
		{
			this.info = info;
			this.self = self;
		}

		public void AddedToWorld(Actor self)
		{
			Action act = () =>
			{
				if (!info.Prerequisites.Any() || self.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.Prerequisites))
					Remove();
				else
					self.Owner.PlayerActor.Trait<TechTree>().Add("remove_" + string.Join("_", info.Prerequisites.OrderBy(a => a)), info.Prerequisites, 0, this);
			};

			if (info.Delay <= 0 && (!info.Prerequisites.Any() || self.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.Prerequisites)))
				Remove();
			else
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.Delay, act)));
		}

		void Remove()
		{
			if (!self.IsDead())
			{
				if (info.KillInstead && self.HasTrait<Health>())
					self.Kill(self);
				else
					self.Destroy();
			}
		}

		public void PrerequisitesAvailable(string key) { Remove(); }
		public void PrerequisitesUnavailable(string key) { }
		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
