#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class SoundInfo
	{
		[FieldLoader.Ignore] public readonly Dictionary<string,string[]> Variants;
		[FieldLoader.Ignore] public readonly Dictionary<string,string[]> Prefixes;
		[FieldLoader.Ignore] public readonly Dictionary<string,string[]> Voices;
		[FieldLoader.Ignore] public readonly Dictionary<string,string[]> Notifications;
		public readonly string DefaultVariant = ".aud" ;
		public readonly string DefaultPrefix = "" ;
		public readonly string[] DisableVariants = { };
		public readonly string[] DisablePrefixes = { };

		static Dictionary<string, string[]> Load(MiniYaml y, string name)
		{
			return y.NodesDict.ContainsKey(name)
				? y.NodesDict[name].NodesDict.ToDictionary(
					a => a.Key,
					a => FieldLoader.GetValue<string[]>("(value)", a.Value.Value))
				: new Dictionary<string, string[]>();
		}

		public readonly Lazy<Dictionary<string, SoundPool>> VoicePools;
		public readonly Lazy<Dictionary<string, SoundPool>> NotificationsPools;

		public SoundInfo( MiniYaml y )
		{
			FieldLoader.Load( this, y );
			Variants = Load(y, "Variants");
			Prefixes = Load(y, "Prefixes");
			Voices = Load(y, "Voices");
			Notifications = Load(y, "Notifications");

			VoicePools = Exts.Lazy(() => Voices.ToDictionary(a => a.Key, a => new SoundPool(a.Value)));
			NotificationsPools = Exts.Lazy(() => Notifications.ToDictionary( a => a.Key, a => new SoundPool(a.Value) ));
		}
	}

	public class SoundPool
	{
		readonly string[] clips;
		readonly List<string> liveclips = new List<string>();

		public SoundPool(params string[] clips)
		{
			this.clips = clips;
		}

		public string GetNext()
		{
			if (liveclips.Count == 0)
				liveclips.AddRange(clips);

			if (liveclips.Count == 0)
				return null;		/* avoid crashing if there's no clips at all */

			var i = Game.CosmeticRandom.Next(liveclips.Count);
			var s = liveclips[i];
			liveclips.RemoveAt(i);
			return s;
		}
	}
}
