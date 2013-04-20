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
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public Dictionary<string, Map> AvailableMaps { get; private set; }
		public readonly WidgetLoader WidgetLoader;
		public ILoadScreen LoadScreen = null;
		public SheetBuilder SheetBuilder;
		public SpriteLoader SpriteLoader;

		public ModData( params string[] mods )
		{
			Manifest = new Manifest( mods );
			ObjectCreator = new ObjectCreator( Manifest );
			LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
			LoadScreen.Init(Manifest.LoadScreen.NodesDict.ToDictionary(x => x.Key, x => x.Value.Value));
			LoadScreen.Display();
			WidgetLoader = new WidgetLoader( this );
		}

		public void LoadInitialAssets()
		{
			// all this manipulation of static crap here is nasty and breaks
			// horribly when you use ModData in unexpected ways.

			FileSystem.UnmountAll();
			foreach (var dir in Manifest.Folders)
				FileSystem.Mount(dir);

			AvailableMaps = FindMaps(Manifest.Mods);

			ChromeMetrics.Initialize(Manifest.ChromeMetrics);
			ChromeProvider.Initialize(Manifest.Chrome);
			SheetBuilder = new SheetBuilder(TextureChannel.Red);
			SpriteLoader = new SpriteLoader(new string[] { ".shp" }, SheetBuilder);
			CursorProvider.Initialize(Manifest.Cursors);
		}

		public Map PrepareMap(string uid)
		{
			LoadScreen.Display();
			if (!AvailableMaps.ContainsKey(uid))
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));
			var map = new Map(AvailableMaps[uid].Path);

			// Reinit all our assets
			LoadInitialAssets();
			foreach (var pkg in Manifest.Packages)
				FileSystem.Mount(pkg);

			// Mount map package so custom assets can be used. TODO: check priority.
			FileSystem.Mount(FileSystem.OpenPackage(map.Path, int.MaxValue));

			Rules.LoadRules(Manifest, map);
			SpriteLoader = new SpriteLoader(Rules.TileSets[map.Tileset].Extensions, SheetBuilder);
			// TODO: Don't load the sequences for assets that are not used in this tileset. Maybe use the existing EditorTilesetFilters.
			SequenceProvider.Initialize(Manifest.Sequences, map.Sequences);

			return map;
		}

		public static IEnumerable<string> FindMapsIn(string dir)
		{
			string[] NoMaps = { };

			if (!Directory.Exists(dir))
				return NoMaps;

			return Directory.GetDirectories(dir)
				.Concat(Directory.GetFiles(dir, "*.zip"))
				.Concat(Directory.GetFiles(dir, "*.oramap"));
		}

		Dictionary<string, Map> FindMaps(string[] mods)
		{
			var paths = mods.SelectMany(p => FindMapsIn("mods{0}{1}{0}maps{0}".F(Path.DirectorySeparatorChar, p)))
				.Concat(mods.SelectMany(p => FindMapsIn("{1}maps{0}{2}{0}".F(Path.DirectorySeparatorChar, Platform.SupportDir, p))));

			var ret = new Dictionary<string, Map>();

			foreach (var path in paths)
			{
				try
				{
					var map = new Map(path);
					ret.Add(map.Uid, map);
				}
				catch(Exception e)
				{
					Console.WriteLine("Failed to load map: {0}", path);
					Console.WriteLine("Details: {0}", e.ToString());
				}
			}

			return ret;
		}

		public Map FindMapByUid(string uid)
		{
			return AvailableMaps.ContainsKey(uid) ? AvailableMaps[uid] : null;
		}
	}

	public interface ILoadScreen
	{
		void Init(Dictionary<string, string> info);
		void Display();
		void StartGame();
	}
}
