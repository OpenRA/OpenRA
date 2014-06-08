#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using csvorbis;

namespace OpenRA.FileFormats
{
	public class OggVorbisLoader
	{
		public readonly int Channels;
		public readonly int SampleRate;
		public readonly int BitsPerSample;

		public readonly byte[] RawData;

		public OggVorbisLoader(Stream s)
		{
			using (var decoder = new OggDecodeStream(s, true))
			{
				this.Channels = decoder.Info.channels;
				this.SampleRate = decoder.Info.rate;
				this.BitsPerSample = 16;
				this.RawData = decoder.ToArray();
			}
		}
	}
}
