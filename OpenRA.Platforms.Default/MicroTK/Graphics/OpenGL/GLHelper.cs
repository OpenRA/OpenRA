#region --- License ---
/* Copyright (c) 2006-2008 the OpenTK team.
 * See license.txt for license info
 * 
 * Contributions by Andy Gill.
 */
#endregion

#region --- Using Directives ---

using System;
using System.Collections.Generic;
#if !MINIMAL
using System.Drawing;
#endif
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Emit;


#endregion

namespace OpenTK.Graphics.OpenGL
{
	/// <summary>
	/// OpenGL bindings for .NET, implementing the full OpenGL API, including extensions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class contains all OpenGL enums and functions defined in the latest OpenGL specification.
	/// The official .spec files can be found at: http://opengl.org/registry/.
	/// </para>
	/// <para> A valid OpenGL context must be created before calling any OpenGL function.</para>
	/// <para>
	/// Use the GL.Load and GL.LoadAll methods to prepare function entry points prior to use. To maintain
	/// cross-platform compatibility, this must be done for both core and extension functions. The GameWindow
	/// and the GLControl class will take care of this automatically.
	/// </para>
	/// <para>
	/// You can use the GL.SupportsExtension method to check whether any given category of extension functions
	/// exists in the current OpenGL context. Keep in mind that different OpenGL contexts may support different
	/// extensions, and under different entry points. Always check if all required extensions are still supported
	/// when changing visuals or pixel formats.
	/// </para>
	/// <para>
	/// You may retrieve the entry point for an OpenGL function using the GL.GetDelegate method.
	/// </para>
	/// </remarks>
	/// <see href="http://opengl.org/registry/"/>
	public sealed partial class GL : GraphicsBindingsBase
	{
		#region --- Fields ---

		internal const string Library = "opengl32.dll";

		// flibit commented this out.
		// static SortedList<string, bool> AvailableExtensions = new SortedList<string, bool>();
		static readonly object sync_root = new object();

		#endregion

		#region --- Constructor ---

		static GL()
		{
		}

		#endregion

		#region --- Public Members ---

		// flibit un-Obsoleted this.
		/// <summary>
		/// Loads all OpenGL entry points (core and extension).
		/// This method is provided for compatibility purposes with older OpenTK versions.
		/// </summary>
		//[Obsolete("If you are using a context constructed outside of OpenTK, create a new GraphicsContext and pass your context handle to it. Otherwise, there is no need to call this method.")]
		public static void LoadAll()
		{
			new GL().LoadEntryPoints();
		}

		#endregion

		#region --- Protected Members ---

		/// <summary>
		/// Returns a synchronization token unique for the GL class.
		/// </summary>
		protected override object SyncRoot
		{
			get { return sync_root; }
		}

		#endregion

		#region --- GL Overloads ---

		#region public static void ShaderSource(Int32 shader, System.String @string)

		public static void ShaderSource(Int32 shader, System.String @string)
		{
			unsafe
			{
				int length = @string.Length;
				GL.ShaderSource((UInt32)shader, 1, new string[] { @string }, &length);
			}
		}

		#endregion

		#endregion
	}

}