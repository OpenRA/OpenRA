#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface IConditionTimerWatcher
	{
		string Condition { get; }
		void Update(int duration, int remaining);
	}

	[Desc("Allows a condition to be granted from an external source (Lua, warheads, etc).")]
	public class ExternalConditionInfo : TraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		public readonly string Condition = null;

		[Desc("If > 0, restrict the number of times that this condition can be granted by a single source.")]
		public readonly int SourceCap = 0;

		[Desc("If > 0, restrict the number of times that this condition can be granted by any source.")]
		public readonly int TotalCap = 0;

		public override object Create(ActorInitializer init) { return new ExternalCondition(this); }
	}

	public class ExternalCondition : ITick, INotifyCreated
	{
		readonly struct TimedToken
		{
			public readonly int Expires;
			public readonly int Token;
			public readonly object Source;

			public TimedToken(int token, Actor self, object source, int duration)
			{
				Token = token;
				Expires = self.World.WorldTick + duration;
				Source = source;
			}
		}

		public readonly ExternalConditionInfo Info;
		readonly Dictionary<object, HashSet<int>> permanentTokens = new Dictionary<object, HashSet<int>>();

		// Tokens are sorted on insert/remove by ascending expiry time
		readonly List<TimedToken> timedTokens = new List<TimedToken>();
		IConditionTimerWatcher[] watchers;
		int duration;
		int expires;

		public ExternalCondition(ExternalConditionInfo info)
		{
			Info = info;
		}

		public bool CanGrantCondition(object source)
		{
			if (source == null)
				return false;

			// Timed tokens do not count towards the source cap: the condition with the shortest
			// remaining duration can always be revoked to make room.
			if (Info.SourceCap > 0)
				if (permanentTokens.TryGetValue(source, out var permanentTokensForSource) && permanentTokensForSource.Count >= Info.SourceCap)
					return false;

			if (Info.TotalCap > 0 && permanentTokens.Values.Sum(t => t.Count) >= Info.TotalCap)
				return false;

			return true;
		}

		public int GrantCondition(Actor self, object source, int duration = 0, int remaining = 0)
		{
			if (!CanGrantCondition(source))
				return Actor.InvalidConditionToken;

			var token = self.GrantCondition(Info.Condition);
			permanentTokens.TryGetValue(source, out var permanent);

			// Callers can override the amount of time remaining by passing a value
			// between 1 and the duration
			if (remaining <= 0 || remaining > duration)
				remaining = duration;

			if (duration > 0)
			{
				// Check level caps
				if (Info.SourceCap > 0)
				{
					var timedCount = timedTokens.Count(t => t.Source == source);
					if ((permanent?.Count ?? 0) + timedCount >= Info.SourceCap)
					{
						// Get timed token from the same source with closest expiration.
						var expireIndex = timedTokens.FindIndex(t => t.Source == source);
						if (expireIndex >= 0)
						{
							var expireToken = timedTokens[expireIndex].Token;
							timedTokens.RemoveAt(expireIndex);
							if (self.TokenValid(expireToken))
								self.RevokeCondition(expireToken);
						}
					}
				}

				if (Info.TotalCap > 0)
				{
					var totalCount = permanentTokens.Values.Sum(t => t.Count) + timedTokens.Count;
					if (totalCount >= Info.TotalCap && timedTokens.Count > 0)
					{
						var expire = timedTokens[0].Token;
						if (self.TokenValid(expire))
							self.RevokeCondition(expire);

						timedTokens.RemoveAt(0);
					}
				}

				var timedToken = new TimedToken(token, self, source, remaining);
				var index = timedTokens.FindIndex(t => t.Expires >= timedToken.Expires);
				if (index >= 0)
					timedTokens.Insert(index, timedToken);
				else
				{
					timedTokens.Add(timedToken);

					// Track the duration and expiration for the longest remaining timer.
					expires = timedToken.Expires;
					this.duration = duration;
				}
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
			if (source == null)
				return false;

			if (permanentTokens.TryGetValue(source, out var permanentTokensForSource))
			{
				if (!permanentTokensForSource.Remove(token))
					return false;
			}
			else
			{
				var index = timedTokens.FindIndex(p => p.Token == token);
				if (index >= 0 && timedTokens[index].Source == source)
					timedTokens.RemoveAt(index);
				else
					return false;
			}

			if (self.TokenValid(token))
				self.RevokeCondition(token);

			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (timedTokens.Count == 0)
				return;

			// Remove expired tokens
			var worldTick = self.World.WorldTick;
			var count = 0;
			while (count < timedTokens.Count && timedTokens[count].Expires < worldTick)
			{
				var token = timedTokens[count].Token;
				if (self.TokenValid(token))
					self.RevokeCondition(token);

				count++;
			}

			if (count > 0)
			{
				timedTokens.RemoveRange(0, count);
				if (timedTokens.Count == 0)
				{
					// Notify watchers that all timers have expired.
					foreach (var w in watchers)
						w.Update(0, 0);

					return;
				}
			}

			// Watchers will be receiving notifications while the condition is enabled.
			// They will also be provided with the number of ticks before the last timer ends,
			// as well as the duration of the longest active instance.
			if (timedTokens.Count > 0)
			{
				var remaining = expires - worldTick;
				foreach (var w in watchers)
					w.Update(duration, remaining);
			}
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }

		void INotifyCreated.Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();
		}
	}
}
