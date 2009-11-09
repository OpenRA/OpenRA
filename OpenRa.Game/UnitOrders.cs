using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
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
					mobile.QueueActivity( new Traits.Activities.Move( order.TargetLocation, 8 ) );

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

					mobile.Cancel(order.Subject);
					// TODO: this block should be a separate activity; "MoveNear", maybe?
					{
						/* todo: choose the appropriate weapon, when only one works against this target */
						var range = Rules.WeaponInfo[weapon].Range;

						mobile.QueueActivity(
							new Traits.Activities.Follow(order.TargetActor, Math.Max(0, (int)range - RangeTolerance)));
					}

					order.Subject.traits.Get<AttackTurreted>().target = order.TargetActor;
					break;
				}
			case "DeployMcv":
				{
					if (!Game.CanPlaceBuilding("fact", order.Subject.Location - new int2(1,1), order.Subject, false))
						break;	/* throw the order on the floor */

					var mobile = order.Subject.traits.Get<Mobile>();
					mobile.Cancel(order.Subject);
					mobile.QueueActivity( new Traits.Activities.Turn( 96 ) );
					mobile.QueueActivity( new Traits.Activities.DeployMcv() );
					break;
				}
			case "DeliverOre":
				{
					var mobile = order.Subject.traits.Get<Mobile>();
					mobile.Cancel(order.Subject);
					mobile.QueueActivity(new Traits.Activities.DeliverOre(order.TargetActor));
					break;
				}
			case "Harvest":
				{
					var mobile = order.Subject.traits.Get<Mobile>();
					mobile.Cancel(order.Subject);
					mobile.QueueActivity(new Traits.Activities.Move(order.TargetLocation, 0));
					mobile.QueueActivity(new Traits.Activities.Harvest() );
					break;
				}
			case "PlaceBuilding":
				{
					Game.world.AddFrameEndTask( _ =>
					{
						var building = Rules.UnitInfo[ order.TargetString ];
						var producing = order.Player.Producing( "Building" );
						if( producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0 )
							return;

						Log.Write( "Player \"{0}\" builds {1}", order.Player.PlayerName, building.Name );

						Game.world.Add( new Actor( building.Name, order.TargetLocation - GameRules.Footprint.AdjustForBuildingSize( building.Name ), order.Player ) );

						order.Player.FinishProduction(Rules.UnitCategory[building.Name]);
					} );
					break;
				}
			case "StartProduction":
				{
					string group = Rules.UnitCategory[ order.TargetString ];
					var ui = Rules.UnitInfo[ order.TargetString ];
					var time = ui.Cost
						* .8f /* Game.BuildSpeed */						/* todo: country-specific build speed bonus */
						* ( 25 * 60 ) /* frames per min */				/* todo: build acceleration, if we do that */
						/ 1000;

					time = .05f * time;						/* temporary hax so we can build stuff fast for test */

					order.Player.BeginProduction(group,
						new ProductionItem(order.TargetString, (int)time, ui.Cost,
							() => Game.world.AddFrameEndTask(
								_ =>
								{
									if (order.Player == Game.LocalPlayer)
										Game.PlaySound(group == "Building" 
											? "conscmp1.aud" : "unitrdy1.aud", false);
									if (group != "Building")
										Game.BuildUnit(order.Player, order.TargetString);
								})));
					break;
				}
			case "PauseProduction":
				{
					var producing = order.Player.Producing( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						producing.Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					var producing = order.Player.Producing( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						order.Player.CancelProduction( Rules.UnitCategory[ order.TargetString ] );
					break;
				}
			case "SetRallyPoint":
				{
					var pt = order.Subject.traits.Get<RallyPoint>();
					pt.rallyPoint = order.TargetLocation;
					break;
				}
			default:
				throw new NotImplementedException();
			}
		}
	}
}
