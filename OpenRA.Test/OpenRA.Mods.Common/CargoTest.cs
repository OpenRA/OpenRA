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
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Test
{
	sealed class CargoTestFacing : IFacing
	{
		public WAngle TurnSpeed => WAngle.Zero;
		public WAngle Facing { get; set; }
		public WRot Orientation => WRot.None;
	}

	sealed class CargoTestObserver(List<string> events) : INotifyPassengerExited, INotifyExitedCargo
	{
		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger) { events.Add("transport-exit"); }
		void INotifyExitedCargo.OnExitedCargo(Actor self, Actor cargo) { events.Add("passenger-exit"); }
	}

	[TestFixture]
	sealed class CargoTest
	{
		const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;
		World world;
		Queue<Action<World>> frameEndActions;
		Actor transport;
		Actor passenger;
		Cargo cargo;
		List<string> events;

		Actor CreateActor(string name, params TraitInfo[] infos)
		{
			var actor = (Actor)typeof(Actor).GetConstructor(InstanceFields, null,
				[typeof(World), typeof(string), typeof(TypeDictionary)], null).Invoke([world, null, new TypeDictionary()]);
			typeof(Actor).GetField(nameof(Actor.Info)).SetValue(actor, new ActorInfo(name, infos));
			return actor;
		}

		void FlushFrameEndTasks()
		{
			while (frameEndActions.TryDequeue(out var action))
				action(world);
		}

		static int ConditionCount(Actor actor, string name)
		{
			var conditions = (Dictionary<string, int>)typeof(Actor).GetField("conditionCache", InstanceFields).GetValue(actor);
			return conditions.GetValueOrDefault(name);
		}

		[SetUp]
		public void SetUp()
		{
			// Run real actor/trait bookkeeping without a renderer or copyrighted game assets.
			world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
			var traitDict = typeof(World).GetField("TraitDict", InstanceFields);
			traitDict.SetValue(world, Activator.CreateInstance(traitDict.FieldType, true));
			frameEndActions = new Queue<Action<World>>();
			typeof(World).GetField("frameEndActions", InstanceFields).SetValue(world, frameEndActions);
			events = [];

			var cargoInfo = new CargoInfo();
			FieldLoader.LoadFieldOrProperty(cargoInfo, nameof(CargoInfo.MaxWeight), "4");
			FieldLoader.LoadFieldOrProperty(cargoInfo, nameof(CargoInfo.LoadedCondition), "loaded");
			FieldLoader.Load(cargoInfo, new MiniYaml(null,
			[
				new MiniYamlNode(nameof(CargoInfo.PassengerConditions), null, [new MiniYamlNode("infantry", "infantry-loaded")])
			]));
			transport = CreateActor("transport", cargoInfo);
			transport.AddTrait(new CargoTestFacing { Facing = new WAngle(100) });
			transport.AddTrait(new CargoTestObserver(events));
			cargo = new Cargo(new ActorInitializer(transport, []), cargoInfo);
			transport.AddTrait(cargo);
			((INotifyCreated)cargo).Created(transport);
			FlushFrameEndTasks();
			passenger = CreatePassenger();
			cargo.Load(transport, passenger);
		}

		Actor CreatePassenger()
		{
			var info = new PassengerInfo();
			FieldLoader.LoadFieldOrProperty(info, nameof(PassengerInfo.Weight), "2");
			FieldLoader.LoadFieldOrProperty(info, nameof(PassengerInfo.CargoCondition), "in-cargo");
			var actor = CreateActor("infantry", info);
			actor.AddTrait(new Passenger(info));
			actor.AddTrait(new CargoTestFacing { Facing = new WAngle(200) });
			actor.AddTrait(new CargoTestObserver(events));
			return actor;
		}

		[Test]
		public void RemoveReleasesCapacityConditionsAndTransportWithoutExit()
		{
			Assert.That(cargo.HasSpace(3), Is.False);
			Assert.That(ConditionCount(transport, "loaded"), Is.EqualTo(1));
			Assert.That(ConditionCount(transport, "infantry-loaded"), Is.EqualTo(1));
			Assert.That(passenger.Trait<Passenger>().Transport, Is.SameAs(transport));
			Assert.That(cargo.RemovePassenger(transport, passenger), Is.SameAs(passenger));
			Assert.That(cargo.IsEmpty(), Is.True);
			Assert.That(cargo.HasSpace(4), Is.True);
			Assert.That(ConditionCount(transport, "loaded"), Is.Zero);
			Assert.That(ConditionCount(transport, "infantry-loaded"), Is.Zero);
			Assert.That(passenger.Trait<Passenger>().Transport, Is.Null);
			Assert.That(passenger.Trait<IFacing>().Facing, Is.EqualTo(new WAngle(200)));
			Assert.That(passenger.IsInWorld, Is.False);
			Assert.That(passenger.WillDispose, Is.False);
			Assert.That(events, Is.Empty);
			Assert.That(ConditionCount(passenger, "in-cargo"), Is.EqualTo(1));
		}

		[Test]
		public void RemoveOnePassengerPreservesTheOther()
		{
			var other = CreatePassenger();
			cargo.Load(transport, other);
			cargo.RemovePassenger(transport, passenger);
			Assert.That(cargo.Passengers, Is.EqualTo(new[] { other }));
			Assert.That(cargo.HasSpace(3), Is.False);
			Assert.That(ConditionCount(transport, "loaded"), Is.EqualTo(1));
			Assert.That(ConditionCount(transport, "infantry-loaded"), Is.EqualTo(1));
			Assert.That(other.Trait<Passenger>().Transport, Is.SameAs(transport));
			cargo.Unload(transport, other);
			Assert.That(cargo.HasSpace(4), Is.True);
			Assert.That(ConditionCount(transport, "loaded"), Is.Zero);
			Assert.That(ConditionCount(transport, "infantry-loaded"), Is.Zero);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void OrdinaryUnloadKeepsFacingAndExitNotifications(bool implicitPassenger)
		{
			var unloaded = implicitPassenger ? cargo.Unload(transport) : cargo.Unload(transport, passenger);
			Assert.That(unloaded, Is.SameAs(passenger));
			Assert.That(events, Is.EqualTo(new[] { "transport-exit", "passenger-exit" }));
			Assert.That(passenger.Trait<IFacing>().Facing, Is.EqualTo(new WAngle(612)));
			Assert.That(passenger.Trait<Passenger>().Transport, Is.Null);
			Assert.That(ConditionCount(passenger, "in-cargo"), Is.Zero);
			Assert.That(ConditionCount(transport, "loaded"), Is.Zero);
			Assert.That(ConditionCount(transport, "infantry-loaded"), Is.Zero);
			Assert.That(cargo.HasSpace(4), Is.True);
		}

		[Test]
		public void InvalidPassengerDoesNotMutateCargo()
		{
			Assert.Throws<ArgumentException>(() => cargo.RemovePassenger(transport, CreatePassenger()));
			Assert.That(cargo.Passengers, Is.EqualTo(new[] { passenger }));
			Assert.That(ConditionCount(transport, "loaded"), Is.EqualTo(1));
			Assert.That(cargo.HasSpace(3), Is.False);
			Assert.That(events, Is.Empty);
		}

		[Test]
		public void NullDoesNotImplicitlyRemoveTheLastPassenger()
		{
			Assert.Throws<ArgumentNullException>(() => cargo.RemovePassenger(transport, null));
			Assert.That(cargo.Passengers, Is.EqualTo(new[] { passenger }));
		}

		[Test]
		public void RepeatedRemovalIsRejectedWithoutDoubleRevocation()
		{
			cargo.RemovePassenger(transport, passenger);
			Assert.Throws<ArgumentException>(() => cargo.RemovePassenger(transport, passenger));
			Assert.That(cargo.HasSpace(4), Is.True);
			Assert.That(ConditionCount(transport, "loaded"), Is.Zero);
			Assert.That(events, Is.Empty);
		}

		[Test]
		public void CallerCanDisposeRemovedPassengerBeforeTransport()
		{
			cargo.RemovePassenger(transport, passenger).Dispose();
			FlushFrameEndTasks();
			Assert.That(passenger.Disposed, Is.True);
			transport.Dispose();
			FlushFrameEndTasks();
			Assert.That(transport.Disposed, Is.True);
			Assert.That(events, Is.Empty);
		}
	}
}
