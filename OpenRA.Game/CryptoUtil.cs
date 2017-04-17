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
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OpenRA
{
	public static class CryptoUtil
	{
		public static string SHA1Hash(Stream data)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());
		}

		public static string SHA1Hash(byte[] data)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());
		}

		public static string SHA1Hash(string data)
		{
			return SHA1Hash(Encoding.UTF8.GetBytes(data));
		}
	}
}
