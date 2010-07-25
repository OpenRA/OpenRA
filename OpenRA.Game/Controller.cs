#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA
{
	public class Controller
	{
		public IOrderGenerator orderGenerator = new UnitOrderGenerator();
		public Selection selection = new Selection();

		public void CancelInputMode() { orderGenerator = new UnitOrderGenerator(); }

		public bool ToggleInputMode<T>() where T : IOrderGenerator, new()
		{
			if (orderGenerator is T)
			{
				CancelInputMode();
				return false;
			}
			else
			{
				orderGenerator = new T();
				return true;
			}
		}

		public void ApplyOrders(World world, float2 xy, MouseInput mi)
		{
			if (orderGenerator == null) return;

			var orders = orderGenerator.Order(world, xy.ToInt2(), mi).ToArray();
			Game.orderManager.IssueOrders( orders );
			
			// Find an actor with a phrase to say
			var done = false;
			foreach (var o in orders)
			{
				foreach (var v in o.Subject.traits.WithInterface<IOrderVoice>())
				{
					if (Sound.PlayVoice(v.VoicePhraseForOrder(o.Subject, o), o.Subject))
					{
						done = true;
						break;
					}
				}
				if (done) break;
			}
		}

		public float2 dragStart, dragEnd;
		public Pair<float2, float2>? SelectionBox
		{
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(Game.CellSize * dragStart, Game.CellSize * dragEnd);
			}
		}

		public float2 MousePosition { get { return dragEnd; } }
		Modifiers modifiers;

		public void SetModifiers(Modifiers mods) { modifiers = mods; }
		public Modifiers GetModifiers() { return modifiers; }
	}
}
