using System;
using System.Collections.Generic;

namespace OpenRa.Game
{
	//Unit Missions:
	//{
	//in rules.ini:
	//	Sleep - no-op
	//	Harmless - no-op, and also not considered a threat
	//	Sticky
	//	Attack
	//	Move
	//	QMove
	//	Retreat
	//	Guard
	//	Enter
	//	Capture
	//	Harvest
	//	Area Guard
	//	[Return] - unused
	//	Stop
	//	[Ambush] - unused
	//	Hunt
	//	Unload
	//	Sabotage
	//	Construction
	//	Selling
	//	Repair
	//	Rescue
	//	Missile
	//
	//not in original RA:
	//	Deploy (Mcv -> Fact) [should this be construction/unload?]
	//}

	[Flags]
	enum SupportedMissions
	{
		Stop = 0,
		Harvest = 1,
		Deploy = 2,
	}

	//delegate void UnitMission( int t );
	//static class UnitMissions
	//{
	//    public static UnitMission Sleep()
	//    {
	//        return t => { };
	//    }

	//    public static UnitMission Move( Unit unit, int2 destination )
	//    {
	//        return t =>
	//        {
	//            Game game = unit.game;

	//            if( unit.nextOrder != null )
	//                destination = unit.toCell;

	//            if( Turn( unit, unit.GetFacing( unit.toCell - unit.fromCell ) ) )
	//                return;

	//            unit.moveFraction += t * unit.unitInfo.Speed;
	//            if( unit.moveFraction < unit.moveFractionTotal )
	//                return;

	//            unit.moveFraction = 0;
	//            unit.moveFractionTotal = 0;
	//            unit.fromCell = unit.toCell;

	//            if( unit.toCell == destination )
	//            {
	//                unit.currentOrder = null;
	//                return;
	//            }

	//            List<int2> res = game.pathFinder.FindUnitPath( unit.toCell, PathFinder.DefaultEstimator( destination ) );
	//            if( res.Count != 0 )
	//            {
	//                unit.toCell = res[ res.Count - 1 ];

	//                int2 dir = unit.toCell - unit.fromCell;
	//                unit.moveFractionTotal = ( dir.X != 0 && dir.Y != 0 ) ? 2500 : 2000;
	//            }
	//            else
	//                destination = unit.toCell;
	//        };
	//    }

	//    public static UnitMission Deploy( Unit unit )
	//    {
	//        return t =>
	//        {
	//            Game game = unit.game;

	//            if( Turn( unit, 12 ) )
	//                return;

	//            game.world.AddFrameEndTask( _ =>
	//            {
	//                game.world.Remove( unit );
	//                game.world.Add( new Building( "fact", unit.fromCell - new int2( 1, 1 ), unit.Owner, game ) );
	//            } );
	//            unit.currentOrder = null;
	//        };
	//    }


	//    public static UnitMission Harvest( Unit unit )
	//    {
	//        UnitMission order = null;
	//        order = t =>
	//        {
	//            // TODO: check that there's actually some ore in this cell :)

	//            // face in one of the 8 directions
	//            if( Turn( unit, ( unit.facing + 1 ) & ~3 ) )
	//                return;

	//            unit.currentOrder = _ => { };
	//            if( unit.nextOrder == null )
	//                unit.nextOrder = order;

	//            string sequenceName = string.Format( "harvest{0}", unit.facing / 4 );
	//            unit.animation.PlayThen( sequenceName, () =>
	//            {
	//                unit.currentOrder = null;
	//                unit.animation.PlayFetchIndex( "idle", () => unit.facing );
	//            } );
	//        };
	//        return order;
	//    }

	//    static bool Turn( Unit unit, int desiredFacing )
	//    {
	//        if( unit.facing == desiredFacing )
	//            return false;

	//        int df = ( desiredFacing - unit.facing + 32 ) % 32;
	//        unit.facing = ( unit.facing + ( df > 16 ? 31 : 1 ) ) % 32;
	//        return true;
	//    }
	//}
}
