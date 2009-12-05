using System;

namespace OpenRa.FileFormats
{
    class FastByteReader
    {
        readonly byte[] src;
        int offset = 0;

        public FastByteReader(byte[] src)
        {
            this.src = src;
        }

        public bool Done() { return offset >= src.Length; }
        public byte ReadByte() { return src[offset++]; }
        public int ReadWord()
        {
            int x = ReadByte();
            return x | (ReadByte() << 8);
        }

        public void CopyTo(byte[] dest, int offset, int count)
        {
            Array.Copy(src, this.offset, dest, offset, count);
            this.offset += count;
        }
    }

	public static class Format80
	{
		static void ReplicatePrevious( byte[] dest, int destIndex, int srcIndex, int count )
		{
			if( srcIndex >= destIndex )
				throw new NotImplementedException( string.Format( "srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex ) );

			if( destIndex - srcIndex == 1 )
			{
				for( int i = 0 ; i < count ; i++ )
					dest[ destIndex + i ] = dest[ destIndex - 1 ];
			}
			else
			{
				for( int i = 0 ; i < count ; i++ )
					dest[ destIndex + i ] = dest[ srcIndex + i ];
			}
		}

		public static int DecodeInto( byte[] src, byte[] dest )
		{
            var ctx = new FastByteReader(src);
			int destIndex = 0;

			while( true )
			{
                byte i = ctx.ReadByte();
				if( ( i & 0x80 ) == 0 )
				{
					// case 2
                    byte secondByte = ctx.ReadByte();
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

					ctx.CopyTo( dest, destIndex, count );
					destIndex += count;
				}
				else
				{
					int count3 = i & 0x3F;
					if( count3 == 0x3E )
					{
						// case 4
                        int count = ctx.ReadWord();
                        byte color = ctx.ReadByte();

						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] = color;
					}
					else if( count3 == 0x3F )
					{
						// case 5
                        int count = ctx.ReadWord();
                        int srcIndex = ctx.ReadWord();
						if( srcIndex >= destIndex )
							throw new NotImplementedException( string.Format( "srcIndex >= destIndex  {0}  {1}", srcIndex, destIndex ) );

						for( int end = destIndex + count ; destIndex < end ; destIndex++ )
							dest[ destIndex ] = dest[ srcIndex++ ];
					}
					else
					{
						// case 3
						int count = count3 + 3;
                        int srcIndex = ctx.ReadWord();
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
