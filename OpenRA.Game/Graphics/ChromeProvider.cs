#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	[Flags]
	public enum PanelSides
	{
		Left = 1,
		Top = 2,
		Right = 4,
		Bottom = 8,
		Center = 16,

		Edges = Left | Top | Right | Bottom,
		All = Edges | Center,
	}

	public static class PanelSidesExts
	{
		public static bool HasSide(this PanelSides self, PanelSides m)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (self & m) == m;
		}
	}

	public static class ChromeProvider
	{
		public class Collection
		{
			public readonly string Image = null;
			public readonly string Image2x = null;
			public readonly string Image3x = null;

			public readonly int[] PanelRegion = null;
			public readonly PanelSides PanelSides = PanelSides.All;
			public readonly Dictionary<string, Rectangle> Regions = new Dictionary<string, Rectangle>();
		}

		public static IReadOnlyDictionary<string, Collection> Collections => collections;
		static Dictionary<string, Collection> collections;
		static Dictionary<string, (Sheet Sheet, int Density)> cachedSheets;
		static Dictionary<string, Dictionary<string, Sprite>> cachedSprites;
		static Dictionary<string, Sprite[]> cachedPanelSprites;
		static Dictionary<Collection, (Sheet Sheet, int)> cachedCollectionSheets;

		static IReadOnlyFileSystem fileSystem;
		static float dpiScale = 1;

		public static void Initialize(ModData modData)
		{
			Deinitialize();

			// Load higher resolution images if available on HiDPI displays
			if (Game.Renderer != null)
				dpiScale = Game.Renderer.WindowScale;

			fileSystem = modData.DefaultFileSystem;
			collections = new Dictionary<string, Collection>();
			cachedSheets = new Dictionary<string, (Sheet, int)>();
			cachedSprites = new Dictionary<string, Dictionary<string, Sprite>>();
			cachedPanelSprites = new Dictionary<string, Sprite[]>();
			cachedCollectionSheets = new Dictionary<Collection, (Sheet, int)>();

			var chrome = MiniYaml.Merge(modData.Manifest.Chrome
				.Select(s => MiniYaml.FromStream(fileSystem.Open(s), s)));

			foreach (var c in chrome)
				if (!c.Key.StartsWith("^", StringComparison.Ordinal))
					LoadCollection(c.Key, c.Value);
		}

		public static void Deinitialize()
		{
			if (cachedSheets != null)
				foreach (var sheet in cachedSheets.Values)
					sheet.Sheet.Dispose();

			collections = null;
			cachedSheets = null;
			cachedSprites = null;
			cachedPanelSprites = null;
			cachedCollectionSheets = null;
		}

		static void LoadCollection(string name, MiniYaml yaml)
		{
			if (Game.ModData.LoadScreen != null)
				Game.ModData.LoadScreen.Display();

			collections.Add(name, FieldLoader.Load<Collection>(yaml));
		}

		static (Sheet Sheet, int Density) SheetForCollection(Collection c)
		{
			// Outer cache avoids recalculating image names
			if (!cachedCollectionSheets.TryGetValue(c, out (Sheet, int) sheetDensity))
			{
				var image = c.Image;
				var density = 1;
				if (dpiScale > 2 && !string.IsNullOrEmpty(c.Image3x))
				{
					image = c.Image3x;
					density = 3;
				}
				else if (dpiScale > 1 && !string.IsNullOrEmpty(c.Image2x))
				{
					image = c.Image2x;
					density = 2;
				}

				// Inner cache makes sure we share sheets between collections
				if (!cachedSheets.TryGetValue(image, out sheetDensity))
				{
					Sheet sheet;
					using (var stream = fileSystem.Open(image))
						sheet = new Sheet(SheetType.BGRA, stream);

					sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;

					sheetDensity = (sheet, density);
					cachedSheets.Add(image, sheetDensity);
				}

				cachedCollectionSheets.Add(c, sheetDensity);
			}

			return sheetDensity;
		}

		public static Sprite GetImage(string collectionName, string imageName)
		{
			var image = TryGetImage(collectionName, imageName);
			if (image == null)
				throw new ArgumentException($"Sprite `{collectionName}/{imageName}` was not found.");

			return image;
		}

		public static Sprite TryGetImage(string collectionName, string imageName)
		{
			if (string.IsNullOrEmpty(collectionName))
				return null;

			// Cached sprite
			if (cachedSprites.TryGetValue(collectionName, out var cachedCollection) && cachedCollection.TryGetValue(imageName, out var sprite))
				return sprite;

			if (!collections.TryGetValue(collectionName, out var collection))
				return null;

			if (!collection.Regions.TryGetValue(imageName, out var mi))
				return null;

			// Cache the sprite
			var sheetDensity = SheetForCollection(collection);
			if (cachedCollection == null)
			{
				cachedCollection = new Dictionary<string, Sprite>();
				cachedSprites.Add(collectionName, cachedCollection);
			}

			var image = new Sprite(sheetDensity.Sheet, sheetDensity.Density * mi, TextureChannel.RGBA, 1f / sheetDensity.Density);
			cachedCollection.Add(imageName, image);

			return image;
		}

		public static Sprite[] GetPanelImages(string collectionName)
		{
			var panel = TryGetPanelImages(collectionName);
			if (panel == null)
				throw new ArgumentException($"Panel `{collectionName}` was not found.");

			return panel;
		}

		public static Sprite[] TryGetPanelImages(string collectionName)
		{
			if (string.IsNullOrEmpty(collectionName))
				return null;

			// Cached sprite
			if (cachedPanelSprites.TryGetValue(collectionName, out var cachedSprites))
				return cachedSprites;

			if (!collections.TryGetValue(collectionName, out var collection))
				return null;

			Sprite[] sprites;
			if (collection.PanelRegion != null)
			{
				if (collection.PanelRegion.Length != 8)
				{
					Log.Write("debug", $"Collection '{collectionName}' does not define a valid PanelRegion");
					return null;
				}

				// Cache the sprites
				var sheetDensity = SheetForCollection(collection);
				var pr = collection.PanelRegion;
				var ps = collection.PanelSides;

				var sides = new (PanelSides PanelSides, Rectangle Bounds)[]
				{
					(PanelSides.Top | PanelSides.Left, new Rectangle(pr[0], pr[1], pr[2], pr[3])),
					(PanelSides.Top, new Rectangle(pr[0] + pr[2], pr[1], pr[4], pr[3])),
					(PanelSides.Top | PanelSides.Right, new Rectangle(pr[0] + pr[2] + pr[4], pr[1], pr[6], pr[3])),
					(PanelSides.Left, new Rectangle(pr[0], pr[1] + pr[3], pr[2], pr[5])),
					(PanelSides.Center, new Rectangle(pr[0] + pr[2], pr[1] + pr[3], pr[4], pr[5])),
					(PanelSides.Right, new Rectangle(pr[0] + pr[2] + pr[4], pr[1] + pr[3], pr[6], pr[5])),
					(PanelSides.Bottom | PanelSides.Left, new Rectangle(pr[0], pr[1] + pr[3] + pr[5], pr[2], pr[7])),
					(PanelSides.Bottom, new Rectangle(pr[0] + pr[2], pr[1] + pr[3] + pr[5], pr[4], pr[7])),
					(PanelSides.Bottom | PanelSides.Right, new Rectangle(pr[0] + pr[2] + pr[4], pr[1] + pr[3] + pr[5], pr[6], pr[7]))
				};

				sprites = sides.Select(x => ps.HasSide(x.PanelSides) ? new Sprite(sheetDensity.Sheet, sheetDensity.Density * x.Bounds, TextureChannel.RGBA, 1f / sheetDensity.Density) : null)
					.ToArray();
			}
			else
			{
				// PERF: We don't need to search for images if there are no definitions.
				// PERF: It's more efficient to send an empty array rather than an array of 9 nulls.
				if (!collection.Regions.Any())
					return Array.Empty<Sprite>();

				// Support manual definitions for unusual dialog layouts
				sprites = new[]
				{
					TryGetImage(collectionName, "corner-tl"),
					TryGetImage(collectionName, "border-t"),
					TryGetImage(collectionName, "corner-tr"),
					TryGetImage(collectionName, "border-l"),
					TryGetImage(collectionName, "background"),
					TryGetImage(collectionName, "border-r"),
					TryGetImage(collectionName, "corner-bl"),
					TryGetImage(collectionName, "border-b"),
					TryGetImage(collectionName, "corner-br")
				};
			}

			cachedPanelSprites.Add(collectionName, sprites);
			return sprites;
		}

		public static Size GetMinimumPanelSize(string collectionName)
		{
			if (string.IsNullOrEmpty(collectionName))
				return new Size(0, 0);

			if (!collections.TryGetValue(collectionName, out var collection))
			{
				Log.Write("debug", "Could not find collection '{0}'", collectionName);
				return new Size(0, 0);
			}

			if (collection.PanelRegion == null || collection.PanelRegion.Length != 8)
			{
				Log.Write("debug", "Collection '{0}' does not define a valid PanelRegion", collectionName);
				return new Size(0, 0);
			}

			var pr = collection.PanelRegion;
			return new Size(pr[2] + pr[6], pr[3] + pr[7]);
		}

		public static void SetDPIScale(float scale)
		{
			if (dpiScale == scale)
				return;

			dpiScale = scale;

			// Clear the sprite caches so the new artwork can be loaded
			// Sheets are not cleared: we assume that the extra memory overhead
			// of having the same sheet in memory in multiple DPIs is better than
			// the overhead of having to dispose and reload everything.
			// Changing the DPI scale is rare, but if it does happen then there
			// is a reasonable chance that it may happen again this session.
			cachedSprites.Clear();
			cachedPanelSprites.Clear();
			cachedCollectionSheets.Clear();
		}
	}
}
