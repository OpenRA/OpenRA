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
	public interface IFolder : IDisposable
	{
		Stream GetContent(string filename);
		bool Exists(string filename);
		IEnumerable<uint> ClassicHashes();
		IEnumerable<uint> CrcHashes();
		IEnumerable<string> AllFileNames();
		void Write(Dictionary<string, byte[]> contents);
		int Priority { get; }
		string Name { get; }
	}
}
