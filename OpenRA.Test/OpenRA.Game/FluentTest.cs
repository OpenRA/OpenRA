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
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class FluentTest
	{
		readonly string pluralForms = @"
label-players = {$player ->
    [one] One player
   *[other] {$player} players
}
";

		[TestCase(TestName = "Fluent Plural Terms")]
		public void TestOne()
		{
			var translation = new Translation("en", pluralForms, e => Console.WriteLine(e.Message));
			var label = translation.GetString("label-players", Translation.Arguments("player", 1));
			Assert.That("One player", Is.EqualTo(label));
			label = translation.GetString("label-players", Translation.Arguments("player", 2));
			Assert.That("2 players", Is.EqualTo(label));
		}
	}
}
