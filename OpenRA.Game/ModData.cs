#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
		public Dictionary<string, Map> AvailableMaps {get; private set;}
		public readonly WidgetLoader WidgetLoader;
		public ILoadScreen LoadScreen = null;
		public SheetBuilder SheetBuilder;
		public CursorSheetBuilder CursorSheetBuilder;
		public SpriteLoader SpriteLoader;
		public HardwarePalette Palette { get; private set; }
		IFolder previousMapMount = null;
		
		public ModData( params string[] mods )
		{		
			Manifest = new Manifest( mods );
			ObjectCreator = new ObjectCreator( Manifest );
			LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
			LoadScreen.Init(Manifest.LoadScreen.NodesDict.ToDictionary(x => x.Key, x => x.Value.Value));
			LoadScreen.Display();
			WidgetLoader = new WidgetLoader( this );
		}
		
		public void ReloadMaps()
		{
			AvailableMaps = FindMaps( Manifest.Mods );
		}

		public void LoadInitialAssets()
		{
			// all this manipulation of static crap here is nasty and breaks 
			// horribly when you use ModData in unexpected ways.
			FileSystem.UnmountAll();
			foreach (var dir in Manifest.Folders)
				FileSystem.Mount(dir);

			ReloadMaps();
			Palette = new HardwarePalette();
			ChromeMetrics.Initialize(Manifest.ChromeMetrics);
			ChromeProvider.Initialize(Manifest.Chrome);
			SheetBuilder = new SheetBuilder(TextureChannel.Red);
			CursorSheetBuilder = new CursorSheetBuilder(this);
			CursorProvider.Initialize(Manifest.Cursors);
			Palette.Update(new IPaletteModifier[] { });
		}

		public Map PrepareMap(string uid)
		{
			LoadScreen.Display();
			if (!AvailableMaps.ContainsKey(uid))
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));
			var map = new Map(AvailableMaps[uid].Path);

			// Maps may contain custom assets
			// TODO: why are they lowest priority? they should be highest.
			if (previousMapMount != null) FileSystem.Unmount(previousMapMount);
			previousMapMount = FileSystem.OpenPackage(map.Path, int.MaxValue);
			FileSystem.Mount(previousMapMount);
			
			// Reinit all our assets
			LoadInitialAssets();
			foreach (var pkg in Manifest.Packages)
				FileSystem.Mount(pkg);
		
			Rules.LoadRules(Manifest, map);
			SpriteLoader = new SpriteLoader( Rules.TileSets[map.Tileset].Extensions, SheetBuilder );
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
			
			Dictionary<string, Map> ret = new Dictionary<string, Map>();
			foreach (var path in paths)
			{
				var map = new Map(path);
				if (ret.ContainsKey(map.Uid))
					System.Console.WriteLine("Ignoring duplicate map: {0}", path);
				else
					ret.Add(map.Uid, map);
			}
			return ret;
		}
		
	}
	
	public interface ILoadScreen
	{
		void Init(Dictionary<string, string> info);
		void Display();
		void StartGame();
	}
}
