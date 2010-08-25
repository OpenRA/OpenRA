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
using OpenRA.Support;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public readonly SheetBuilder SheetBuilder;
		public readonly CursorSheetBuilder CursorSheetBuilder;
		public readonly Dictionary<string, MapStub> AvailableMaps;
		public ILoadScreen LoadScreen = null;
		
		public ModData( params string[] mods )
		{
			Manifest = new Manifest( mods );
			FileSystem.LoadFromManifest( Manifest );
			ChromeProvider.Initialize( Manifest.Chrome );
			
			ObjectCreator = new ObjectCreator( Manifest );

			LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen);
			LoadScreen.Display();
			
			SheetBuilder = new SheetBuilder( TextureChannel.Red );
			CursorSheetBuilder = new CursorSheetBuilder( this );
			AvailableMaps = FindMaps( mods );
		}
		
		// TODO: Do this nicer
		Dictionary<string, MapStub> FindMaps(string[] mods)
		{
			var paths = new[] { "maps/" }.Concat(mods.Select(m => "mods/" + m + "/maps/"))
				.Where(p => Directory.Exists(p))
				.SelectMany(p => Directory.GetDirectories(p)).ToList();

			return paths.Select(p => new MapStub(new Folder(p))).ToDictionary(m => m.Uid);
		}
		
		string cachedTheatre = null;
		public Map PrepareMap(string uid)
		{
			LoadScreen.Display();

			if (!AvailableMaps.ContainsKey(uid))
				throw new InvalidDataException("Invalid map uid: {0}".F(uid));
			
			Timer.Time("----PrepareMap");
			var map = new Map(AvailableMaps[uid].Package);
			Timer.Time( "Map: {0}" );
			
			Rules.LoadRules(Manifest, map);
			Timer.Time( "Rules: {0}" );

			if (map.Theater != cachedTheatre)
			{
				SpriteSheetBuilder.Initialize( Rules.TileSets[map.Tileset] );
				SequenceProvider.Initialize(Manifest.Sequences);
				Timer.Time("SSB, SeqProv: {0}");
				cachedTheatre = map.Theater;
			}
			Timer.Time("----end PrepareMap");
			return map;
		}
	}
	
	public interface ILoadScreen { void Display(); }
}
