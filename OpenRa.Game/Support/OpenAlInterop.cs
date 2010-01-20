using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		[DllImport("OpenAL32.dll")]
		public static extern void alGenSources(int n, IntPtr sources);

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
