#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Graphics
{
	public static class ChromeProvider
	{
		struct Collection
		{
			public string src;
			public Dictionary<string, MappedImage> regions;
		}

		static Dictionary<string, Collection> collections;
		static Dictionary<string, Sheet> cachedSheets;
		static Dictionary<string, Dictionary<string, Sprite>> cachedSprites;

		public static void Initialize(IEnumerable<string> chromeFiles)
		{
			collections = new Dictionary<string, Collection>();
			cachedSheets = new Dictionary<string, Sheet>();
			cachedSprites = new Dictionary<string, Dictionary<string, Sprite>>();

			var chrome = chromeFiles.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal);

			foreach (var c in chrome)
				LoadCollection(c.Key, c.Value);
		}

		public static void Save(string file)
		{
			var root = new List<MiniYamlNode>();
			foreach (var kv in collections)
				root.Add(new MiniYamlNode(kv.Key, SaveCollection(kv.Value)));

			root.WriteToFile(file);
		}

		static MiniYaml SaveCollection(Collection collection)
		{
			var root = new List<MiniYamlNode>();
			foreach (var kv in collection.regions)
				root.Add(new MiniYamlNode(kv.Key, kv.Value.Save(collection.src)));

			return new MiniYaml(collection.src, root);
		}

		static void LoadCollection(string name, MiniYaml yaml)
		{
			Game.modData.LoadScreen.Display();
			var collection = new Collection()
			{
				src = yaml.Value,
				regions = yaml.Nodes.ToDictionary(n => n.Key, n => new MappedImage(yaml.Value, n.Value))
			};

			collections.Add(name, collection);
		}

		public static Sprite GetImage(string collectionName, string imageName)
		{
			// Cached sprite
			Dictionary<string, Sprite> cachedCollection;
			Sprite sprite;
			if (cachedSprites.TryGetValue(collectionName, out cachedCollection) && cachedCollection.TryGetValue(imageName, out sprite))
				return sprite;

			Collection collection;
			if (!collections.TryGetValue(collectionName, out collection))
			{
				Log.Write("debug", "Could not find collection '{0}'", collectionName);
				return null;
			}

			MappedImage mi;
			if (!collection.regions.TryGetValue(imageName, out mi))
				return null;

			// Cached sheet
			Sheet sheet;
			if (cachedSheets.ContainsKey(mi.src))
				sheet = cachedSheets[mi.src];
			else
			{
				sheet = new Sheet(mi.src);
				cachedSheets.Add(mi.src, sheet);
			}

			// Cache the sprite
			if (cachedCollection == null)
			{
				cachedCollection = new Dictionary<string, Sprite>();
				cachedSprites.Add(collectionName, cachedCollection);
			}
			var image = mi.GetImage(sheet);
			cachedCollection.Add(imageName, image);

			return image;
		}
	}
}
