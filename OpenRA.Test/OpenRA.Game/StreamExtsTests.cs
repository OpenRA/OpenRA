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

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	class StreamExtsTests
	{
		[TestCase(TestName = "ReadAllLines is equivalent to ReadAllLinesAsMemory")]
		public void ReadAllLines()
		{
			foreach (var source in new[]
			{
				"abc",
				"abc\n",
				"abc\r\n",
				"abc\ndef",
				"abc\r\ndef",
				"abc\n\n\n",
				"abc\r\n\r\n\r\n",
				"abc\n\n\ndef",
				"abc\r\n\r\n\r\ndef",
				"abc\ndef\nghi\n",
				"abc\r\ndef\r\nghi\r\n",
				"abc\ndef\nghi\njkl",
				"abc\r\ndef\r\nghi\r\njkl",
				new string('a', 126),
				new string('a', 126) + '\n',
				new string('a', 126) + "\r\n",
				new string('a', 126) + "b",
				new string('a', 126) + "\nb",
				new string('a', 126) + "\r\nb",
				new string('a', 127),
				new string('a', 127) + '\n',
				new string('a', 127) + "\r\n",
				new string('a', 127) + "b",
				new string('a', 127) + "\nb",
				new string('a', 127) + "\r\nb",
				new string('a', 128),
				new string('a', 128) + '\n',
				new string('a', 128) + "\r\n",
				new string('a', 128) + "b",
				new string('a', 128) + "\nb",
				new string('a', 128) + "\r\nb",
				new string('a', 129),
				new string('a', 129) + '\n',
				new string('a', 129) + "\r\n",
				new string('a', 129) + "b",
				new string('a', 129) + "\nb",
				new string('a', 129) + "\r\nb",
			})
			{
				var bytes = Encoding.UTF8.GetBytes(source);
				var lines = new MemoryStream(bytes).ReadAllLines().ToArray();
				var linesAsMemory = new MemoryStream(bytes).ReadAllLinesAsMemory().Select(l => l.ToString()).ToArray();
				Assert.That(linesAsMemory, Is.EquivalentTo(lines));
			}
		}
	}
}
