#region --- OpenTK.OpenAL License ---
/* AlFunctions.cs
 * C header: \OpenAL 1.1 SDK\include\Al.h
 * Spec: http://www.openal.org/openal_webstf/specs/OpenAL11Specification.pdf
 * Copyright (c) 2008 Christoph Brandtner and Stefanos Apostolopoulos
 * See license.txt for license details
 * http://www.OpenTK.net */
#endregion

using System;
using System.Runtime.InteropServices;
using System.Security;

using OpenTK;

#pragma warning disable 3021

namespace OpenTK.Audio.OpenAL
{
	public static partial class AL
	{

		#region Constants

		internal const string Lib = "soft_oal.dll";
		internal const CallingConvention Style = CallingConvention.Cdecl;

		#endregion Constants

		#region Renderer State management

		[DllImport(AL.Lib, EntryPoint = "alEnable", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Enable(ALCapability capability);
		//AL_API void AL_APIENTRY alEnable( ALenum capability );

		[DllImport(AL.Lib, EntryPoint = "alDisable", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Disable(ALCapability capability);

		[DllImport(AL.Lib, EntryPoint = "alIsEnabled", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern bool IsEnabled(ALCapability capability);

		#endregion Renderer State management

		#region State retrieval

		[DllImport(AL.Lib, EntryPoint = "alGetString", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		private static extern IntPtr GetStringPrivate(ALGetString param); // accepts the enums AlError, AlContextString

		public static string Get(ALGetString param)
		{
			return Marshal.PtrToStringAnsi(GetStringPrivate(param));
		}

		public static string GetErrorString(ALError param)
		{
			return Marshal.PtrToStringAnsi(GetStringPrivate((ALGetString)param));
		}

		[DllImport(AL.Lib, EntryPoint = "alGetInteger", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern int Get(ALGetInteger param);

		[DllImport(AL.Lib, EntryPoint = "alGetFloat", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern float Get(ALGetFloat param);

		[DllImport(AL.Lib, EntryPoint = "alGetError", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern ALError GetError();

		#endregion State retrieval

		#region Extension support.

		[DllImport(AL.Lib, EntryPoint = "alIsExtensionPresent", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern bool IsExtensionPresent([In] string extname);

		[DllImport(AL.Lib, EntryPoint = "alGetProcAddress", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern IntPtr GetProcAddress([In] string fname);

		[DllImport(AL.Lib, EntryPoint = "alGetEnumValue", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern int GetEnumValue([In] string ename);

		#endregion Extension support.

		#region Set Listener parameters

		[DllImport(AL.Lib, EntryPoint = "alListenerf", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Listener(ALListenerf param, float value);

		[DllImport(AL.Lib, EntryPoint = "alListener3f", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Listener(ALListener3f param, float value1, float value2, float value3);

		[DllImport(AL.Lib, EntryPoint = "alListenerfv", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		unsafe private static extern void ListenerPrivate(ALListenerfv param, float* values);

		public static void Listener(ALListenerfv param, ref float[] values)
		{
			unsafe
			{
				fixed (float* ptr = &values[0])
				{
					ListenerPrivate(param, ptr);
				}
			}
		}

		// Not used by any Enums

		#endregion Set Listener parameters

		#region Get Listener parameters

		[DllImport(AL.Lib, EntryPoint = "alGetListenerf", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void GetListener(ALListenerf param, [Out] out float value);

		[DllImport(AL.Lib, EntryPoint = "alGetListener3f", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void GetListener(ALListener3f param, [Out] out float value1, [Out] out float value2, [Out] out float value3);

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alGetListenerfv", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		unsafe public static extern void GetListener(ALListenerfv param, float* values);

		#endregion Get Listener parameters

		#region Create Source objects

		#region GenSources()

		[DllImport(AL.Lib, EntryPoint = "alGenSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		unsafe private static extern void GenSourcesPrivate(int n, [Out] uint* sources);

		[CLSCompliant(false)]
		public static void GenSources(int n, out uint sources)
		{
			unsafe
			{
				fixed (uint* sources_ptr = &sources)
				{
					GenSourcesPrivate(n, sources_ptr);
				}
			}
		}

		public static void GenSources(int n, out int sources)
		{
			unsafe
			{
				fixed (int* sources_ptr = &sources)
				{
					GenSourcesPrivate(n, (uint*)sources_ptr);
				}
			}
		}

		public static void GenSources(int[] sources)
		{
			uint[] temp = new uint[sources.Length];
			GenSources(temp.Length, out temp[0]);
			for (int i = 0; i < temp.Length; i++)
			{
				sources[i] = (int)temp[i];
			}
		}

		public static int[] GenSources(int n)
		{
			uint[] temp = new uint[n];
			GenSources(temp.Length, out temp[0]);
			int[] sources = new int[n];
			for (int i = 0; i < temp.Length; i++)
			{
				sources[i] = (int)temp[i];
			}
			return sources;
		}

		public static int GenSource()
		{
			int temp;
			GenSources(1, out temp);
			return (int)temp;
		}

		[CLSCompliant(false)]
		public static void GenSource(out uint source)
		{
			GenSources(1, out source);
		}

		#endregion GenSources()

		#region DeleteSources()

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		unsafe public static extern void DeleteSources(int n, [In] uint* sources); // Delete Source objects 

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void DeleteSources(int n, ref uint sources);

		[DllImport(AL.Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void DeleteSources(int n, ref int sources);

		[CLSCompliant(false)]
		public static void DeleteSources(uint[] sources)
		{
			if (sources == null) throw new ArgumentNullException();
			if (sources.Length == 0) throw new ArgumentOutOfRangeException();
			DeleteBuffers(sources.Length, ref sources[0]);
		}

		public static void DeleteSources(int[] sources)
		{
			if (sources == null) throw new ArgumentNullException();
			if (sources.Length == 0) throw new ArgumentOutOfRangeException();
			DeleteBuffers(sources.Length, ref sources[0]);
		}

		[CLSCompliant(false)]
		public static void DeleteSource(ref uint source)
		{
			DeleteSources(1, ref source);
		}

		public static void DeleteSource(int source)
		{
			DeleteSources(1, ref source);
		}

		#endregion DeleteSources()

		#region IsSource()

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alIsSource", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern bool IsSource(uint sid);

		public static bool IsSource(int sid)
		{
			return IsSource((uint)sid);
		}

		#endregion IsSource()

		#endregion Create Source objects

		#region Set Source parameters

		#region Sourcef

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourcef", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Source(uint sid, ALSourcef param, float value);

		public static void Source(int sid, ALSourcef param, float value)
		{
			Source((uint)sid, param, value);
		}

		#endregion Sourcef

		#region Source3f

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSource3f", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Source(uint sid, ALSource3f param, float value1, float value2, float value3);

		public static void Source(int sid, ALSource3f param, float value1, float value2, float value3)
		{
			Source((uint)sid, param, value1, value2, value3);
		}

		#endregion Source3f

		#region Sourcei

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourcei", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Source(uint sid, ALSourcei param, int value);

		public static void Source(int sid, ALSourcei param, int value)
		{
			Source((uint)sid, param, value);
		}

		[CLSCompliant(false)]
		public static void Source(uint sid, ALSourceb param, bool value)
		{
			Source(sid, (ALSourcei)param, (value) ? 1 : 0);
		}

		public static void Source(int sid, ALSourceb param, bool value)
		{
			Source((uint)sid, (ALSourcei)param, (value) ? 1 : 0);
		}

		[CLSCompliant(false)]
		public static void BindBufferToSource(uint source, uint buffer)
		{
			Source(source, ALSourcei.Buffer, (int)buffer);
		}

		public static void BindBufferToSource(int source, int buffer)
		{
			Source((uint)source, ALSourcei.Buffer, buffer);
		}

		#endregion Sourcei

		#region Source3i

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSource3i", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Source(uint sid, ALSource3i param, int value1, int value2, int value3);

		public static void Source(int sid, ALSource3i param, int value1, int value2, int value3)
		{
			Source((uint)sid, param, value1, value2, value3);
		}

		#endregion Source3i

		#endregion Set Source parameters

		#region Get Source parameters

		#region GetSourcef

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alGetSourcef", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void GetSource(uint sid, ALSourcef param, [Out] out float value);

		public static void GetSource(int sid, ALSourcef param, out float value)
		{
			GetSource((uint)sid, param, out value);
		}

		#endregion GetSourcef

		#region GetSource3f

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alGetSource3f", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void GetSource(uint sid, ALSource3f param, [Out] out float value1, [Out] out float value2, [Out] out float value3);

		public static void GetSource(int sid, ALSource3f param, out float value1, out float value2, out float value3)
		{
			GetSource((uint)sid, param, out value1, out value2, out value3);
		}

		#endregion GetSource3f

		#region GetSourcei

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alGetSourcei", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void GetSource(uint sid, ALGetSourcei param, [Out] out int value);

		public static void GetSource(int sid, ALGetSourcei param, out int value)
		{
			GetSource((uint)sid, param, out value);
		}

		[CLSCompliant(false)]
		public static void GetSource(uint sid, ALSourceb param, out bool value)
		{
			int result;
			GetSource(sid, (ALGetSourcei)param, out result);
			value = result != 0;
		}

		public static void GetSource(int sid, ALSourceb param, out bool value)
		{
			int result;
			GetSource((uint)sid, (ALGetSourcei)param, out result);
			value = result != 0;
		}

		#endregion GetSourcei

		// Not used by any Enum:

		#endregion Get Source parameters

		#region Source vector based playback calls

		#region SourcePlay

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourcePlayv"), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void SourcePlay(int ns, [In] uint* sids);

		[CLSCompliant(false)]
		public static void SourcePlay(int ns, uint[] sids)
		{
			unsafe
			{
				fixed (uint* ptr = sids)
				{
					SourcePlay(ns, ptr);
				}
			}
		}

		public static void SourcePlay(int ns, int[] sids)
		{
			uint[] temp = new uint[ns];
			for (int i = 0; i < ns; i++)
			{
				temp[i] = (uint)sids[i];
			}
			SourcePlay(ns, temp);
		}

		[CLSCompliant(false)]
		public static void SourcePlay(int ns, ref uint sids)
		{
			unsafe
			{
				fixed (uint* ptr = &sids)
				{
					SourcePlay(ns, ptr);
				}
			}
		}

		#endregion SourcePlay

		#region SourceStop

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourceStopv"), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void SourceStop(int ns, [In] uint* sids);

		[CLSCompliant(false)]
		public static void SourceStop(int ns, uint[] sids)
		{
			unsafe
			{
				fixed (uint* ptr = sids)
				{
					SourceStop(ns, ptr);
				}
			}
		}

		public static void SourceStop(int ns, int[] sids)
		{
			uint[] temp = new uint[ns];
			for (int i = 0; i < ns; i++)
			{
				temp[i] = (uint)sids[i];
			}
			SourceStop(ns, temp);
		}

		[CLSCompliant(false)]
		public static void SourceStop(int ns, ref uint sids)
		{
			unsafe
			{
				fixed (uint* ptr = &sids)
				{
					SourceStop(ns, ptr);
				}
			}
		}

		#endregion SourceStop

		#region SourceRewind

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourceRewindv"), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void SourceRewind(int ns, [In] uint* sids);

		[CLSCompliant(false)]
		public static void SourceRewind(int ns, uint[] sids)
		{
			unsafe
			{
				fixed (uint* ptr = sids)
				{
					SourceRewind(ns, ptr);
				}
			}
		}

		public static void SourceRewind(int ns, int[] sids)
		{
			uint[] temp = new uint[ns];
			for (int i = 0; i < ns; i++)
			{
				temp[i] = (uint)sids[i];
			}
			SourceRewind(ns, temp);
		}

		[CLSCompliant(false)]
		public static void SourceRewind(int ns, ref uint sids)
		{
			unsafe
			{
				fixed (uint* ptr = &sids)
				{
					SourceRewind(ns, ptr);
				}
			}
		}

		#endregion SourceRewind

		#region SourcePause

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourcePausev"), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void SourcePause(int ns, [In] uint* sids);

		[CLSCompliant(false)]
		public static void SourcePause(int ns, uint[] sids)
		{
			unsafe
			{
				fixed (uint* ptr = sids)
				{
					SourcePause(ns, ptr);
				}
			}
		}
		public static void SourcePause(int ns, int[] sids)
		{
			uint[] temp = new uint[ns];
			for (int i = 0; i < ns; i++)
			{
				temp[i] = (uint)sids[i];
			}
			SourcePause(ns, temp);
		}

		[CLSCompliant(false)]
		public static void SourcePause(int ns, ref uint sids)
		{
			unsafe
			{
				fixed (uint* ptr = &sids)
				{
					SourcePause(ns, ptr);
				}
			}
		}

		#endregion SourcePause

		#endregion Source vector based playback calls

		#region Source based playback calls

		#region SourcePlay

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourcePlay", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void SourcePlay(uint sid);

		public static void SourcePlay(int sid)
		{
			SourcePlay((uint)sid);
		}

		#endregion SourcePlay

		#region SourceStop

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourceStop", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void SourceStop(uint sid);

		public static void SourceStop(int sid)
		{
			SourceStop((uint)sid);
		}

		#endregion SourceStop

		#region SourceRewind

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourceRewind", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void SourceRewind(uint sid);

		public static void SourceRewind(int sid)
		{
			SourceRewind((uint)sid);
		}

		#endregion SourceRewind

		#region SourcePause

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourcePause", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void SourcePause(uint sid);

		public static void SourcePause(int sid)
		{
			SourcePause((uint)sid);
		}

		#endregion SourcePause

		#endregion Source based playback calls

		#region Source Queuing

		#region SourceQueueBuffers

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourceQueueBuffers", CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void SourceQueueBuffers(uint sid, int numEntries, [In] uint* bids);

		[CLSCompliant(false)]
		public static void SourceQueueBuffers(uint sid, int numEntries, uint[] bids)
		{
			unsafe
			{
				fixed (uint* ptr = bids)
				{
					SourceQueueBuffers(sid, numEntries, ptr);
				}
			}
		}

		public static void SourceQueueBuffers(int sid, int numEntries, int[] bids)
		{
			uint[] temp = new uint[numEntries];
			for (int i = 0; i < numEntries; i++)
			{
				temp[i] = (uint)bids[i];
			}
			SourceQueueBuffers((uint)sid, numEntries, temp);
		}

		[CLSCompliant(false)]
		public static void SourceQueueBuffers(uint sid, int numEntries, ref uint bids)
		{
			unsafe
			{
				fixed (uint* ptr = &bids)
				{
					SourceQueueBuffers(sid, numEntries, ptr);
				}
			}
		}

		public static void SourceQueueBuffer(int source, int buffer)
		{
			unsafe { AL.SourceQueueBuffers((uint)source, 1, (uint*)&buffer); }
		}

		#endregion SourceQueueBuffers

		#region SourceUnqueueBuffers

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers", CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void SourceUnqueueBuffers(uint sid, int numEntries, [In] uint* bids);

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers", CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, [Out] uint[] bids);

		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers", CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(int sid, int numEntries, [Out] int[] bids);

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers", CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, ref uint bids);

		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers", CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(int sid, int numEntries, ref int bids);

		public static int SourceUnqueueBuffer(int sid)
		{
			uint buf;
			unsafe { SourceUnqueueBuffers((uint)sid, 1, &buf); }
			return (int)buf;
		}

		public static int[] SourceUnqueueBuffers(int sid, int numEntries)
		{
			if (numEntries <= 0) throw new ArgumentOutOfRangeException("numEntries", "Must be greater than zero.");
			int[] buf = new int[numEntries];
			SourceUnqueueBuffers(sid, numEntries, buf);
			return buf;
		}

		#endregion SourceUnqueueBuffers

		#endregion Source Queuing

		#region Buffer objects

		#region GenBuffers

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void GenBuffers(int n, [Out] uint* buffers);

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void GenBuffers(int n, [Out] int* buffers);

		[CLSCompliant(false)]
		public static void GenBuffers(int n, out uint buffers)
		{
			unsafe
			{
				fixed (uint* pbuffers = &buffers)
				{
					GenBuffers(n, pbuffers);
				}
			}
		}

		public static void GenBuffers(int n, out int buffers)
		{
			unsafe
			{
				fixed (int* pbuffers = &buffers)
				{
					GenBuffers(n, pbuffers);
				}
			}
		}

		public static int[] GenBuffers(int n)
		{
			int[] buffers = new int[n];
			GenBuffers(buffers.Length, out buffers[0]);
			return buffers;
		}

		public static int GenBuffer()
		{
			int temp;
			GenBuffers(1, out temp);
			return (int)temp;
		}

		[CLSCompliant(false)]
		public static void GenBuffer(out uint buffer)
		{
			GenBuffers(1, out buffer);
		}

		#endregion GenBuffers

		#region DeleteBuffers

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		unsafe public static extern void DeleteBuffers(int n, [In] uint* buffers);

		[CLSCompliant(false)]
		[DllImport(AL.Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		unsafe public static extern void DeleteBuffers(int n, [In] int* buffers);

		[CLSCompliant(false)]
		public static void DeleteBuffers(int n, [In] ref uint buffers)
		{
			unsafe
			{
				fixed (uint* pbuffers = &buffers)
				{
					DeleteBuffers(n, pbuffers);
				}
			}
		}

		public static void DeleteBuffers(int n, [In] ref int buffers)
		{
			unsafe
			{
				fixed (int* pbuffers = &buffers)
				{
					DeleteBuffers(n, pbuffers);
				}
			}
		}

		[CLSCompliant(false)]
		public static void DeleteBuffers(uint[] buffers)
		{
			if (buffers == null) throw new ArgumentNullException();
			if (buffers.Length == 0) throw new ArgumentOutOfRangeException();
			DeleteBuffers(buffers.Length, ref buffers[0]);
		}

		public static void DeleteBuffers(int[] buffers)
		{
			if (buffers == null) throw new ArgumentNullException();
			if (buffers.Length == 0) throw new ArgumentOutOfRangeException();
			DeleteBuffers(buffers.Length, ref buffers[0]);
		}

		[CLSCompliant(false)]
		public static void DeleteBuffer(ref uint buffer)
		{
			DeleteBuffers(1, ref buffer);
		}

		public static void DeleteBuffer(int buffer)
		{
			DeleteBuffers(1, ref buffer);
		}

		#endregion DeleteBuffers

		#region IsBuffer

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alIsBuffer", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern bool IsBuffer(uint bid);

		public static bool IsBuffer(int bid)
		{
			uint temp = (uint)bid;
			return IsBuffer(temp);
		}

		#endregion IsBuffer

		#region BufferData

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alBufferData", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void BufferData(uint bid, ALFormat format, IntPtr buffer, int size, int freq);

		public static void BufferData(int bid, ALFormat format, IntPtr buffer, int size, int freq)
		{
			BufferData((uint)bid, format, buffer, size, freq);
		}

		public static void BufferData<TBuffer>(int bid, ALFormat format, TBuffer[] buffer, int size, int freq)
			where TBuffer : struct
		{
			if (!BlittableValueType.Check(buffer))
				throw new ArgumentException("buffer");

			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try { BufferData(bid, format, handle.AddrOfPinnedObject(), size, freq); }
			finally { handle.Free(); }
		}

		#endregion BufferData

		#endregion Buffer objects

		#region Set Buffer parameters (currently parameters can only be read)

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alBufferi", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Buffer(int bid, ALBufferi param, uint value);

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alBufferiv", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void Buffer(int bid, ALBufferiv param, uint[] value);

		#endregion Set Buffer parameters

		#region Get Buffer parameters

		#region GetBufferi

		[CLSCompliant(false), DllImport(AL.Lib, EntryPoint = "alGetBufferi", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void GetBuffer(uint bid, ALGetBufferi param, [Out] out int value);

		public static void GetBuffer(int bid, ALGetBufferi param, out int value)
		{
			GetBuffer((uint)bid, param, out value);
		}

		#endregion GetBufferi


		#endregion Get Buffer parameters

		#region Global Parameters

		[DllImport(AL.Lib, EntryPoint = "alDopplerFactor", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void DopplerFactor(float value);

		[DllImport(AL.Lib, EntryPoint = "alDopplerVelocity", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void DopplerVelocity(float value);

		[DllImport(AL.Lib, EntryPoint = "alSpeedOfSound", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void SpeedOfSound(float value);

		[DllImport(AL.Lib, EntryPoint = "alDistanceModel", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void DistanceModel(ALDistanceModel distancemodel);

		#endregion Global Parameters

		#region Helpers

		[CLSCompliant(false)]
		public static ALSourceState GetSourceState(uint sid)
		{
			int temp;
			AL.GetSource(sid, ALGetSourcei.SourceState, out temp);
			return (ALSourceState)temp;
		}

		public static ALSourceState GetSourceState(int sid)
		{
			int temp;
			AL.GetSource(sid, ALGetSourcei.SourceState, out temp);
			return (ALSourceState)temp;
		}

		[CLSCompliant(false)]
		public static ALSourceType GetSourceType(uint sid)
		{
			int temp;
			AL.GetSource(sid, ALGetSourcei.SourceType, out temp);
			return (ALSourceType)temp;
		}

		public static ALSourceType GetSourceType(int sid)
		{
			int temp;
			AL.GetSource(sid, ALGetSourcei.SourceType, out temp);
			return (ALSourceType)temp;
		}

		public static ALDistanceModel GetDistanceModel()
		{
			return (ALDistanceModel)AL.Get(ALGetInteger.DistanceModel);
		}

		#endregion Helpers
	}
}

#pragma warning restore 3021
