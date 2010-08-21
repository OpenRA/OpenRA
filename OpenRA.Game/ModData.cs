using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using System.IO;
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
		
		public ModData( params string[] mods )
		{
			Manifest = new Manifest( mods );
			ObjectCreator = new ObjectCreator( Manifest );
			FileSystem.LoadFromManifest( Manifest );
			SheetBuilder = new SheetBuilder( TextureChannel.Red );
			CursorSheetBuilder = new CursorSheetBuilder( this );
			
			ChromeProvider.Initialize( Manifest.Chrome );
			
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
}
