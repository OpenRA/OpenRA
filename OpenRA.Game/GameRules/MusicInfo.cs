
using System;

using System.Collections.Generic;
using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly MusicPool Pool;
		public readonly string[] Music = { };

		public MusicInfo( MiniYaml y )
		{
			FieldLoader.Load(this, y);
			Pool = new MusicPool(Music);
		}
	}
	
	public class MusicPool
	{
		readonly string[] clips;
		int playing = 0;

		public MusicPool(params string[] clips)
		{
			this.clips = clips;
		}

		public string GetNext() 
		{ 
			playing = (playing + 1) % clips.Length;
			return clips[playing]; 
		}
		public string GetPrev() 
		{ 
			playing = (playing + clips.Length - 1) % clips.Length;
			return clips[playing]; 
		}
		public string GetCurrent(){ return clips[playing];}
		
	}
}
