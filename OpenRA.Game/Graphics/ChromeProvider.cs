#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	static class ChromeProvider
	{
		static Dictionary<string, Dictionary<string, MappedImage>> collections;
		static Dictionary<string, Sheet> cachedSheets;
		static Dictionary<string, Dictionary<string, Sprite>> cachedSprites;

		public static void Initialize(params string[] chromeFiles)
		{
			collections = new Dictionary<string, Dictionary<string, MappedImage>>();
			cachedSheets = new Dictionary<string, Sheet>();
			cachedSprites = new Dictionary<string, Dictionary<string, Sprite>>();

			foreach (var f in chromeFiles)
				LoadChromeSource(f);
		}

		static void LoadChromeSource(string filename)
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open(filename));
			foreach (XmlElement eCollection in document.SelectNodes("/chrome/collection"))
				LoadChromeForCollection(eCollection);
		}

		static void LoadChromeForCollection(XmlElement eCollection)
		{
			string elementName = eCollection.GetAttribute("name");
			string defaultSrc = (eCollection.HasAttribute("src") ? eCollection.GetAttribute("src") : null);

			var images = eCollection.SelectNodes("./image").OfType<XmlElement>()
				.Select(e => new MappedImage(defaultSrc, e))
				.ToDictionary(s => s.Name);

			collections.Add(elementName, images);
		}

		public static Sprite GetImage(Renderer renderer, string collection, string image)
		{
			// Cached sprite
			if (cachedSprites.ContainsKey(collection) && cachedSprites[collection].ContainsKey(image))
				return cachedSprites[collection][image];

			MappedImage mi;
			try { mi = collections[collection][image]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Collection `{0}` does not have an image `{1}`".F(collection, image));
			}

			// Cached sheet
			Sheet sheet;
			if (cachedSheets.ContainsKey(mi.Src))
				sheet = cachedSheets[mi.Src];
			else
			{
				sheet = new Sheet(renderer, mi.Src);
				cachedSheets.Add(mi.Src, sheet);
			}

			// Cache the sprite
			if (!cachedSprites.ContainsKey(collection))
				cachedSprites.Add(collection, new Dictionary<string, Sprite>());
			cachedSprites[collection].Add(image, mi.GetImage(renderer, sheet));

			return cachedSprites[collection][image];
		}
	}
}
