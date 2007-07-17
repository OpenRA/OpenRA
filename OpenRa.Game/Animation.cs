using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class Animation
	{
		readonly string name;
		Sequence currentSequence;
		int frame = 0;

		public Animation( string name )
		{
			this.name = name;
			PlayToEnd( "idle" );
		}

		public Sprite[] Images { get { return new Sprite[] { currentSequence.GetSprite( frame ) }; } }

		public void PlayToEnd( string sequenceName )
		{
			currentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = 0;
			//tickFunc = delegate
			//{
			//      ++frame;
			//      if( frame >= currentSequence.Length )
			//      {
			//            frame = currentSequence.Length - 1;
			//            tickFunc = delegate { };
			//      }
			//};
		}

		public void PlayRepeating( string sequenceName )
		{
			currentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = 0;
			//tickFunc = delegate
			//{
			//      frame = ( frame + 1 ) % currentSequence.Length;
			//};
		}

		Action<double> tickFunc;
		public void Tick( double t )
		{
			//tickFunc( t );
		}
	}
}
