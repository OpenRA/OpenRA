using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.GameRules
{
	class WarheadInfoLoader
	{
		readonly Dictionary<string, WarheadInfo> warheadInfos = new Dictionary<string, WarheadInfo>();

		public WarheadInfoLoader(IniFile rules)
		{
			foreach (var s in Util.ReadAllLines(FileSystem.Open("warheads.txt")))
			{
				var unitName = s.Split(',')[0];
				warheadInfos.Add(unitName.ToLowerInvariant(),
					new WarheadInfo(rules.GetSection(unitName)));
			}
		}

		public WarheadInfo this[string unitName]
		{
			get
			{
				return warheadInfos[unitName.ToLowerInvariant()];
			}
		}
	}

	class WarheadInfo
	{
		public readonly int Spread = 1;
		public readonly string Verses = "100%,100%,100%,100%,100%";
		public readonly bool Wall = false;
		public readonly bool Wood = false;
		public readonly bool Ore = false;
		public readonly int Explosion = 0;
		public readonly int InfDeath = 0;

		public WarheadInfo(IniSection ini)
		{
			FieldLoader.Load(this, ini);
		}
	}
}
