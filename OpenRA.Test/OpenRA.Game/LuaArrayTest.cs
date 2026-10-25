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

using System.Collections.Generic;
using Eluant;
using NUnit.Framework;
using OpenRA.Scripting;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class LuaArrayTest
	{
		[TestCase("return {296,7,12,1,2,3,3,4,5,6,6}")]
		[TestCase("return {[1]=296,[2]=7,[3]=12,[4]=1,[5]=2,[6]=3,[7]=3,[8]=4,[9]=5,[10]=6,[11]=6}")]
		[TestCase("local t = {}; t[11]=6; t[3]=12; t[1]=296; t[8]=4; t[2]=7; t[4]=1; t[5]=2; t[6]=3; t[7]=3; t[9]=5; t[10]=6; return t")]
		public void DenseSequencesUseNumericKeyOrder(string expression)
		{
			using var runtime = new LuaRuntime();
			using var returned = runtime.DoString(expression);
			Assert.That(returned[0].TryGetClrValue<int[]>(out var actual), Is.True);
			Assert.That(actual, Is.EqualTo(new[] { 296, 7, 12, 1, 2, 3, 3, 4, 5, 6, 6 }));
		}

		[TestCase("return {a=7,b=12}")]
		[TestCase("return {[2]=7,[5]=12}")]
		[TestCase("return {[1]=7,extra=12}")]
		[TestCase("return {[0]=7,[1]=12}")]
		[TestCase("return {[-1]=7,[1]=12}")]
		[TestCase("return {[1.5]=7,[2]=12}")]
		[TestCase("return setmetatable({named=7}, {__index=function() return 99 end})")]
		public void OtherTablesRetainIterationOrder(string expression)
		{
			using var runtime = new LuaRuntime();
			using var returned = runtime.DoString(expression);
			var table = (LuaTable)returned[0];
			var expected = new List<int>();
			foreach (var pair in table)
				using (pair.Key)
				using (pair.Value)
					expected.Add((int)pair.Value.ToNumber().Value);

			Assert.That(table.TryGetClrValue<int[]>(out var actual), Is.True);
			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void EmptyTableConvertsToEmptyArray()
		{
			using var runtime = new LuaRuntime();
			using var returned = runtime.DoString("return {}");
			Assert.That(returned[0].TryGetClrValue<int[]>(out var actual), Is.True);
			Assert.That(actual, Is.Empty);
		}

		[Test]
		public void StringSequenceUsesNumericKeyOrder()
		{
			using var runtime = new LuaRuntime();
			using var returned = runtime.DoString("return {[2]='second',[1]='first'}");
			Assert.That(returned[0].TryGetClrValue<string[]>(out var actual), Is.True);
			Assert.That(actual, Is.EqualTo(new[] { "first", "second" }));
		}

		[Test]
		public void LuaValueArrayElementsRemainUsable()
		{
			using var runtime = new LuaRuntime();
			using var returned = runtime.DoString("return {[2]={label='second'},[1]={label='first'}}");
			Assert.That(returned[0].TryGetClrValue<LuaValue[]>(out var actual), Is.True);
			try
			{
				using var first = ((LuaTable)actual[0])["label"];
				using var second = ((LuaTable)actual[1])["label"];
				Assert.That(first.ToString(), Is.EqualTo("first"));
				Assert.That(second.ToString(), Is.EqualTo("second"));
			}
			finally
			{
				foreach (var value in actual)
					value.Dispose();
			}
		}

		[Test]
		public void InvalidElementStillThrowsLuaException()
		{
			using var runtime = new LuaRuntime();
			using var returned = runtime.DoString("return {[1]=1,[2]='not a number'}");
			Assert.Throws<LuaException>(() => returned[0].TryGetClrValue<int[]>(out _));
		}
	}
}
