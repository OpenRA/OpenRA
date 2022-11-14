--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SovietStartReinf = { "e2", "e2" }
SovietStartToBasePath = { StartPoint.Location, SovietBasePoint.Location }
SovietMCVReinf = { "mcv", "3tnk", "3tnk", "e1", "e1" }
SovExpansionPointGuard = { "2tnk", "2tnk", "e3", "e3", "e3" }

if Difficulty == "easy" then
	ArmorReinfGreece = { "jeep", "1tnk", "1tnk" }
else
	ArmorReinfGreece = { "jeep", "jeep", "1tnk", "1tnk", "1tnk" }
end
InfantryReinfGreece = { "e1", "e1", "e1", "e1", "e1" }
CrossroadsReinfPath = { ReinfRoadPoint.Location }
ArtyReinf = { "e3", "e3", "e3", "arty", "arty" }
CoastGuardReinf = { "e1", "e1", "e3", "e1", "e3" }
DDPatrol1 = { "dd", "dd", "dd" }
DDPatrol1Path = { EIslandPoint.Location, WIslandPoint.Location, DDAttackPoint.Location, SReinfPathPoint4.Location, DDAttackPoint.Location, WIslandPoint.Location, EIslandPoint.Location, NearDockPoint.Location }
DDPatrol2 = { "dd", "dd" }
DDPatrol2Path = { NReinfPathPoint1.Location, SReinfPathPoint1.Location, SReinfPathPoint2.Location, SReinfPathPoint1.Location, NReinfPathPoint1.Location, NearDockPoint.Location }
ShipArrivePath = { ReinfNorthPoint.Location, NearDockPoint.Location }

AlliedInfantryTypes = { "e1", "e3" }
AlliedTankTypes = { "jeep", "1tnk" }
AlliedAttackPath = { GreeceBaseEPoint.Location, CrossroadPoint.Location, NWOrefieldPoint.Location, AtUSSRBasePoint.Location }
AlliedCrossroadsToRadarPath = { ReinfRoadPoint.Location, CrossroadPoint.Location, GreeceBaseEPoint.Location, GreeceBasePoint.Location }
SouthReinfPath = { ReinfEastPoint.Location, SReinfPathPoint1.Location, SReinfPathPoint2.Location, SReinfPathPoint3.Location, SReinfPathPoint4.Location, USSRlstPoint.Location }
NorthReinfPath = { ReinfEastPoint.Location, NReinfPathPoint1.Location, GGUnloadPoint.Location }
GoodGuyOrefieldPatrolPath = { PatrolPoint1.Location, PatrolPoint2.Location, PatrolPoint3.Location, PatrolPoint4.Location }

Ships = { }
GreeceInfAttack = { }
GGInfAttack = { }
TankAttackGG = { }

ShipWaypoints = { EIslandPoint, WIslandPoint, DDAttackPoint }
InfantryWaypoints = { CrossroadPoint, NWOrefieldPoint, AtUSSRBasePoint, SovietBasePoint }
InfantryGGWaypoints = { PatrolPoint2, BetweenBasesPoint, PrepGGArmyPoint }
TanksGGWaypoints = { PatrolPoint2, BetweenBasesPoint, PrepGGArmyPoint }

Para = function()
	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = player })
	powerproxy.TargetParatroopers(ParaPoint.CenterPosition, Angle.New(112))
	powerproxy.Destroy()
end

Para2 = function()
	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = player })
	powerproxy.TargetParatroopers(USSRExpansionPoint.CenterPosition, Angle.New(112))
	powerproxy.Destroy()
end

ReinfInf = function()
	Reinforcements.Reinforce(Greece, InfantryReinfGreece, CrossroadsReinfPath, 0, function(soldier)
		soldier.Hunt()
	end)
end

ReinfArmor = function()
	RCheck = false
	Reinforcements.Reinforce(Greece, ArmorReinfGreece, CrossroadsReinfPath, 0, function(soldier)
		soldier.Hunt()
	end)
end

IslandTroops1 = function()
	local units = Reinforcements.ReinforceWithTransport(GoodGuy, "lst", CoastGuardReinf, { ReinfEastPoint.Location, NReinfPathPoint1.Location, GGUnloadPoint.Location }, { ReinfEastPoint.Location })[2]
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(coastguard)
			coastguard.AttackMove(CoastGuardPoint.Location)
		end)
	end)
	if not CheckForCYard() then
		return
	elseif Difficulty == "easy" then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Difficulty == "hard" then
				Trigger.AfterDelay(DateTime.Minutes(3), IslandTroops1)
			else
				Trigger.AfterDelay(DateTime.Minutes(5), IslandTroops1)
			end
		end)
	end
end

IslandTroops2 = function()
	local units = Reinforcements.ReinforceWithTransport(Greece, "lst", ArmorReinfGreece, NorthReinfPath, { ReinfEastPoint.Location })[2]
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(patrols)
			patrols.Patrol(GoodGuyOrefieldPatrolPath, true, 150)
		end)
	end)
	if not CheckForCYard() then
		return
	elseif Difficulty == "easy" then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Difficulty == "hard" then
				Trigger.AfterDelay(DateTime.Minutes(3), IslandTroops2)
			else
				Trigger.AfterDelay(DateTime.Minutes(5), IslandTroops2)
			end
		end)
	end
end

IslandTroops3 = function()
	local units = Reinforcements.ReinforceWithTransport(Greece, "lst", SovExpansionPointGuard, SouthReinfPath, { ReinfEastPoint.Location })[2]
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(guards)
			guards.AttackMove(USSRExpansionPoint.Location)
		end)
	end)
	if not CheckForCYard() then
		return
	elseif Difficulty == "easy" then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Difficulty == "hard" then
				Trigger.AfterDelay(DateTime.Minutes(3), IslandTroops3)
			else
				Trigger.AfterDelay(DateTime.Minutes(5), IslandTroops3)
			end
		end)
	end
end

BringDDPatrol1 = function()
	local units = Reinforcements.Reinforce(Greece, DDPatrol1, ShipArrivePath, 0)
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(patrols)
			patrols.Patrol(DDPatrol1Path, true, 250)
		end)
	end)
	if not CheckForCYard() then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Difficulty == "hard" then
				Trigger.AfterDelay(DateTime.Minutes(4), BringDDPatrol1)
			else
				Trigger.AfterDelay(DateTime.Minutes(7), BringDDPatrol1)
			end
		end)
	end
end

BringDDPatrol2 = function()
	local units = Reinforcements.Reinforce(Greece, DDPatrol2, ShipArrivePath, 0)
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function(patrols)
			patrols.Patrol(DDPatrol2Path, true, 250)
		end)
	end)
	if not CheckForCYard() then
		return
	else
		Trigger.OnAllKilled(units, function()
			if Difficulty == "hard" then
				Trigger.AfterDelay(DateTime.Minutes(4), BringDDPatrol2)
			else
				Trigger.AfterDelay(DateTime.Minutes(7), BringDDPatrol2)
			end
		end)
	end
end
