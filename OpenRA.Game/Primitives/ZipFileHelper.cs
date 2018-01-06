#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace OpenRA.Primitives
{
	public static class ZipFileHelper
	{
		/// <summary>
		/// Creates a <see cref="ZipFile"/> with UTF8 encoding. Avoid using <see cref="ZipFile(Stream)"/> as you
		/// cannot be sure of the encoding that will be used.
		/// </summary>
		public static ZipFile Create(Stream stream)
		{
			// SharpZipLib uses this global as the encoding to use for all ZipFiles.
			// The initial value is the system code page, which causes several problems.
			// 1) On some systems, the code page for a certain encoding might not even be installed.
			// 2) The code page is different on every system, resulting in unpredictability.
			// 3) The code page might not work for decoding some archives.
			// We set the default to UTF8 instead which fixes all these problems.
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
			return new ZipFile(stream);
		}

		/// <summary>
		/// Creates a <see cref="ZipFile"/> with UTF8 encoding. Avoid using <see cref="ZipFile(FileStream)"/> as you
		/// cannot be sure of the encoding that will be used.
		/// </summary>
		public static ZipFile Create(FileStream stream)
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
			return new ZipFile(stream);
		}
	}
}
