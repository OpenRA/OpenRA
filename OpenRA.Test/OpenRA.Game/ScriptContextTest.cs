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
using System.Reflection;
using System.Runtime.CompilerServices;
using Eluant;
using NUnit.Framework;
using OpenRA.Scripting;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class ScriptContextTest
	{
		ScriptContext context;
		MemoryConstrainedLuaRuntime runtime;
		Queue<Action<World>> frameEndActions;

		[SetUp]
		public void SetUp()
		{
			// Exercise the real Lua runtime without loading mod assets or constructing a renderer.
			runtime = new MemoryConstrainedLuaRuntime();
			context = (ScriptContext)RuntimeHelpers.GetUninitializedObject(typeof(ScriptContext));
			SetField(context, "runtime", runtime);
			var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
			frameEndActions = new Queue<Action<World>>();
			SetField(world, "frameEndActions", frameEndActions);
			SetField(context, "<World>k__BackingField", world);
			Log.AddChannel("lua", null);
		}

		static void SetField(object target, string name, object value)
		{
			target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
		}

		[TearDown]
		public void TearDown()
		{
			context.Dispose();
		}

		[Test]
		public void PassesPlainValuesAndDiscardsReturnValues()
		{
			runtime.DoString("function Choose(a,b,c,d,e) text=a; number=b+c; flag=d; empty=e==nil; return {}, function() end end").Dispose();
			Assert.That(context.TryInvokeGlobal("Choose", "campaign", 2, 0.5, true, null), Is.True);
			using var text = runtime.Globals["text"];
			using var number = runtime.Globals["number"];
			using var flag = runtime.Globals["flag"];
			using var empty = runtime.Globals["empty"];
			Assert.That(text.ToString(), Is.EqualTo("campaign"));
			Assert.That(number.ToNumber(), Is.EqualTo(2.5));
			Assert.That(flag.ToBoolean(), Is.True);
			Assert.That(empty.ToBoolean(), Is.True);
		}

		[Test]
		public void WorldLoadedUsesTheSameInvocationPath()
		{
			context.WorldLoaded();
			runtime.DoString("function WorldLoaded() loaded=true end").Dispose();
			context.WorldLoaded();
			using var loaded = runtime.Globals["loaded"];
			Assert.That(loaded.ToBoolean(), Is.True);
			context.Dispose();
			Assert.DoesNotThrow(context.WorldLoaded);
		}

		[Test]
		public void RejectsInvalidInvocationArguments()
		{
			Assert.Throws<ArgumentException>(() => context.TryInvokeGlobal(""));
			Assert.Throws<ArgumentNullException>(() => context.TryInvokeGlobal("Choose", null));
		}

		[Test]
		public void LooksUpReplacementFunctionOnEveryCall()
		{
			runtime.DoString("count=0; function Choose() count=count+1; Choose=function() count=count+10 end end").Dispose();
			Assert.That(context.TryInvokeGlobal("Choose"), Is.True);
			Assert.That(context.TryInvokeGlobal("Choose"), Is.True);
			using var count = runtime.Globals["count"];
			Assert.That(count.ToNumber(), Is.EqualTo(11));
		}

		[TestCase("nil")]
		[TestCase("3")]
		[TestCase("{}")]
		public void MissingOrNonFunctionGlobalIsOptional(string value)
		{
			runtime.DoString("Choose=" + value).Dispose();
			Assert.That(context.TryInvokeGlobal("Choose"), Is.False);
			Assert.That(context.FatalErrorOccurred, Is.False);
		}

		[Test]
		public void CanInvokeAfterGameOverWithoutTickingTheWorld()
		{
			SetField(context.World, "<IsGameOver>k__BackingField", true);
			runtime.DoString("function Choose() called=true end").Dispose();
			Assert.That(context.TryInvokeGlobal("Choose"), Is.True);
			using var called = runtime.Globals["called"];
			Assert.That(called.ToBoolean(), Is.True);
			Assert.That(context.World.IsGameOver, Is.True);
			Assert.That(frameEndActions, Is.Empty);
		}

		[Test]
		public void DoesNotCallDisposedRuntime()
		{
			context.Dispose();
			Assert.That(context.TryInvokeGlobal("Choose"), Is.False);
		}

		[Test]
		public void LuaErrorUsesExistingFatalErrorPolicy()
		{
			runtime.DoString("function Choose() error('selection failed') end").Dispose();
			Assert.That(context.TryInvokeGlobal("Choose"), Is.False);
			Assert.That(context.FatalErrorOccurred, Is.True);
			Assert.That(context.ErrorMessage, Does.Contain("selection failed"));
			Assert.That(frameEndActions, Has.Count.EqualTo(1));
			Assert.That(context.TryInvokeGlobal("Choose"), Is.False);
			Assert.That(frameEndActions, Has.Count.EqualTo(1));
		}

		[Test]
		public void RejectsCallerOwnedLuaHandlesWithoutDisposingThem()
		{
			runtime.DoString("function Choose() called=true end").Dispose();
			using var table = runtime.CreateTable();
			Assert.Throws<ArgumentException>(() => context.TryInvokeGlobal("Choose", table));
			table["stillAlive"] = true;
			using var called = runtime.Globals["called"];
			Assert.That(called, Is.TypeOf<LuaNil>());
		}

		[Test]
		public void RepeatedCallsRemainUsableAfterGarbageCollection()
		{
			runtime.DoString("count=0; function Choose(s) count=count+1; return {}, function() return s end end").Dispose();
			for (var i = 0; i < 1000; i++)
				Assert.That(context.TryInvokeGlobal("Choose", "campaign"), Is.True);
			runtime.DoString("collectgarbage('collect')").Dispose();
			Assert.That(context.TryInvokeGlobal("Choose", "campaign"), Is.True);
			using var count = runtime.Globals["count"];
			Assert.That(count.ToNumber(), Is.EqualTo(1001));
		}
	}
}
