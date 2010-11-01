#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA
{
	public class NullInputHandler : IInputHandler
	{
		// ignore all input
		public void ModifierKeys( Modifiers mods ) { }
		public void OnKeyInput( KeyInput input ) { }
		public void OnMouseInput( MouseInput input ) { }
	}

	public class DefaultInputHandler : IInputHandler
	{
		readonly World world;
		public DefaultInputHandler( World world )
		{
			this.world = world;
		}

		public void ModifierKeys( Modifiers mods )
		{
			Game.HandleModifierKeys( mods );
		}

		public void OnKeyInput( KeyInput input )
		{
			Sync.CheckSyncUnchanged( world, () =>
			{
				Widget.HandleKeyPress( input );
			} );
		}

		public void OnMouseInput( MouseInput input )
		{
			Sync.CheckSyncUnchanged( world, () =>
			{
				Widget.HandleInput( input );
			} );
		}
	}
}
