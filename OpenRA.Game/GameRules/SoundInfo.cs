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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.GameRules
{
	public class SoundInfo
	{
		public readonly Dictionary<string, string[]> Variants = new Dictionary<string, string[]>();
		public readonly Dictionary<string, string[]> Prefixes = new Dictionary<string, string[]>();
		public readonly Dictionary<string, string[]> Voices = new Dictionary<string, string[]>();
		public readonly Dictionary<string, string[]> Notifications = new Dictionary<string, string[]>();
		public readonly string DefaultVariant = ".aud";
		public readonly string DefaultPrefix = "";
		public readonly HashSet<string> DisableVariants = new HashSet<string>();
		public readonly HashSet<string> DisablePrefixes = new HashSet<string>();

		public readonly Lazy<Dictionary<string, SoundPool>> VoicePools;
		public readonly Lazy<Dictionary<string, SoundPool>> NotificationsPools;

		public SoundInfo(MiniYaml y)
		{
			FieldLoader.Load(this, y);

			VoicePools = Exts.Lazy(() => Voices.ToDictionary(a => a.Key, a => new SoundPool(0, 1f, a.Value)));
			NotificationsPools = Exts.Lazy(() => ParseSoundPool(y, "Notifications"));
		}

		Dictionary<string, SoundPool> ParseSoundPool(MiniYaml y, string key)
		{
			var ret = new Dictionary<string, SoundPool>();
			var classifiction = y.Nodes.First(x => x.Key == key);
			foreach (var t in classifiction.Value.Nodes)
			{
				var rateLimit = 0;
				var rateLimitNode = t.Value.Nodes.FirstOrDefault(x => x.Key == "RateLimit");
				if (rateLimitNode != null)
					rateLimit = FieldLoader.GetValue<int>(rateLimitNode.Key, rateLimitNode.Value.Value);

				var volumeModifier = 1f;
				var volumeModifierNode = t.Value.Nodes.FirstOrDefault(x => x.Key == "VolumeModifier");
				if (volumeModifierNode != null)
					volumeModifier = FieldLoader.GetValue<float>(volumeModifierNode.Key, volumeModifierNode.Value.Value);

				var names = FieldLoader.GetValue<string[]>(t.Key, t.Value.Value);
				var sp = new SoundPool(rateLimit, volumeModifier, names);
				ret.Add(t.Key, sp);
			}

			return ret;
		}
	}

	public class SoundPool
	{
		public readonly float VolumeModifier;
		readonly string[] clips;
		readonly int rateLimit;
		readonly List<string> liveclips = new List<string>();
		long lastPlayed = 0;

		public SoundPool(int rateLimit, float volumeModifier, params string[] clips)
		{
			VolumeModifier = volumeModifier;
			this.clips = clips;
			this.rateLimit = rateLimit;
		}

		public string GetNext()
		{
			if (liveclips.Count == 0)
				liveclips.AddRange(clips);

			// Avoid crashing if there's no clips at all
			if (liveclips.Count == 0)
				return null;

			// Perform rate limiting if necessary
			if (rateLimit != 0)
			{
				var now = Game.RunTime;
				if (lastPlayed != 0 && now < lastPlayed + rateLimit)
					return null;

				lastPlayed = now;
			}

			var i = Game.CosmeticRandom.Next(liveclips.Count);
			var s = liveclips[i];
			liveclips.RemoveAt(i);
			return s;
		}
	}
}
