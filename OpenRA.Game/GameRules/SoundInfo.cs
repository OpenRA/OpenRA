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

			VoicePools = Exts.Lazy(() => Voices.ToDictionary(a => a.Key, a => new SoundPool(1f, a.Value)));
			NotificationsPools = Exts.Lazy(() => ParseSoundPool(y, "Notifications"));
		}

		Dictionary<string, SoundPool> ParseSoundPool(MiniYaml y, string key)
		{
			var ret = new Dictionary<string, SoundPool>();
			var classifiction = y.Nodes.First(x => x.Key == key);
			foreach (var t in classifiction.Value.Nodes)
			{
				var volumeModifier = 1f;
				var volumeModifierNode = t.Value.Nodes.FirstOrDefault(x => x.Key == "VolumeModifier");
				if (volumeModifierNode != null)
					volumeModifier = FieldLoader.GetValue<float>(volumeModifierNode.Key, volumeModifierNode.Value.Value);

				var names = FieldLoader.GetValue<string[]>(t.Key, t.Value.Value);
				var sp = new SoundPool(volumeModifier, names);
				ret.Add(t.Key, sp);
			}

			return ret;
		}
	}

	public class SoundPool
	{
		public readonly float VolumeModifier;
		readonly string[] clips;
		readonly List<string> liveclips = new List<string>();

		public SoundPool(float volumeModifier, params string[] clips)
		{
			VolumeModifier = volumeModifier;
			this.clips = clips;
		}

		public string GetNext()
		{
			if (liveclips.Count == 0)
				liveclips.AddRange(clips);

			// Avoid crashing if there's no clips at all
			if (liveclips.Count == 0)
				return null;

			var i = Game.CosmeticRandom.Next(liveclips.Count);
			var s = liveclips[i];
			liveclips.RemoveAt(i);
			return s;
		}
	}
}
