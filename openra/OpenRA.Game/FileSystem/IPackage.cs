#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileSystem
{
	public interface IPackageLoader
	{
		/// <summary>
		/// Attempt to parse a stream as this type of package.
		/// If successful, the loader is expected to take ownership of `s` and dispose it once done.
		/// If unsuccessful, the loader is expected to return the stream position to where it started.
		/// </summary>
		bool TryParsePackage(Stream s, string filename, FileSystem context, out IReadOnlyPackage package);
	}

	public interface IReadOnlyPackage : IDisposable
	{
		string Name { get; }
		IEnumerable<string> Contents { get; }
		Stream GetStream(string filename);
		bool Contains(string filename);
		IReadOnlyPackage OpenPackage(string filename, FileSystem context);
	}

	public interface IReadWritePackage : IReadOnlyPackage
	{
		void Update(string filename, byte[] contents);
		void Delete(string filename);
	}
}
