#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public readonly Dictionary<string, MapStub> AvailableMaps;
		public readonly WidgetLoader WidgetLoader;
		public ILoadScreen LoadScreen = null;
		public SheetBuilder SheetBuilder;
		public CursorSheetBuilder CursorSheetBuilder;

		public ModData( params string[] mods )
		{		
			Manifest = new Manifest( mods );
			ObjectCreator = new ObjectCreator( Manifest );
			LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen);
			LoadScreen.Init();
			LoadScreen.Display();
			
            // all this manipulation of static crap here is nasty and breaks 
            // horribly when you use ModData in unexpected ways.

			FileSystem.LoadFromManifest( Manifest );
			ChromeProvider.Initialize( Manifest.Chrome );
			SheetBuilder = new SheetBuilder( TextureChannel.Red );
			CursorSheetBuilder = new CursorSheetBuilder( this );
			AvailableMaps = FindMaps( mods );
			WidgetLoader = new WidgetLoader( this );
		}
		
        public static IEnumerable<string> FindMapsIn(string dir)
        {
            string[] NoMaps = { };

            if (!Directory.Exists(dir))
                return NoMaps;

            // todo: look for compressed maps too.
            return Directory.GetDirectories(dir)
                .Concat(Directory.GetFiles(dir, "*.zip"))
                .Concat(Directory.GetFiles(dir, "*.oramap"));
        }

		Dictionary<string, MapStub> FindMaps(string[] mods)
		{
            var paths = mods.SelectMany(p => FindMapsIn("mods{0}{1}{0}maps{0}".F(Path.DirectorySeparatorChar, p)))
				.Concat(mods.SelectMany(p => FindMapsIn("{1}maps{0}{2}{0}".F(Path.DirectorySeparatorChar, Game.SupportDir, p))));
			
			Dictionary<string, MapStub> ret = new Dictionary<string, MapStub>();
			foreach (var path in paths)
			{
				var map = new MapStub(path);
				if (ret.ContainsKey(map.Uid))
					System.Console.WriteLine("Ignoring duplicate map: {0}", path);
				else
					ret.Add(map.Uid, map);
			}
			return ret;
		}
		
		string cachedTileset = null;
		bool previousMapHadSequences = true;
		IFolder previousMapMount = null;

		public Map PrepareMap(string uid)
		{
			LoadScreen.Display();

			if (!AvailableMaps.ContainsKey(uid))
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));
			
			var map = new Map(AvailableMaps[uid].Path);

			// unload the previous map mount if we have one
			if (previousMapMount != null) FileSystem.Unmount(previousMapMount);

			// Adds the map its container to the FileSystem
			// allowing the map to use custom assets
			// Container should have the lowest priority of all (ie int max)
			// Store a reference so we can unload it next time
			previousMapMount = FileSystem.OpenPackage(map.Path, int.MaxValue);
			FileSystem.Mount(previousMapMount);
			Rules.LoadRules(Manifest, map);

			if (map.Tileset != cachedTileset
				|| previousMapHadSequences || map.Sequences.Count > 0)
			{
				SheetBuilder = new SheetBuilder( TextureChannel.Red );
				SpriteLoader.Initialize( Rules.TileSets[map.Tileset], SheetBuilder );
				CursorSheetBuilder = new CursorSheetBuilder( this );
				CursorProvider.Initialize(Manifest.Cursors);
				SequenceProvider.Initialize(Manifest.Sequences, map.Sequences);
				cachedTileset = map.Tileset;
			}

			previousMapHadSequences = map.Sequences.Count > 0;

			return map;
		}
	}
	
	public interface ILoadScreen { void Display(); void Init(); }
}
