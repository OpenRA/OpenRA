#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;

namespace OpenRA.Graphics
{
	public class Animation
	{
		string name;
		public Sequence CurrentSequence { get; private set; }
		int frame = 0;
		bool backwards = false;
		bool tickAlways;

		Func<int> facingFunc;

		public string Name { get { return name; } }

		public Animation( string name )
			: this( name, () => 0 )
		{
		}

		public Animation( string name, Func<int> facingFunc )
		{
			this.name = name.ToLowerInvariant();
			this.tickFunc = () => { };
			this.facingFunc = facingFunc;
		}

		public Sprite Image
		{
			get
			{
				return backwards
					? CurrentSequence.GetSprite(CurrentSequence.End - frame - 1, facingFunc())
					: CurrentSequence.GetSprite(frame, facingFunc());
			}
		}

		public void Play( string sequenceName )
		{
			PlayThen(sequenceName, () => { });
		}

		public void PlayRepeating( string sequenceName )
		{
			PlayThen( sequenceName, () => PlayRepeating( CurrentSequence.Name ) );
		}

		public void ReplaceAnim(string sequenceName)
		{
			if (!HasSequence(sequenceName))
				return;

			CurrentSequence = SequenceProvider.GetSequence(name, sequenceName);
			frame %= CurrentSequence.Length;
		}

		public void PlayThen( string sequenceName, Action after )
		{
			after = after ?? ( () => { } );
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

		public bool HasSequence(string seq) { return SequenceProvider.HasSequence( name, seq ); }

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
					timeUntilNextFrame += CurrentSequence != null ? CurrentSequence.Tick : 40; // 25 fps == 40 ms
				}
			}
		}

		public void ChangeImage(string newImage)
		{
			newImage = newImage.ToLowerInvariant();
			
			if (name != newImage)
			{
				name = newImage.ToLowerInvariant();
				ReplaceAnim(CurrentSequence.Name);
			}
		}

		public Sequence GetSequence( string sequenceName )
		{
			return SequenceProvider.GetSequence( name, sequenceName );
		}
	}
}
