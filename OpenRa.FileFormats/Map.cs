using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Linq;

namespace OpenRa.FileFormats
{
	public class Map
	{
		public readonly string Title;
		public readonly string Theater;

		public readonly int XOffset;
		public readonly int YOffset;
		public int2 Offset { get { return new int2( XOffset, YOffset ); } }

		public readonly int Width;
		public readonly int Height;
		public int2 Size { get { return new int2(Width, Height); } }

		public readonly TileReference[ , ] MapTiles = new TileReference[ 128, 128 ];
		public readonly List<TreeReference> Trees = new List<TreeReference>();

		public static bool[] overlayIsFence =
			{
				true, true, true, true, true,
				false, false, false, false,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, true, true,
			};

		public static bool[] overlayIsOre =
			{
				false, false, false, false, false,
				true, true, true, true,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};

		public static bool[] overlayIsGems =
			{
				false, false, false, false, false,
				false, false, false, false,
				true, true, true, true,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};

		static string Truncate( string s, int maxLength )
		{
			return s.Length <= maxLength ? s : s.Substring(0,maxLength );
		}

		public string TileSuffix { get { return "." + Truncate(Theater, 3); } }

		public Map(IniFile file)
		{
			for (int j = 0; j < 128; j++)
				for (int i = 0; i < 128; i++)
					MapTiles[i, j] = new TileReference();

			IniSection basic = file.GetSection("Basic");
			Title = basic.GetValue("Name", "(null)");

			IniSection map = file.GetSection("Map");
			Theater = Truncate(map.GetValue("Theater", "TEMPERATE"), 8);

			XOffset = int.Parse(map.GetValue("X", "0"));
			YOffset = int.Parse(map.GetValue("Y", "0"));

			Width = int.Parse(map.GetValue("Width", "0"));
			Height = int.Parse(map.GetValue("Height", "0"));

			UnpackTileData(ReadPackedSection(file.GetSection("MapPack")));
			UnpackOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
			ReadTrees(file);

			InitOreDensity();
		}

		IEnumerable<int2> AdjacentTiles(int2 p)
		{
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					yield return new int2(u, v) + p;
		}

		byte GetOreDensity(int i, int j)
		{
			return (byte)Math.Min(11, (3 * AdjacentTiles(new int2(i, j)).Sum(
							p => ContainsOre(p.X, p.Y) ? 1 : 0) / 2));
		}

		byte GetGemDensity(int i, int j)
		{
			return (byte)Math.Min(2, (AdjacentTiles(new int2(i, j)).Sum(
							p => ContainsGem(p.X, p.Y) ? 1 : 0) / 3));
		}

		void InitOreDensity()
		{
			for( int j = 0; j < 128; j++ )
				for (int i = 0; i < 128; i++)
				{
					if (ContainsOre(i,j)) MapTiles[i, j].density = GetOreDensity(i, j);
					if (ContainsGem(i,j)) MapTiles[i,j].density = GetGemDensity(i,j);
				}
		}

		bool HasOverlay(int i, int j)
		{
			return MapTiles[i, j].overlay < overlayIsOre.Length;
		}

		bool ContainsOre(int i, int j)
		{
			return HasOverlay(i,j) && overlayIsOre[MapTiles[i,j].overlay];
		}

		bool ContainsGem(int i, int j)
		{
			return HasOverlay(i, j) && overlayIsGems[MapTiles[i, j].overlay];
		}

		public void GrowOre( Func<int2, bool> canSpreadIntoCell )				/* todo: deal with ore pits */
		{
			/* phase 1: grow into neighboring regions */
			var newOverlay = new byte[128, 128];
			for( int j = 1; j < 127; j++ )
				for (int i = 1; i < 127; i++)
				{
					newOverlay[i,j] = 0xff;
					if (!HasOverlay(i, j) && GetOreDensity(i, j) > 0 && canSpreadIntoCell(new int2(i,j)))
						newOverlay[i, j] = 5;	/* todo: randomize [5..8] */

					if (!HasOverlay(i, j) && GetGemDensity(i, j) > 0 && canSpreadIntoCell(new int2(i, j)))
						newOverlay[i, j] = 9;	/* todo: randomize [9..12] */
				}

			for (int j = 1; j < 127; j++)
				for (int i = 1; i < 127; i++)
					if (newOverlay[i, j] != 0xff)
						MapTiles[i, j].overlay = newOverlay[i, j];

			/* phase 2: increase density of existing areas */
			var newDensity = new byte[128, 128];
			for (int j = 1; j < 127; j++)
				for (int i = 1; i < 127; i++)
				{
					if (ContainsOre(i, j)) newDensity[i,j] = GetOreDensity(i, j);
					if (ContainsGem(i, j)) newDensity[i,j] = GetGemDensity(i, j);
				}

			for (int j = 1; j < 127; j++)
				for (int i = 1; i < 127; i++)
					if (MapTiles[i, j].density < newDensity[i, j])
						MapTiles[i, j].density = newDensity[i, j];
		}

		static MemoryStream ReadPackedSection(IniSection mapPackSection)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 1; ; i++)
			{
				string line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			byte[] data = Convert.FromBase64String(sb.ToString());
			List<byte[]> chunks = new List<byte[]>();
			BinaryReader reader = new BinaryReader(new MemoryStream(data));

			try
			{
				while (true)
				{
					uint length = reader.ReadUInt32() & 0xdfffffff;
					byte[] dest = new byte[8192];
					byte[] src = reader.ReadBytes((int)length);

					int actualLength = Format80.DecodeInto(src, dest);

					chunks.Add(dest);
				}
			}
			catch (EndOfStreamException) { }

			MemoryStream ms = new MemoryStream();
			foreach (byte[] chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			return ms;
		}

		static byte ReadByte( Stream s )
		{
			int ret = s.ReadByte();
			if( ret == -1 )
				throw new NotImplementedException();
			return (byte)ret;
		}

		static ushort ReadWord(Stream s)
		{
			ushort ret = ReadByte(s);
			ret |= (ushort)(ReadByte(s) << 8);

			return ret;
		}

		void UnpackTileData( MemoryStream ms )
		{
			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
					MapTiles[j, i].tile = ReadWord(ms);

			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
				{
                    MapTiles[j, i].image = (byte)ms.ReadByte();
					if( MapTiles[ j, i ].tile == 0xff || MapTiles[ j, i ].tile == 0xffff )
						MapTiles[ j, i ].image = (byte)( i % 4 + ( j % 4 ) * 4 );
				}
		}

		void UnpackOverlayData( MemoryStream ms )
		{
			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
					MapTiles[ j, i ].overlay = ReadByte( ms );
		}

		void ReadTrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN" );
			if( terrain == null )
				return;

			foreach( KeyValuePair<string, string> kv in terrain )
				Trees.Add( new TreeReference( int.Parse( kv.Key ), kv.Value ) );
		}

		public bool IsInMap(int x, int y)
		{
			return (x >= XOffset && y >= YOffset && x < XOffset + Width && y < YOffset + Height);
		}
	}
}
