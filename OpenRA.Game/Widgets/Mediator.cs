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

using OpenRA.Primitives;

namespace OpenRA.Widgets
{
	public sealed class Mediator
	{
		readonly TypeDictionary types = new();

		public void Subscribe<T>(T instance)
		{
			types.Add(instance);
		}

		public void Unsubscribe<T>(T instance)
		{
			types.Remove(instance);
		}

		public void Send<T>(T notification)
		{
			var handlers = types.WithInterface<INotificationHandler<T>>();

			foreach (var handler in handlers)
				handler.Handle(notification);
		}
	}

	public interface INotificationHandler<T>
	{
		void Handle(T notification);
	}
}
