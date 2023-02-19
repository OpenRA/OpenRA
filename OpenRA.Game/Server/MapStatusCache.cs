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

using System;
using System.Collections.Generic;
using System.Threading;
using OpenRA.Network;

namespace OpenRA.Server
{
	public interface ILintServerMapPass { void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules); }

	public class MapStatusCache
	{
		readonly Dictionary<MapPreview, Session.MapStatus> cache = new Dictionary<MapPreview, Session.MapStatus>();
		readonly Action<string, Session.MapStatus> onStatusChanged;
		readonly bool enableRemoteLinting;
		readonly ModData modData;

		public MapStatusCache(ModData modData, Action<string, Session.MapStatus> onStatusChanged, bool enableRemoteLinting)
		{
			this.modData = modData;
			this.enableRemoteLinting = enableRemoteLinting;
			this.onStatusChanged = onStatusChanged;
		}

		void RunLintTests(MapPreview map, Ruleset rules)
		{
			var status = cache[map];
			var failed = false;

			void OnLintFailure(string message)
			{
				Log.Write("server", "Map {0} failed lint with error: {1}", map.Title, message);
				failed = true;
			}

			void OnLintWarning(string _) { }

			foreach (var customMapPassType in modData.ObjectCreator.GetTypesImplementing<ILintServerMapPass>())
			{
				try
				{
					var customMapPass = (ILintServerMapPass)modData.ObjectCreator.CreateBasic(customMapPassType);
					customMapPass.Run(OnLintFailure, OnLintWarning, modData, map, rules);
				}
				catch (Exception e)
				{
					OnLintFailure(e.ToString());
				}
			}

			status &= ~Session.MapStatus.Validating;
			status |= failed ? Session.MapStatus.Incompatible : Session.MapStatus.Playable;

			cache[map] = status;
			onStatusChanged?.Invoke(map.Uid, status);
		}

		public Session.MapStatus this[MapPreview map]
		{
			get
			{
				if (cache.TryGetValue(map, out var status))
					return status;

				Ruleset rules = null;
				try
				{
					rules = map.LoadRuleset();

					// Locally installed maps are assumed to not require linting
					status = enableRemoteLinting && map.Status != MapStatus.Available ? Session.MapStatus.Validating : Session.MapStatus.Playable;
					if (map.DefinesUnsafeCustomRules())
						status |= Session.MapStatus.UnsafeCustomRules;
				}
				catch (Exception e)
				{
					Log.Write("server", "Failed to load rules for `{0}` with error :{1}", map.Title, e.Message);
					status = Session.MapStatus.Incompatible;
				}

				if (map.Players.Players.Count > MapPlayers.MaximumPlayerCount)
				{
					Log.Write("server", "Failed to load `{0}`: Player count exceeds maximum ({1}/{2}).", map.Title, map.Players.Players.Count, MapPlayers.MaximumPlayerCount);
					status = Session.MapStatus.Incompatible;
				}

				cache[map] = status;

				if ((status & Session.MapStatus.Validating) != 0)
					ThreadPool.QueueUserWorkItem(_ => RunLintTests(map, rules));

				return status;
			}
		}
	}
}
