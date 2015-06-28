#region --- OpenTK.OpenAL License ---
/* AlcFunctions.cs
 * C header: \OpenAL 1.1 SDK\include\Alc.h
 * Spec: http://www.openal.org/openal_webstf/specs/OpenAL11Specification.pdf
 * Copyright (c) 2008 Christoph Brandtner and Stefanos Apostolopoulos
 * See license.txt for license details
 * http://www.OpenTK.net */
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

#pragma warning disable 3021

namespace OpenTK.Audio.OpenAL
{
	public static class Alc
	{
		#region Constants

		private const string Lib = AL.Lib;
		private const CallingConvention Style = CallingConvention.Cdecl;

		#endregion Constants

		#region Context Management

		#region CreateContext

		[DllImport(Alc.Lib, EntryPoint = "alcCreateContext", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity]
		unsafe static extern IntPtr sys_CreateContext([In] IntPtr device, [In] int* attrlist);

		[CLSCompliant(false)]
		unsafe public static ContextHandle CreateContext([In] IntPtr device, [In] int* attrlist)
		{
			return new ContextHandle(sys_CreateContext(device, attrlist));
		}

		public static ContextHandle CreateContext(IntPtr device, int[] attriblist)
		{
			unsafe
			{
				fixed (int* attriblist_ptr = attriblist)
				{
					return CreateContext(device, attriblist_ptr);
				}
			}
		}

		#endregion

		[DllImport(Alc.Lib, EntryPoint = "alcMakeContextCurrent", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		static extern bool MakeContextCurrent(IntPtr context);

		public static bool MakeContextCurrent(ContextHandle context)
		{
			return MakeContextCurrent(context.Handle);
		}

		[DllImport(Alc.Lib, EntryPoint = "alcProcessContext", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		static extern void ProcessContext(IntPtr context);

		public static void ProcessContext(ContextHandle context)
		{
			ProcessContext(context.Handle);
		}

		[DllImport(Alc.Lib, EntryPoint = "alcSuspendContext", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		static extern void SuspendContext(IntPtr context);

		public static void SuspendContext(ContextHandle context)
		{
			SuspendContext(context.Handle);
		}

		[DllImport(Alc.Lib, EntryPoint = "alcDestroyContext", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		static extern void DestroyContext(IntPtr context);

		public static void DestroyContext(ContextHandle context)
		{
			DestroyContext(context.Handle);
		}

		[DllImport(Alc.Lib, EntryPoint = "alcGetCurrentContext", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		private static extern IntPtr sys_GetCurrentContext();

		public static ContextHandle GetCurrentContext()
		{
			return new ContextHandle(sys_GetCurrentContext());
		}

		[DllImport(Alc.Lib, EntryPoint = "alcGetContextsDevice", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		static extern IntPtr GetContextsDevice(IntPtr context);

		public static IntPtr GetContextsDevice(ContextHandle context)
		{
			return GetContextsDevice(context.Handle);
		}

		#endregion Context Management

		#region Device Management

		[DllImport(Alc.Lib, EntryPoint = "alcOpenDevice", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern IntPtr OpenDevice([In] string devicename);

		[DllImport(Alc.Lib, EntryPoint = "alcCloseDevice", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		public static extern bool CloseDevice([In] IntPtr device);

		#endregion Device Management

		#region Error support.

		[DllImport(Alc.Lib, EntryPoint = "alcGetError", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		public static extern AlcError GetError([In] IntPtr device);

		#endregion Error support.

		#region Extension support.

		[DllImport(Alc.Lib, EntryPoint = "alcIsExtensionPresent", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern bool IsExtensionPresent([In] IntPtr device, [In] string extname);

		[DllImport(Alc.Lib, EntryPoint = "alcGetProcAddress", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern IntPtr GetProcAddress([In] IntPtr device, [In] string funcname);

		[DllImport(Alc.Lib, EntryPoint = "alcGetEnumValue", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern int GetEnumValue([In] IntPtr device, [In] string enumname);

		#endregion Extension support.

		#region Query functions

		[DllImport(Alc.Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		private static extern IntPtr GetStringPrivate([In] IntPtr device, AlcGetString param);

		public static string GetString(IntPtr device, AlcGetString param)
		{
			return Marshal.PtrToStringAnsi(GetStringPrivate(device, param));
		}

		public static IList<string> GetString(IntPtr device, AlcGetStringList param)
		{
			List<string> result = new List<string>();
			IntPtr t = GetStringPrivate(IntPtr.Zero, (AlcGetString)param);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			byte b;
			int offset = 0;
			do
			{
				b = Marshal.ReadByte(t, offset++);
				if (b != 0)
					sb.Append((char)b);
				if (b == 0)
				{
					result.Add(sb.ToString());
					if (Marshal.ReadByte(t, offset) == 0) // offset already properly increased through ++
						break; // 2x null
					else
						sb.Remove(0, sb.Length); // 1x null
				}
			} while (true);

			return (IList<string>)result;
		}

		[DllImport(Alc.Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		unsafe static extern void GetInteger(IntPtr device, AlcGetInteger param, int size, int* data);

		public static void GetInteger(IntPtr device, AlcGetInteger param, int size, out int data)
		{
			unsafe
			{
				fixed (int* data_ptr = &data)
				{
					GetInteger(device, param, size, data_ptr);
				}
			}
		}

		public static void GetInteger(IntPtr device, AlcGetInteger param, int size, int[] data)
		{
			unsafe
			{
				fixed (int* data_ptr = data)
				{
					GetInteger(device, param, size, data_ptr);
				}
			}
		}

		#endregion Query functions

		#region Capture functions

		[CLSCompliant(false), DllImport(Alc.Lib, EntryPoint = "alcCaptureOpenDevice", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern IntPtr CaptureOpenDevice(string devicename, uint frequency, ALFormat format, int buffersize);

		[DllImport(Alc.Lib, EntryPoint = "alcCaptureOpenDevice", ExactSpelling = true, CallingConvention = Alc.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity()]
		public static extern IntPtr CaptureOpenDevice(string devicename, int frequency, ALFormat format, int buffersize);


		[DllImport(Alc.Lib, EntryPoint = "alcCaptureCloseDevice", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		public static extern bool CaptureCloseDevice([In] IntPtr device);

		[DllImport(Alc.Lib, EntryPoint = "alcCaptureStart", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void CaptureStart([In] IntPtr device);

		[DllImport(Alc.Lib, EntryPoint = "alcCaptureStop", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void CaptureStop([In] IntPtr device);

		[DllImport(Alc.Lib, EntryPoint = "alcCaptureSamples", ExactSpelling = true, CallingConvention = Alc.Style), SuppressUnmanagedCodeSecurity()]
		public static extern void CaptureSamples(IntPtr device, IntPtr buffer, int samples);

		public static void CaptureSamples<T>(IntPtr device, ref T buffer, int samples)
			where T : struct
		{
			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try
			{
				CaptureSamples(device, handle.AddrOfPinnedObject(), samples);
			}
			finally
			{
				handle.Free();
			}
		}

		public static void CaptureSamples<T>(IntPtr device, T[] buffer, int samples)
			where T : struct
		{
			CaptureSamples(device, ref buffer[0], samples);
		}

		public static void CaptureSamples<T>(IntPtr device, T[,] buffer, int samples)
			where T : struct
		{
			CaptureSamples(device, ref buffer[0, 0], samples);
		}

		public static void CaptureSamples<T>(IntPtr device, T[, ,] buffer, int samples)
			where T : struct
		{
			CaptureSamples(device, ref buffer[0, 0, 0], samples);
		}

		#endregion Capture functions

	}

}

#pragma warning restore 3021