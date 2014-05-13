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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to the player actor.")]
	public class UpgradeManagerInfo : ITraitInfo, Requires<TechTreeInfo>
	{
		public object Create(ActorInitializer init) { return new UpgradeManager(init); }
	}

	public class UpgradeManager : ITechTreeElement
	{
		public readonly Actor self;
		public readonly Dictionary<string, List<INotifyUpgrade>> Upgradables = new Dictionary<string, List<INotifyUpgrade>>();

		public readonly TechTree TechTree;

		public UpgradeManager(ActorInitializer init)
		{
			self = init.self;
			TechTree = self.Trait<TechTree>();

			init.world.ActorAdded += ActorAdded;
			init.world.ActorRemoved += ActorRemoved;
		}

		void ActorAdded(Actor a)
		{
			if (a.Owner != self.Owner)
				return;

			foreach (var t in a.TraitsImplementing<INotifyUpgrade>())
			{
				if (t.UpgradePrerequisites.Any())
				{
					if (!Upgradables.ContainsKey(t.UpgradeKey))
					{
						Upgradables.Add(t.UpgradeKey, new List<INotifyUpgrade>());

						if (t.UpgradePrerequisites.Any())
						{
							TechTree.Add(t.UpgradeKey, t.UpgradePrerequisites, 0, this);
						}
					}

					t.OnUpgrade(t.UpgradeKey, TechTree.HasPrerequisites(t.UpgradePrerequisites));
					Upgradables[t.UpgradeKey].Add(t);
				}
			}
		}

		void ActorRemoved(Actor a)
		{
			if (a.Owner != self.Owner || !a.HasTrait<INotifyUpgrade>())
				return;

			foreach (var t in a.TraitsImplementing<INotifyUpgrade>())
			{
				Upgradables[t.UpgradeKey].Remove(t);

				if (Upgradables[t.UpgradeKey].Count == 0)
				{
					Upgradables.Remove(t.UpgradeKey);
					TechTree.Remove(t.UpgradeKey);
				}
			}
		}

		public void PrerequisitesAvailable(string key)
		{
			List<INotifyUpgrade> ups;
			if (Upgradables.TryGetValue(key, out ups))
			{
				foreach (var up in ups)
					up.OnUpgrade(key, true);
			}
		}

		public void PrerequisitesUnavailable(string key)
		{
			List<INotifyUpgrade> ups;
			if (Upgradables.TryGetValue(key, out ups))
			{
				foreach (var up in ups)
					up.OnUpgrade(key, false);
			}
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
