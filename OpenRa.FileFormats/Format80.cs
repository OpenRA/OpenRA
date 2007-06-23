using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.FileFormats
{
	public static class Format80
	{
		static byte ReadByte( MemoryStream input )
		{
			int inp = input.ReadByte();
			if( inp == -1 )
				throw new InvalidDataException();

			return (byte)inp;
		}

		static int ReadWord( MemoryStream input )
		{
			int inp = ReadByte( input );
			return inp + ( ReadByte( input ) << 8 );
		}

		static void ReplicatePrevious( byte[] dest, int destIndex, int srcIndex, int count )
		{
			if( srcIndex >= destIndex )
				throw new NotImplementedException( string.Format( "srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex ) );

			for( int i = 0 ; i < Math.Min( count, destIndex - srcIndex ) ; i++ )
				dest[ destIndex + i ] = dest[ srcIndex + i ];

			if( srcIndex + count <= destIndex )
				return;

			for( int i = destIndex + destIndex - srcIndex ; i < destIndex + count ; i++ )
				dest[ i ] = dest[ destIndex - 1 ];
		}

		public static int DecodeInto( MemoryStream input, byte[] dest )
		{
			int destIndex = 0;
			while( true )
			{
				byte i = ReadByte( input );
				if( ( i & 0x80 ) == 0 )
				{
					// case 2
					byte secondByte = ReadByte( input );
					int count = ( ( i & 0x70 ) >> 4 ) + 3;
					int rpos = ( ( i & 0xf ) << 8 ) + secondByte;

					ReplicatePrevious( dest, destIndex, destIndex - rpos, count );
					destIndex += count;
				}
				else if( ( i & 0x40 ) == 0 )
				{
					// case 1
					int count = i & 0x3F;
					if( count == 0 )
						return destIndex;

					input.Read( dest, destIndex, count );
					destIndex += count;
				}
				else
				{
					int count3 = i & 0x3F;
					if( count3 == 0x3E )
					{
						// case 4
						int count = ReadWord( input );
						byte color = ReadByte( input );

						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] = color;
					}
					else if( count3 == 0x3F )
					{
						// case 5
						int count = ReadWord( input );
						int srcIndex = ReadWord( input );
						if( srcIndex >= destIndex )
							throw new NotImplementedException( string.Format( "srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex ) );

						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] = dest[ srcIndex++ ];
					}
					else
					{
						// case 3
						int count = count3 + 3;
						int srcIndex = ReadWord( input );
						if( srcIndex >= destIndex )
							throw new NotImplementedException( string.Format( "srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex ) );

						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] = dest[ srcIndex++ ];
					}
				}
			}
		}
	}
}
