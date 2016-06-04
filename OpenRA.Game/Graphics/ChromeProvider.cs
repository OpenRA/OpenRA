#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Graphics
{
	public static class ChromeProvider
	{
		struct Collection
		{
			public string Src;
			public Dictionary<string, MappedImage> Regions;
		}

		static Dictionary<string, Collection> collections;
		static Dictionary<string, Sheet> cachedSheets;
		static Dictionary<string, Dictionary<string, Sprite>> cachedSprites;
		static IReadOnlyFileSystem fileSystem;

		public static void Initialize(ModData modData)
		{
			Deinitialize();

			fileSystem = modData.DefaultFileSystem;
			collections = new Dictionary<string, Collection>();
			cachedSheets = new Dictionary<string, Sheet>();
			cachedSprites = new Dictionary<string, Dictionary<string, Sprite>>();

			var chrome = MiniYaml.Merge(modData.Manifest.Chrome
				.Select(s => MiniYaml.FromStream(fileSystem.Open(s), s)));

			foreach (var c in chrome)
				LoadCollection(c.Key, c.Value);
		}

		public static void Deinitialize()
		{
			if (cachedSheets != null)
				foreach (var sheet in cachedSheets.Values)
					sheet.Dispose();

			collections = null;
			cachedSheets = null;
			cachedSprites = null;
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
			foreach (var kv in collection.Regions)
				root.Add(new MiniYamlNode(kv.Key, kv.Value.Save(collection.Src)));

			return new MiniYaml(collection.Src, root);
		}

		static void LoadCollection(string name, MiniYaml yaml)
		{
			if (Game.ModData.LoadScreen != null)
				Game.ModData.LoadScreen.Display();
			var collection = new Collection()
			{
				Src = yaml.Value,
				Regions = yaml.Nodes.ToDictionary(n => n.Key, n => new MappedImage(yaml.Value, n.Value))
			};

			collections.Add(name, collection);
		}

		public static Sprite GetImage(string collectionName, string imageName)
		{
			if (string.IsNullOrEmpty(collectionName))
				return null;

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
			if (!collection.Regions.TryGetValue(imageName, out mi))
				return null;

			// Cached sheet
			Sheet sheet;
			if (cachedSheets.ContainsKey(mi.Src))
				sheet = cachedSheets[mi.Src];
			else
			{
				using (var stream = fileSystem.Open(mi.Src))
					sheet = new Sheet(SheetType.BGRA, stream);

				cachedSheets.Add(mi.Src, sheet);
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
