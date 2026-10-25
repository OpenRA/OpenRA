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
using Eluant;
using NUnit.Framework;
using OpenRA.Mods.Common.UtilityCommands.Documentation;
using OpenRA.Scripting;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class LuaValueTest
	{
		[TestCase(0.0)]
		[TestCase(1.25)]
		[TestCase(-0.5)]
		[TestCase((double)float.MaxValue)]
		[TestCase(double.NaN)]
		[TestCase(double.PositiveInfinity)]
		public void LuaNumberConvertsToSingle(double number)
		{
			using var value = new LuaNumber(number);
			Assert.That(value.TryGetClrValue<float>(out var actual), Is.True);
			Assert.That(actual, Is.EqualTo((float)number));
		}

		[Test]
		public void LuaNumberConvertsToNullableSingle()
		{
			using var value = new LuaNumber(1.25);
			Assert.That(value.TryGetClrValue<float?>(out var actual), Is.True);
			Assert.That(actual, Is.EqualTo(1.25f));
		}

		[TestCase(0f)]
		[TestCase(1.25f)]
		[TestCase(float.MaxValue)]
		public void SingleConvertsToLuaNumber(float number)
		{
			using var value = number.ToLuaValue(null);
			Assert.That(value, Is.TypeOf<LuaNumber>());
			Assert.That(value.ToNumber().Value, Is.EqualTo((double)number));
		}

		[TestCase(typeof(double))]
		[TestCase(typeof(int))]
		[TestCase(typeof(short))]
		[TestCase(typeof(byte))]
		public void ExistingNumericConversionsRetainTheirTypes(Type type)
		{
			using var value = new LuaNumber(1);
			Assert.That(value.TryGetClrValue(type, out var actual), Is.True);
			Assert.That(actual, Is.TypeOf(type));
			Assert.That(actual, Is.EqualTo(1));
		}

		[TestCase(typeof(float), "number")]
		[TestCase(typeof(float[]), "number[]")]
		[TestCase(typeof(float?), "number?")]
		public void SingleHasEmmyLuaType(Type type, string expected)
		{
			Assert.That(type.EmmyLuaString(nameof(SingleHasEmmyLuaType)), Is.EqualTo(expected));
		}
	}
}
