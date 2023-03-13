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
using System.ComponentModel;
using System.Reflection;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class FacingInit : ValueActorInit<WAngle>, ISingleInstanceInit
	{
		public FacingInit(WAngle value)
			: base(value) { }
	}

	public class CreationActivityDelayInit : ValueActorInit<int>, ISingleInstanceInit
	{
		public CreationActivityDelayInit(int value)
			: base(value) { }
	}

	public class DynamicFacingInit : ValueActorInit<Func<WAngle>>, ISingleInstanceInit
	{
		public DynamicFacingInit(Func<WAngle> value)
			: base(value) { }
	}

	// Cannot use ValueInit because map.yaml is expected to use the numeric value instead of enum name
	public class SubCellInit : ActorInit, ISingleInstanceInit
	{
		readonly int value;
		public SubCellInit(SubCell value)
		{
			this.value = (int)value;
		}

		public virtual SubCell Value => (SubCell)value;

		public void Initialize(MiniYaml yaml)
		{
			Initialize((int)FieldLoader.GetValue(nameof(value), typeof(int), yaml.Value));
		}

		public void Initialize(int value)
		{
			GetType()
				.GetField(nameof(value), BindingFlags.NonPublic | BindingFlags.Instance)
				?.SetValue(this, value);
		}

		public override MiniYaml Save()
		{
			return new MiniYaml(FieldSaver.FormatValue(value));
		}
	}

	public class CenterPositionInit : ValueActorInit<WPos>, ISingleInstanceInit
	{
		public CenterPositionInit(WPos value)
			: base(value) { }
	}

	// Allows maps / transformations to specify the faction variant of an actor.
	public class FactionInit : ValueActorInit<string>, ISingleInstanceInit
	{
		public FactionInit(string value)
			: base(value) { }
	}

	public class EffectiveOwnerInit : ValueActorInit<Player>
	{
		public EffectiveOwnerInit(Player value)
			: base(value) { }
	}

	sealed class ActorInitLoader : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			return new ActorInitActorReference(value as string);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is ActorInitActorReference reference)
				return reference.InternalName;

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	[TypeConverter(typeof(ActorInitLoader))]
	public class ActorInitActorReference
	{
		public readonly string InternalName;
		readonly Actor actor;

		public ActorInitActorReference(Actor actor)
		{
			this.actor = actor;
		}

		public ActorInitActorReference(string internalName)
		{
			InternalName = internalName;
		}

		Actor InnerValue(World world)
		{
			if (actor != null)
				return actor;

			var sma = world.WorldActor.Trait<SpawnMapActors>();
			return sma.Actors[InternalName];
		}

		/// <summary>
		/// The lazy value may reference other actors that have not been created
		/// yet, so must not be resolved from the actor constructor or Created method.
		/// Use a FrameEndTask or wait until it is actually needed.
		/// </summary>
		public Lazy<Actor> Actor(World world)
		{
			return new Lazy<Actor>(() => InnerValue(world));
		}

		public static implicit operator ActorInitActorReference(Actor a)
		{
			return new ActorInitActorReference(a);
		}

		public static implicit operator ActorInitActorReference(string mapName)
		{
			return new ActorInitActorReference(mapName);
		}
	}
}
