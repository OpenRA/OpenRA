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

	public class ExternalCondition : ITick
	{
		class TimedToken
		{
			public readonly int Token;
			public readonly int Expires;
			public readonly object Source;

			public TimedToken(int token, Actor self, object source, int duration)
			{
				Token = token;
				Expires = self.World.WorldTick + duration;
				Source = source;
			}
		}

		public readonly ExternalConditionInfo Info;
		readonly ConditionManager conditionManager;
		readonly Dictionary<object, HashSet<int>> permanentTokens = new Dictionary<object, HashSet<int>>();
		readonly Dictionary<object, Dictionary<int, TimedToken>> timedTokensBySource = new Dictionary<object, Dictionary<int, TimedToken>>();
		readonly List<KeyValuePair<int, TimedToken>> timedTokensByExpiration = new List<KeyValuePair<int, TimedToken>>();

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
			if (Info.SourceCap > 0)
			{
				HashSet<int> permanentTokensForSource;
				if (permanentTokens.TryGetValue(source, out permanentTokensForSource) && permanentTokensForSource.Count >= Info.SourceCap)
					return false;
			}

			if (Info.TotalCap > 0 && permanentTokens.Values.Sum(t => t.Count) >= Info.TotalCap)
				return false;

			return true;
		}

		void RemoveFromExpiration(TimedToken timedToken)
		{
			timedTokensByExpiration.RemoveAt(timedTokensByExpiration.FindIndex(p => p.Value == timedToken));
		}

		void AddForExpiration(TimedToken timedToken)
		{
			var index = timedTokensByExpiration.FindIndex(p => p.Key >= timedToken.Expires);
			if (index >= 0)
				timedTokensByExpiration.Insert(index, new KeyValuePair<int, TimedToken>(timedToken.Expires, timedToken));
			else
				timedTokensByExpiration.Add(new KeyValuePair<int, TimedToken>(timedToken.Expires, timedToken));
		}

		public int GrantCondition(Actor self, object source, int duration = 0)
		{
			if (!CanGrantCondition(self, source))
				return ConditionManager.InvalidConditionToken;

			var token = conditionManager.GrantCondition(self, Info.Condition, duration);
			HashSet<int> permanent;
			permanentTokens.TryGetValue(source, out permanent);

			if (duration > 0)
			{
				var timed = timedTokensBySource.GetOrAdd(source);

				// Check level caps
				if (Info.SourceCap > 0)
				{
					if ((permanent != null ? permanent.Count + timed.Count : timed.Count) >= Info.SourceCap)
					{
						var expire = timed.MinByOrDefault(t => t.Value.Expires).Value;
						if (expire != null)
						{
							timed.Remove(expire.Token);
							RemoveFromExpiration(expire);

							if (conditionManager.TokenValid(self, expire.Token))
								conditionManager.RevokeCondition(self, expire.Token);
						}
					}
				}

				if (Info.TotalCap > 0)
				{
					var totalCount = permanentTokens.Values.Sum(t => t.Count) + timedTokensByExpiration.Count;
					if (totalCount >= Info.TotalCap)
					{
						// Prefer tokens from the same source
						var expire = timedTokensByExpiration.FirstOrDefault().Value;
						if (expire != null)
						{
							if (conditionManager.TokenValid(self, expire.Token))
								conditionManager.RevokeCondition(self, expire.Token);

							timedTokensBySource[expire.Source].Remove(expire.Token);
							RemoveFromExpiration(expire);
						}
					}
				}

				var timedToken = new TimedToken(token, self, source, duration);
				timed.Add(token, timedToken);
				AddForExpiration(timedToken);
			}
			else if (permanent == null)
				permanentTokens.Add(source, new HashSet<int> { token });
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

			HashSet<int> permanentTokensForSource;
			if (permanentTokens.TryGetValue(source, out permanentTokensForSource))
			{
				if (!permanentTokensForSource.Remove(token))
					return false;
			}
			else
			{
				Dictionary<int, TimedToken> timedTokensForSource;
				if (timedTokensBySource.TryGetValue(source, out timedTokensForSource))
				{
					TimedToken timedToken;
					if (!timedTokensForSource.TryGetValue(token, out timedToken))
						return false;

					timedTokensForSource.Remove(token);
					RemoveFromExpiration(timedToken);
				}
			}

			if (conditionManager.TokenValid(self, token))
				conditionManager.RevokeCondition(self, token);

			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (timedTokensByExpiration.Count == 0)
				return;

			// Remove expired tokens
			var worldTick = self.World.WorldTick;
			var pair = timedTokensByExpiration[0];
			while (pair.Key < worldTick)
			{
				var timed = pair.Value;
				var timedTokensForSource = timedTokensBySource[timed.Source];
				timedTokensForSource.Remove(timed.Token);
				if (timedTokensForSource.Count == 0)
					timedTokensBySource.Remove(timed.Source);

				timedTokensByExpiration.RemoveAt(0);
				if (timedTokensByExpiration.Count == 0)
					return;

				pair = timedTokensByExpiration[0];
			}
		}
	}
}
