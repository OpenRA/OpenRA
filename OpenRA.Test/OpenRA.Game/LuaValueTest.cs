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
		sealed class SingleValue
		{
			public float Value { get; set; }
			public float? OptionalValue { get; set; }

			public float SetValue(float value = 0.5f) => Value = value;
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

		[TestCase("return setValue(0)", 0f)]
		[TestCase("return setValue(1.25)", 1.25f)]
		[TestCase("return setValue(-0.5)", -0.5f)]
		[TestCase("return setValue(3.4028234663852886e38)", float.MaxValue)]
		[TestCase("return setValue(0 / 0)", float.NaN)]
		[TestCase("return setValue(math.huge)", float.PositiveInfinity)]
		[TestCase("return setValue()", 0.5f)]
		public void SingleMethodCanBeCalledFromLua(string script, float expected)
		{
			using var runtime = new LuaRuntime();
			var target = new SingleValue();
			var wrapper = new ScriptMemberWrapper(null, target, typeof(SingleValue).GetMethod(nameof(SingleValue.SetValue)));
			using var function = wrapper.Get(runtime);
			runtime.Globals["setValue"] = function;

			using var result = runtime.DoString(script);
			Assert.That(result.Count, Is.EqualTo(1));
			using var value = result[0];
			Assert.That(value, Is.TypeOf<LuaNumber>());
			Assert.That(value.ToNumber().Value, Is.EqualTo((double)expected));
			Assert.That(target.Value, Is.EqualTo(expected));
		}

		[TestCase(nameof(SingleValue.Value))]
		[TestCase(nameof(SingleValue.OptionalValue))]
		public void SinglePropertyAcceptsNumbersOnly(string name)
		{
			var wrapper = new ScriptMemberWrapper(null, new SingleValue(), typeof(SingleValue).GetProperty(name));
			using var input = new LuaNumber(1.25);
			wrapper.Set(input);

			using var output = wrapper.Get(null);
			Assert.That(output, Is.TypeOf<LuaNumber>());
			Assert.That(output.TryGetClrValue<float?>(out var actual), Is.True);
			Assert.That(actual, Is.EqualTo(1.25f));

			using var invalid = new LuaString("2.5");
			Assert.That(() => wrapper.Set(invalid), Throws.TypeOf<LuaException>());
			using var unchanged = wrapper.Get(null);
			Assert.That(unchanged.ToNumber().Value, Is.EqualTo(1.25));
		}

		[Test]
		public void LuaTableConvertsToSingleArray()
		{
			using var runtime = new LuaRuntime();
			using var result = runtime.DoString("return { 1.25, -0.5, 0 }");
			using var table = result[0];

			Assert.That(table.TryGetClrValue<float[]>(out var actual), Is.True);
			Assert.That(actual, Is.EquivalentTo([1.25f, -0.5f, 0f]));
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
