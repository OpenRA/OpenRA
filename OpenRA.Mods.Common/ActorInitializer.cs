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
	public class FacingInit(WAngle value) : ValueActorInit<WAngle>(value), ISingleInstanceInit
	{
	}

	public class TerrainOrientationInit(WRot value) : ValueActorInit<WRot>(value), ISingleInstanceInit, ISuppressInitExport
	{
	}

	public class CreationActivityDelayInit(int value) : ValueActorInit<int>(value), ISingleInstanceInit
	{
	}

	public class DynamicFacingInit(Func<WAngle> value) : ValueActorInit<Func<WAngle>>(value), ISingleInstanceInit
	{
	}

	// Cannot use ValueInit because map.yaml is expected to use the numeric value instead of enum name
	public class SubCellInit(SubCell value) : ActorInit, ISingleInstanceInit
	{
		readonly int value = (int)value;

		public virtual SubCell Value => (SubCell)value;

		public void Initialize(MiniYaml yaml)
		{
			Initialize(FieldLoader.GetValue<int>(nameof(value), yaml.Value));
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

	public class CenterPositionInit(WPos value) : ValueActorInit<WPos>(value), ISingleInstanceInit
	{
	}

	// Allows maps / transformations to specify the faction variant of an actor.
	public class FactionInit(string value) : ValueActorInit<string>(value), ISingleInstanceInit
	{
	}

	public class EffectiveOwnerInit(Player value) : ValueActorInit<Player>(value)
	{
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
