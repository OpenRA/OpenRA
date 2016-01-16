#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.FileSystem
{
	public interface IReadOnlyPackage : IDisposable
	{
		Stream GetContent(string filename);
		bool Exists(string filename);
		IEnumerable<uint> ClassicHashes();
		IEnumerable<uint> CrcHashes();
		IEnumerable<string> AllFileNames();
		int Priority { get; }
		string Name { get; }
	}

	public interface IReadWritePackage : IReadOnlyPackage
	{
		void Write(Dictionary<string, byte[]> contents);
	}
}
