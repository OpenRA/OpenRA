#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Runtime.InteropServices;

namespace OpenRa.Support
{
	public static class OpenAlInterop
	{
		public const int AL_GAIN = 0x100A;

		public const int AL_FORMAT_MONO8 = 0x1100;
		public const int AL_FORMAT_MONO16 = 0x1101;
		public const int AL_FORMAT_STEREO8 = 0x1102;
		public const int AL_FORMAT_STEREO16 = 0x1103;

		public const int AL_PITCH = 0x1003;

		public const int AL_POSITION = 0x1004;
		public const int AL_DIRECTION = 0x1005;
		public const int AL_VELOCITY = 0x1006;

		public const int AL_LOOPING = 0x1007;
		public const int AL_BUFFER = 0x1009;

		public const int AL_TRUE = 1;
		public const int AL_FALSE = 0;

		public const int AL_SOURCE_STATE = 0x1010;
		public const int AL_INITIAL = 0x1011;
		public const int AL_PLAYING = 0x1012;
		public const int AL_PAUSED = 0x1013;
		public const int AL_STOPPED = 0x1014;

		[DllImport("OpenAL32.dll")]
		public static extern IntPtr alcOpenDevice(IntPtr deviceName);

		[DllImport("OpenAL32.dll")]
		public static extern IntPtr alcCreateContext(IntPtr device, IntPtr attrList);

		[DllImport("OpenAL32.dll")]
		public static extern bool alcMakeContextCurrent(IntPtr context);

		[DllImport("OpenAL32.dll")]
		public static extern void alListenerf(int param, float value);

		[DllImport("OpenAL32.dll")]
		public static extern void alGenBuffers(int n, out int buffers);

		[DllImport("OpenAL32.dll")]
		public static extern void alBufferData(int buffer, int format, byte[] data, int size, int freq);

		//[DllImport("OpenAL32.dll")]
		//public static extern void alGenSources(int n, IntPtr sources);

		[DllImport("OpenAL32.dll")]
		public static extern void alGenSources(int one, out int source);

		[DllImport("OpenAL32.dll")]
		public static extern int alGetError();

		[DllImport("OpenAL32.dll")]
		public static extern void alSourcef(int source, int param, float value);

		[DllImport("OpenAL32.dll")]
		public static extern void alSource3f(int source, int param, float v1, float v2, float v3);

		[DllImport("OpenAL32.dll")]
		public static extern void alSourcei(int source, int param, int value);

		[DllImport("OpenAL32.dll")]
		public static extern void alGetSourcei(int source, int param, out int value);


		[DllImport("OpenAL32.dll")]
		public static extern void alSourcePlay(int source);
	}
}
