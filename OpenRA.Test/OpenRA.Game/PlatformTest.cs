#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class PlatformTest
	{
		string supportDir;
		string gameDir;

		[SetUp]
		public void SetUp()
		{
			supportDir = Platform.SupportDir;
			gameDir = Platform.GameDir;
		}

		[TestCase(TestName = "Returns literal paths")]
		public void ResolvePath()
		{
			Assert.That(Platform.ResolvePath("^testpath"),
				Is.EqualTo(Path.Combine(supportDir, "testpath")));

			Assert.That(Platform.ResolvePath(".\\testpath"),
				Is.EqualTo(Path.Combine(gameDir, "testpath")));

			Assert.That(Platform.ResolvePath("./testpath"),
				Is.EqualTo(Path.Combine(gameDir, "testpath")));

			Assert.That(Platform.ResolvePath("testpath"),
				Is.EqualTo("testpath"));
		}

		[TestCase(TestName = "Returns encoded paths")]
		public void UnresolvePath()
		{
			Assert.That(Platform.UnresolvePath(Path.Combine(supportDir, "testpath")),
				Is.EqualTo("^testpath"));

			Assert.That(Platform.UnresolvePath(Path.Combine(gameDir, "testpath")),
				Is.EqualTo("./testpath"));

			Assert.That(Platform.UnresolvePath("testpath"),
				Is.EqualTo("testpath"));
		}
	}
}
