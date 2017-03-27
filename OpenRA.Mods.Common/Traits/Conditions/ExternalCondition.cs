#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows a condition to be granted from an external source (Lua, warheads, etc).")]
	public class ExternalConditionInfo : ITraitInfo, Requires<ConditionManagerInfo>
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		public readonly string Condition = null;

		[Desc("If > 0, restrict the number of times that this condition can be granted by a single source.")]
		public readonly int SourceCap = 0;

		[Desc("If > 0, restrict the number of times that this condition can be granted by any source.")]
		public readonly int TotalCap = 0;

		public object Create(ActorInitializer init) { return new ExternalCondition(init.Self, this); }
	}

	public class ExternalCondition
	{
		class TimedToken
		{
			public int Token;
			public int Expires;
		}

		public readonly ExternalConditionInfo Info;
		readonly ConditionManager conditionManager;
		readonly Dictionary<object, HashSet<int>> permanentTokens = new Dictionary<object, HashSet<int>>();
		readonly Dictionary<object, HashSet<TimedToken>> timedTokens = new Dictionary<object, HashSet<TimedToken>>();

		public ExternalCondition(Actor self, ExternalConditionInfo info)
		{
			Info = info;
			conditionManager = self.Trait<ConditionManager>();
		}

		public bool CanGrantCondition(Actor self, object source)
		{
			if (conditionManager == null || source == null)
				return false;

			// Timed tokens do not count towards the source cap: the condition with the shortest
			// remaining duration can always be revoked to make room.
			if (Info.SourceCap > 0 && permanentTokens.GetOrAdd(source).Count >= Info.SourceCap)
				return false;

			if (Info.TotalCap > 0 && permanentTokens.Values.SelectMany(t => t).Count() >= Info.TotalCap)
				return false;

			return true;
		}

		public int GrantCondition(Actor self, object source, int duration = 0, int value = 1)
		{
			if (conditionManager == null || source == null || !CanGrantCondition(self, source))
				return ConditionManager.InvalidConditionToken;

			var token = conditionManager.GrantTimedCondition(self, Info.Condition, duration, value);
			var permanent = permanentTokens.GetOrAdd(source);

			if (duration > 0)
			{
				var timed = timedTokens.GetOrAdd(source);

				// Remove expired tokens
				timed.RemoveWhere(t => t.Expires < self.World.WorldTick);

				// Check level caps
				if (Info.SourceCap > 0)
				{
					if (permanent.Count + timed.Count >= Info.SourceCap)
					{
						var expire = timed.MinByOrDefault(t => t.Expires);
						if (expire != null)
						{
							timed.Remove(expire);
							if (conditionManager.TokenValid(self, expire.Token))
								conditionManager.RevokeCondition(self, expire.Token);
						}
					}
				}

				if (Info.TotalCap > 0)
				{
					var totalCount = permanentTokens.Values.SelectMany(t => t).Count() + timedTokens.Values.SelectMany(t => t).Count();
					if (totalCount >= Info.TotalCap)
					{
						// Prefer tokens from the same source
						var expire = timedTokens.SelectMany(t => t.Value.Select(tt => new Tuple<object, TimedToken>(t.Key, tt)))
							.MinByOrDefault(t => t.Item2.Expires);
						if (expire != null)
						{
							if (conditionManager.TokenValid(self, expire.Item2.Token))
								conditionManager.RevokeCondition(self, expire.Item2.Token);

							timedTokens[expire.Item1].Remove(expire.Item2);
						}
					}
				}

				timed.Add(new TimedToken { Expires = self.World.WorldTick + duration, Token = token });
			}
			else
				permanent.Add(token);

			return token;
		}

		/// <summary>Revokes the external condition with the given token if it was granted by this trait.</summary>
		/// <returns><c>true</c> if the now-revoked condition was originally granted by this trait.</returns>
		public bool TryRevokeCondition(Actor self, object source, int token)
		{
			if (conditionManager == null || source == null)
				return false;

			var removed = permanentTokens.GetOrAdd(source).Remove(token) ||
				timedTokens.GetOrAdd(source).RemoveWhere(t => t.Token == token) > 0;

			if (removed && conditionManager.TokenValid(self, token))
				conditionManager.RevokeCondition(self, token);

			return true;
		}
	}
}
