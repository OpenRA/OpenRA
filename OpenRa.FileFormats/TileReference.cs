using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.FileFormats
{
	public struct TileReference
	{
		public ushort tile;
		public byte image;
		public byte overlay;
		public byte smudge;
		public byte density;	/* used for ore/gems */

		public override int GetHashCode() { return tile.GetHashCode() ^ image.GetHashCode(); }

		public override bool Equals( object obj )
		{
			if( obj == null )
				return false;

			TileReference r = (TileReference)obj;
			return ( r.image == image && r.tile == tile );
		}

		public static bool operator ==( TileReference a, TileReference b ) { return a.Equals( b ); }
		public static bool operator !=( TileReference a, TileReference b ) { return !a.Equals( b ); }
	}
}
