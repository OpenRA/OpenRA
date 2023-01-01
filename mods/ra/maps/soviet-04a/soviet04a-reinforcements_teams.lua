--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Civs = { civ1, civ2, civ3 }
Village = { civ1, civ2, civ3, village1, village2, village5 }
SovietMCV = { "mcv" }
InfantryReinfGreece = { "e1", "e1", "e1", "e1", "e1" }
Avengers = { "jeep", "1tnk", "2tnk", "2tnk", "1tnk" }
Patrol1Group = { "jeep", "jeep", "2tnk", "2tnk" }
Patrol2Group = { "jeep", "1tnk", "1tnk", "1tnk" }
AlliedInfantryTypes = { "e1", "e3" }
AlliedArmorTypes = { "jeep", "jeep", "1tnk", "1tnk", "1tnk" }
InfAttack = { }
ArmorAttack = { }

SovietStartToBasePath = { StartPoint.Location, SovietBasePoint.Location }
InfReinfPath = { SWRoadPoint.Location, InVillagePoint.Location }
ArmorReinfPath = { NRoadPoint.Location, CrossroadsNorthPoint.Location }
Patrol1Path = { NearRadarPoint.Location, ToRadarPoint.Location, InVillagePoint.Location, ToRadarPoint.Location }
Patrol2Path = { BridgeEntrancePoint.Location, NERoadTurnPoint.Location, CrossroadsEastPoint.Location, BridgeEntrancePoint.Location }

VillageCamArea = { CPos.New(68, 75),CPos.New(68, 76),CPos.New(68, 77),CPos.New(68, 78),CPos.New(68, 79), CPos.New(68, 80), CPos.New(68, 81), CPos.New(68, 82) }

if Difficulty == "easy" then
	ArmorReinfGreece = { "jeep", "1tnk", "1tnk" }
else
	ArmorReinfGreece = { "jeep", "jeep", "1tnk", "1tnk", "1tnk" }
end

AttackPaths =
{
	{ VillageEntrancePoint },
	{ BridgeEntrancePoint, NERoadTurnPoint, CrossroadsEastPoint }
}

ReinfInf = function()
	if RadarDome.IsDead or RadarDome.Owner ~= Greece then
		return
	end

	Reinforcements.Reinforce(Greece, InfantryReinfGreece, InfReinfPath, 0, function(soldier)
		soldier.Hunt()
	end)
end

ReinfArmor = function()
	if not RadarDome.IsDead and RadarDome.Owner == Greece then
		RCheck = true
		Reinforcements.Reinforce(Greece, ArmorReinfGreece, ArmorReinfPath, 0, function(soldier)
			soldier.Hunt()
		end)
	end
end

BringPatrol1 = function()
	if RadarDome.IsDead or RadarDome.Owner ~= Greece then
		return
	end

	local units = Reinforcements.Reinforce(Greece, Patrol1Group, { SWRoadPoint.Location }, 0)
	Utils.Do(units, function(patrols)
		patrols.Patrol(Patrol1Path, true, 250)
	end)

	Trigger.OnAllKilled(units, function()
		if Difficulty == "hard" then
			Trigger.AfterDelay(DateTime.Minutes(4), BringPatrol1)
		else
			Trigger.AfterDelay(DateTime.Minutes(7), BringPatrol1)
		end
	end)
end

BringPatrol2 = function()
	if RadarDome.IsDead or RadarDome.Owner ~= Greece then
		return
	end

	local units = Reinforcements.Reinforce(Greece, Patrol2Group, { NRoadPoint.Location }, 0)
	Utils.Do(units, function(patrols)
		patrols.Patrol(Patrol2Path, true, 250)
	end)

	Trigger.OnAllKilled(units, function()
		if Difficulty == "hard" then
			Trigger.AfterDelay(DateTime.Minutes(4), BringPatrol2)
		else
			Trigger.AfterDelay(DateTime.Minutes(7), BringPatrol2)
		end
	end)
end
