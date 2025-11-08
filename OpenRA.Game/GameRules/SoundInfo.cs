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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace OpenRA.GameRules
{
	public class SoundInfo
	{
		public readonly FrozenDictionary<string, ImmutableArray<string>> Variants = FrozenDictionary<string, ImmutableArray<string>>.Empty;
		public readonly FrozenDictionary<string, ImmutableArray<string>> Prefixes = FrozenDictionary<string, ImmutableArray<string>>.Empty;
		public readonly FrozenDictionary<string, ImmutableArray<string>> Voices = FrozenDictionary<string, ImmutableArray<string>>.Empty;
		public readonly FrozenDictionary<string, ImmutableArray<string>> Notifications = FrozenDictionary<string, ImmutableArray<string>>.Empty;
		public readonly string DefaultVariant = ".aud";
		public readonly string DefaultPrefix = "";
		public readonly FrozenSet<string> DisableVariants = FrozenSet<string>.Empty;
		public readonly FrozenSet<string> DisablePrefixes = FrozenSet<string>.Empty;

		public readonly Lazy<FrozenDictionary<string, SoundPool>> VoicePools;
		public readonly Lazy<FrozenDictionary<string, SoundPool>> NotificationsPools;

		public SoundInfo(MiniYaml y)
		{
			FieldLoader.Load(this, y);

			VoicePools = Exts.Lazy(() => Voices.ToFrozenDictionary(a => a.Key, a => new SoundPool(1f, SoundPool.DefaultInterruptType, a.Value)));
			NotificationsPools = Exts.Lazy(() => ParseSoundPool(y, "Notifications"));
		}

		static FrozenDictionary<string, SoundPool> ParseSoundPool(MiniYaml y, string key)
		{
			var classifiction = y.NodeWithKey(key);
			var ret = new Dictionary<string, SoundPool>(classifiction.Value.Nodes.Length);
			foreach (var t in classifiction.Value.Nodes)
			{
				var volumeModifier = 1f;
				var volumeModifierNode = t.Value.NodeWithKeyOrDefault(nameof(SoundPool.VolumeModifier));
				if (volumeModifierNode != null)
					volumeModifier = FieldLoader.GetValue<float>(volumeModifierNode.Key, volumeModifierNode.Value.Value);

				var interruptType = SoundPool.DefaultInterruptType;
				var interruptTypeNode = t.Value.NodeWithKeyOrDefault(nameof(SoundPool.InterruptType));
				if (interruptTypeNode != null)
					interruptType = FieldLoader.GetValue<SoundPool.InterruptType>(interruptTypeNode.Key, interruptTypeNode.Value.Value);

				var names = FieldLoader.GetValue<ImmutableArray<string>>(t.Key, t.Value.Value);
				var sp = new SoundPool(volumeModifier, interruptType, names);
				ret.Add(t.Key, sp);
			}

			return ret.ToFrozenDictionary();
		}
	}

	public class SoundPool
	{
		public enum InterruptType { DoNotPlay, Interrupt, Overlap }
		public const InterruptType DefaultInterruptType = InterruptType.DoNotPlay;
		public readonly float VolumeModifier;
		public readonly InterruptType Type;
		readonly ImmutableArray<string> clips;
		readonly List<string> liveclips = [];

		public SoundPool(float volumeModifier, InterruptType interruptType, ImmutableArray<string> clips)
		{
			VolumeModifier = volumeModifier;
			Type = interruptType;
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
