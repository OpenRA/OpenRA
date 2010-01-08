using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using IjwFramework.Types;
using System.Collections;

namespace OpenRa.Game.GameRules
{
	class InfoLoader<T> : IEnumerable<KeyValuePair<string, T>>
	{
		readonly Dictionary<string, T> infos = new Dictionary<string, T>();

		public InfoLoader(params Pair<string, Func<string,T>>[] srcs)
		{
			foreach (var src in srcs)
				foreach (var name in Rules.Categories[src.First])
				{
					var t = src.Second(name);
					FieldLoader.Load(t, Rules.AllRules.GetSection(name));
					infos[name] = t;
				}
		}

		public T this[string name]
		{
			get { return infos[name.ToLowerInvariant()]; }
		}

		public IEnumerator<KeyValuePair<string, T>> GetEnumerator() { return infos.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return infos.GetEnumerator(); }
	}
}
