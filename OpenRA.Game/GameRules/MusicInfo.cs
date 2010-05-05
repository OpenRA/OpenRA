
using System;

using System.Collections.Generic;
using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly Lazy<Dictionary<string, MusicPool>> Pools;
		public readonly string[] Music = { };

		public MusicInfo( MiniYaml y )
		{
			FieldLoader.Load(this, y);

			Pools = Lazy.New(() =>
				new Dictionary<string, MusicPool>
				{
					{ "Music", new MusicPool(Music) },
				});
		}
	}
	
	public class MusicPool
	{
		readonly string[] clips;
		readonly List<string> liveclips = new List<string>();

		public MusicPool(params string[] clips)
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
