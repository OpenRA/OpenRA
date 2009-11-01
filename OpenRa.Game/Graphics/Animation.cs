using System;

namespace OpenRa.Game.Graphics
{
	class Animation
	{
		readonly string name;
		public Sequence CurrentSequence { get; private set; }
		int frame = 0;
		bool backwards = false;
		bool tickAlways;

		public Animation( string name )
		{
			this.name = name.ToLowerInvariant();
			// Play( "idle" );
		}

		public Sprite Image
		{
			get
			{
				return backwards
					? CurrentSequence.GetSprite(CurrentSequence.End - frame - 1)
					: CurrentSequence.GetSprite(frame);
			}
		}

		public float2 Center { get { return 0.25f * new float2(CurrentSequence.GetSprite(0).bounds.Size); } }

		public void Play( string sequenceName )
		{
			PlayThen(sequenceName, () => { });
		}

		public void PlayBackwards(string sequenceName)
		{
			PlayBackwardsThen(sequenceName, () => { });
		}

		public void PlayRepeating( string sequenceName )
		{
			PlayThen( sequenceName, () => PlayRepeating( sequenceName ) );
		}

		public void PlayRepeatingPreservingPosition(string sequenceName)
		{
			var f = frame;
			PlayThen(sequenceName, () => PlayRepeating(sequenceName));
			frame = f % CurrentSequence.Length;
		}

		public void PlayThen( string sequenceName, Action after )
		{
			backwards = false;
			tickAlways = false;
			CurrentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if( frame >= CurrentSequence.Length )
				{
					frame = CurrentSequence.Length - 1;
					tickFunc = () => { };
					after();
				}
			};
		}

		public void PlayBackwardsThen(string sequenceName, Action after)
		{
			PlayThen(sequenceName, after);
			backwards = true;
		}

		public void PlayFetchIndex( string sequenceName, Func<int> func )
		{
			backwards = false;
			tickAlways = true;
			CurrentSequence = SequenceProvider.GetSequence( name, sequenceName );
			frame = func();
			tickFunc = () => frame = func();
		}

		int timeUntilNextFrame;
		Action tickFunc;

		public void Tick()
		{
			Tick( 40 ); // tick one frame
		}

		public void Tick( int t )
		{
			if( tickAlways )
				tickFunc();
			else
			{
				timeUntilNextFrame -= t;
				while( timeUntilNextFrame <= 0 )
				{
					tickFunc();
					timeUntilNextFrame += 40; // 25 fps == 40 ms
				}
			}
		}
	}
}
