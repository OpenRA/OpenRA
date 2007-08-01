using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using IjwFramework.Delegates;

namespace OpenRa.Game
{
	class Animation
	{
		readonly string name;
		Sequence currentSequence;
		int frame = 0;
		bool tickAlways;

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
			tickAlways = false;
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

		public void PlayFetchIndex( string sequenceName, Provider<int> func )
		{
			tickAlways = true;
			currentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = func();
			tickFunc = delegate
			{
				frame = func();
			};
		}

		int timeUntilNextFrame;

		Action<int> tickFunc;
		public void Tick( int t )
		{
			if( tickAlways )
				tickFunc( t );
			else
			{
				timeUntilNextFrame -= t;
				while( timeUntilNextFrame <= 0 )
				{
					tickFunc( 40 );
					timeUntilNextFrame += 40; // 25 fps == 40 ms
				}
			}
		}
	}
}
