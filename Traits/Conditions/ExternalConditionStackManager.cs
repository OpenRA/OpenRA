#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This should be part of ConditionManager IMO.")]
	class ExternalConditionStackManagerInfo : ITraitInfo, Requires<ConditionManagerInfo>
	{
		public object Create(ActorInitializer init)
		{
			return new ExternalConditionStackManager(init.Self);
		}
	}

	public class ExternalConditionStackManager : ITick
	{
		// Have to maintain the timers in this shim, because if a condition expires within ConditionManager,
		// you have no method to also expire the token associated with it.
		public class ManagedExternalConditionEntry
		{
			public int Token;
			public Actor Source;
			public int Duration;

			public ManagedExternalConditionEntry(int token, Actor sourceActor, int duration)
			{
				Token = token;
				Source = sourceActor;
				Duration = duration;
			}
		}

		readonly Actor self;
		readonly ConditionManager cm;

		Dictionary<string, HashSet<ManagedExternalConditionEntry>> managedExternalConditions;

		public ExternalConditionStackManager(Actor self)
		{
			this.self = self;
			cm = self.Trait<ConditionManager>();
			managedExternalConditions = new Dictionary<string, HashSet<ManagedExternalConditionEntry>>();
		}

		public void GrantExternalCondition(string condition, Actor sourceActor, int duration = 0, int maxStacks = 1)
		{
			if (managedExternalConditions.Keys.Contains(condition))
			{
				var mecentry = managedExternalConditions[condition];
				if (mecentry.Count(x => x.Source == sourceActor) < maxStacks)
				{
					// Need to allow the appliance of timed conditions unconditionally, because ConditionManager
					// will consider them permanent on it's own and would return false if the max value is reached with timed durations.
					if (duration == 0 && !cm.AcceptsExternalCondition(self, condition))
						return;

					var token = cm.GrantCondition(self, condition, true);
					mecentry.Add(new ManagedExternalConditionEntry(token, sourceActor, duration));
				}
				else
				{
					if (duration != 0 && mecentry.Where(x => x.Source == sourceActor).Min(x => x.Duration) < duration)
					{
						var currEntry = mecentry.Where(x => x.Source == sourceActor && x.Duration > 0).MinBy(x => x.Duration);
						if (currEntry != null)
							currEntry.Duration = duration;
					}
				}
			}
			else
			{
				if (!cm.AcceptsExternalCondition(self, condition))
						return;

				var token = cm.GrantCondition(self, condition, true);

				managedExternalConditions.Add(condition,
					new HashSet<ManagedExternalConditionEntry>() {new ManagedExternalConditionEntry(token, sourceActor, duration)});
			}
		}


		void ITick.Tick(Actor self)
		{
			var removeDict = new Dictionary<string, HashSet<ManagedExternalConditionEntry>>();

			foreach (var v in managedExternalConditions)
			{
				removeDict.Add(v.Key, v.Value.Where(x => x.Duration == 1).ToHashSet());
				
				v.Value.Do(x =>
				{
					if (x.Duration > 0)
						x.Duration--;
				});
			}

			foreach (var remove in removeDict)
			{
				foreach (var v in remove.Value)
				{
					cm.RevokeCondition(self, v.Token);
					managedExternalConditions[remove.Key].Remove(v);
				}
			}
		}
	}
}
