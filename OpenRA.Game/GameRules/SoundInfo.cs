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
			var nd = y.ToDictionary();
			return nd.ContainsKey(name)
				? nd[name].ToDictionary(my => FieldLoader.GetValue<string[]>("(value)", my.Value))
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
		public readonly string sub;
		readonly List<string> liveclips = new List<string>();

		public SoundPool(params string[] clips)
		{
			var hackyclips = clips.ToList<string>();

			foreach (var c in hackyclips)
			{
				if (c.StartsWith("\""))
				{
					sub = c;
					break;
				}
			}
			hackyclips.Remove(sub);
			this.clips = hackyclips.ToArray<String>();
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
