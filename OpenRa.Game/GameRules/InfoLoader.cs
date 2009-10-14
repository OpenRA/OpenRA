using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.GameRules
{
	class InfoLoader<T>
	{
		readonly Dictionary<string, T> infos = new Dictionary<string, T>();

		public InfoLoader(IniFile rules, params Pair<string, Func<string,T>>[] srcs)
		{
			foreach (var src in srcs)
				foreach (var s in Util.ReadAllLines(FileSystem.Open(src.First)))
				{
					var name = s.Split(',')[0];
					var t = src.Second(name.ToLowerInvariant());
					FieldLoader.Load(t, rules.GetSection(name));
					infos[name.ToLowerInvariant()] = t;
				}
		}

		public T this[string name]
		{
			get { return infos[name.ToLowerInvariant()]; }
		}
	}
}
