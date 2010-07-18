#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.FileFormats
{
	public static class Format40
	{
		public static int DecodeInto( byte[] src, byte[] dest )
		{
            var ctx = new FastByteReader(src);
			int destIndex = 0;

			while( true )
			{
                byte i = ctx.ReadByte();
				if( ( i & 0x80 ) == 0 )
				{
					int count = i & 0x7F;
					if( count == 0 )
					{
						// case 6
                        count = ctx.ReadByte();
                        byte value = ctx.ReadByte();
						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] ^= value;
					}
					else
					{
						// case 5
						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
                            dest[destIndex] ^= ctx.ReadByte();
					}
				}
				else
				{
					int count = i & 0x7F;
					if( count == 0 )
					{
                        count = ctx.ReadWord();
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
                                dest[destIndex] ^= ctx.ReadByte();
						}
						else
						{
							// case 4
                            byte value = ctx.ReadByte();
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
