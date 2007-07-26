using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace OpenRa
{
	public class PriorityQueue<T>
		where T : IComparable<T>
	{
		List<T[]> items = new List<T[]>();
		int level;
		int index;

		public PriorityQueue()
		{
			items.Add( new T[ 1 ] );
		}

		public void Add( T item )
		{
			int addLevel = level;
			int addIndex = index;

			while( addLevel >= 1 && Above( addLevel, addIndex ).CompareTo( item ) > 0 )
			{
				items[ addLevel ][ addIndex ] = Above( addLevel, addIndex );
				--addLevel;
				addIndex >>= 1;
			}

			items[ addLevel ][ addIndex ] = item;

			++index;
			if( index >= ( 1 << level ) )
			{
				index = 0;
				++level;
				if( items.Count <= level )
					items.Add( new T[ 1 << level ] );
			}
		}

		public bool Empty
		{
			get { return ( level == 0 ); }
		}

		T At( int level, int index )
		{
			return items[ level ][ index ];
		}

		T Above( int level, int index )
		{
			return items[ level - 1 ][ index >> 1 ];
		}

		T Last()
		{
			int lastLevel = level;
			int lastIndex = index;
			--lastIndex;
			if( lastIndex < 0 )
			{
				--lastLevel;
				lastIndex = ( 1 << lastLevel ) - 1;
			}
			return At( lastLevel, lastIndex );
		}

		public T Pop()
		{
			if( level == 0 && index == 0 )
				throw new InvalidOperationException( "Attempting to pop empty PriorityQueue" );

			T ret = At( 0, 0 );
			BubbleInto( 0, 0, Last() );
			--index;
			if( index < 0 )
			{
				--level;
				index = ( 1 << level ) - 1;
			}

			return ret;
		}

		void BubbleInto( int intoLevel, int intoIndex, T val )
		{
			int downLevel = intoLevel + 1;
			int downIndex = intoIndex << 1;

			if( downLevel > level || ( downLevel == level && downIndex >= index ))
			{
				items[ intoLevel ][ intoIndex ] = val;
				return;
			}

			if( downLevel == level && downIndex == index - 1 )
			{
				//Log.Write( "one-option bubble" );
			}
			else if( At( downLevel, downIndex ).CompareTo( At( downLevel, downIndex + 1 ) ) < 0 )
			{
				//Log.Write( "left bubble" );
			}
			else
			{
				//Log.Write( "right bubble" );
				++downIndex;
			}

			if( val.CompareTo( At( downLevel, downIndex ) ) <= 0 )
			{
				items[ intoLevel ][ intoIndex ] = val;
				return;
			}

			items[ intoLevel ][ intoIndex ] = At( downLevel, downIndex );
			BubbleInto( downLevel, downIndex, val );
		}

		//void Invariant()
		//{
		//      for( int i = 1 ; i < level ; i++ )
		//            for( int j = 0 ; j < RowLength( i ) ; j++ )
		//                  if( At( i, j ).CompareTo( Above( i, j ) ) < 0 )
		//                        System.Diagnostics.Debug.Assert( At( i, j ).CompareTo( Above( i, j ) ) < 0, "At( i, j ) > Above( i, j )" );
		//}

		int RowLength( int i )
		{
			if( i == level )
				return index;
			return ( 1 << i );
		}
	}
}
