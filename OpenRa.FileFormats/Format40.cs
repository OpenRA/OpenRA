using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.FileFormats
{
	public static class Format40
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

		public static int DecodeInto( MemoryStream input, byte[] dest )
		{
			int destIndex = 0;
			while( true )
			{
				byte i = ReadByte( input );
				if( ( i & 0x80 ) == 0 )
				{
					int count = i & 0x7F;
					if( count == 0 )
					{
						// case 6
						count = ReadByte( input );
						byte value = ReadByte( input );
						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] ^= value;
					}
					else
					{
						// case 5
						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] ^= ReadByte( input );
					}
				}
				else
				{
					int count = i & 0x7F;
					if( count == 0 )
					{
						count = ReadWord( input );
						if( count == 0 )
							return destIndex;

						if( ( count & 0x8000 ) == 0 )
						{
							// case 2
							destIndex += ( count & 0x7FFF );
						}
						else if( ( count & 0x4000 ) == 0 )
						{
							// case 3
							for( int end = destIndex + ( count & 0x3FFF ) ; destIndex < end ; destIndex++ )
								dest[ destIndex ] ^= ReadByte( input );
						}
						else
						{
							// case 4
							byte value = ReadByte( input );
							for( int end = destIndex + ( count & 0x3FFF ) ; destIndex < end ; destIndex++ )
								dest[ destIndex ] ^= value;
						}
					}
					else
					{
						// case 1
						destIndex += count;
					}
				}
			}
		}
	}
}
