using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	static class UnitOrders
	{
		public static void ProcessOrder( Order order )
		{
			switch( order.OrderString )
			{
			case "Move":
				{
					var mobile = order.Subject.traits.Get<Mobile>();
					mobile.Cancel( order.Subject );
					mobile.QueueActivity( new Mobile.MoveTo( order.TargetLocation ) );

					var attackBase = order.Subject.traits.WithInterface<AttackBase>().FirstOrDefault();
					if( attackBase != null )
						attackBase.target = null;	/* move cancels attack order */
					break;
				}
			case "Attack":
				{
					const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
					var mobile = order.Subject.traits.GetOrDefault<Mobile>();
					var weapon = order.Subject.unitInfo.Primary ?? order.Subject.unitInfo.Secondary;

					mobile.Cancel( order.Subject );
					// TODO: this block should be a separate activity; "MoveNear", maybe?
					{
						/* todo: choose the appropriate weapon, when only one works against this target */
						var range = Rules.WeaponInfo[ weapon ].Range;

						mobile.QueueActivity(
							new Mobile.MoveTo( order.TargetActor,
								Math.Max( 0, (int)range - RangeTolerance ) ) );
					}

					order.Subject.traits.Get<AttackTurreted>().target = order.TargetActor;
					break;
				}
			case "DeployMcv":
				{
					var mobile = order.Subject.traits.Get<Mobile>();
					mobile.QueueActivity( new Mobile.Turn( 96 ) );
					mobile.QueueActivity( new Traits.Activities.DeployMcv() );
					break;
				}
			case "DeliverOre":
				{
					var mobile = order.Subject.traits.Get<Mobile>();
					mobile.Cancel(order.Subject);
					mobile.QueueActivity(new Mobile.MoveTo(order.TargetActor.Location + new int2(1, 2)));
					mobile.QueueActivity(new Mobile.Turn(64));

					/* todo: actual deliver activity! [animation + add cash] */
					break;
				}
			case "PlaceBuilding":
				{
					Game.world.AddFrameEndTask( _ =>
					{
						var building = Rules.UnitInfo[ order.TargetString ];
						Log.Write( "Player \"{0}\" builds {1}", order.Player.PlayerName, building.Name );

						//Adjust placement for cursor to be in middle
						Game.world.Add( new Actor( building.Name, order.TargetLocation - GameRules.Footprint.AdjustForBuildingSize( building.Name ), order.Player ) );

						Game.controller.orderGenerator = null;

						order.Player.FinishProduction(Rules.UnitCategory[building.Name]);
					} );
					break;
				}
			case "BuildUnit":
				{
					Game.world.AddFrameEndTask(_ => Game.BuildUnit( order.Player, order.TargetString ));
					break;
				}
			default:
				throw new NotImplementedException();
			}
		}
	}
}
