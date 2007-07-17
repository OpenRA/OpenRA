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
			Play( "idle" );
		}

		public Sprite[] Images { get { return new Sprite[] { currentSequence.GetSprite( frame ) }; } }

		public void Play( string sequenceName )
		{
			PlayThen( sequenceName, delegate { } );
		}

		public void PlayRepeating( string sequenceName )
		{
			PlayThen( sequenceName, delegate { PlayRepeating( sequenceName ); } );
		}

		public void PlayThen( string sequenceName, MethodInvoker after )
		{
			currentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = 0;
			tickFunc = delegate
			{
				++frame;
				if( frame >= currentSequence.Length )
				{
					frame = currentSequence.Length - 1;
					tickFunc = delegate { };
					after();
				}
			};
		}

		double timeUntilNextFrame;

		Action<double> tickFunc;
		public void Tick( double t )
		{
			timeUntilNextFrame -= t;
			while( timeUntilNextFrame <= 0 )
			{
				tickFunc( t );
				timeUntilNextFrame += ( 40.0 / 1000.0 ); // 25 fps == 40 ms
			}
		}
	}
}
