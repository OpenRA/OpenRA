--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
StartUnits = { "jeep", "jeep", "1tnk", "1tnk", "1tnk", "1tnk", "2tnk", "2tnk" }
StartingPlanes = { Yak1, Yak2 }
ReinforcementFootprint1 = { CPos.New(93,78), CPos.New(94,78), CPos.New(95,78) }
InfantrySquad = { "e1", "e1", "e1", "e3", "e3" }
ReinforcementFootprint2 = { CPos.New(63,84), CPos.New(63,85), CPos.New(63,86), CPos.New(63,87) }
SouthWaterPath1 = { SouthWaterEntry.Location, SouthWaterLanding1.Location }
SouthWaterPath2 = { SouthWaterEntry.Location, SouthWaterLanding2.Location }
SouthWaterTeam = { "dtrk", "1tnk", "1tnk", "2tnk", "2tnk" }
SouthChinookPath = { ChinookEntry1.Location, ChinookLZ1.Location }
SouthChinookChalk = { "e3", "e3", "e3", "e1", "e1" }
MammothAttackFootprint = { CPos.New(49,81), CPos.New(50,81), CPos.New(51,81), CPos.New(52,81), CPos.New(53,81) }
BridgeMammoths = { Mammoth2, Mammoth3 }
ReinforcementFootprint3 = { CPos.New(60,74), CPos.New(61,74) }
MoneyTrucks = { "truk", "truk" }
SovietHouseSquad = { "e2", "e2", "e2", "e3", "e3" }
AlliedHouseSquad = { "e6", "e6", "medi", "spy", "mech" }
MediumTanks = { Medium1, Medium2, Medium3, Medium4 }
ReinforcementFootprint4 = { CPos.New(77,64), CPos.New(78,64), CPos.New(79,64), CPos.New(85,67), CPos.New(85,68), CPos.New(85,69) }
LightTanks = { LightTank1, LightTank2 }
ReinforcementFootprint5 = { CPos.New(92,54), CPos.New(92,55), CPos.New(92,56), CPos.New(92,57), CPos.New(96,62), CPos.New(97,62), CPos.New(98,62), CPos.New(99,62) }
ReinforcementFootprint6 = { CPos.New(97,51), CPos.New(98,51) }
NorthChinookPath = { ChinookEntry2.Location, ChinookLZ2.Location }
NorthChinookChalk = { "e3", "e3", "e3", "e3", "e3" }
Scientists = { "einstein", "chan", "chan", "chan", "chan" }
NorthWaterPath1 = { NorthWaterEntry.Location, NorthWaterLanding1.Location }
NorthWaterPath2 = { NorthWaterEntry.Location, NorthWaterLanding2.Location }
NorthWaterPath3 = { NorthWaterEntry.Location, NorthWaterLanding3.Location }
NorthWaterTeam = { "1tnk", "1tnk", "1tnk", "2tnk", "2tnk" }
BoatofMammoths = { "4tnk", "4tnk", "4tnk", "4tnk", "4tnk" }
EvacuateFootprint = { CPos.New(93,92), CPos.New(94,92), CPos.New(95,92), CPos.New(96,92), CPos.New(97,92), CPos.New(98,92), CPos.New(99,92) }
TimerTicks = DateTime.Minutes(54)

AlliedReinforcements = function()
	Reinforcements.Reinforce(Greece, StartUnits, { AlliesEntry.Location, AlliesRally.Location }, 6)
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		UnitsArrived = true
	end)

	local foot1Triggered
	Trigger.OnEnteredFootprint(ReinforcementFootprint1, function(actor, id)
		if actor.Owner == Greece and not foot1Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot1Triggered = true

			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
			Reinforcements.Reinforce(Greece, InfantrySquad, { InfantrySquadEntry.Location, InfantrySquadStop.Location }, 2)
		end
	end)

	local foot2Triggered
	Trigger.OnEnteredFootprint(ReinforcementFootprint2, function(actor, id)
		if actor.Owner == Greece and not foot2Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot2Triggered = true

			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
			local demoTeam = Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", SouthWaterTeam, SouthWaterPath1, { SouthWaterPath1[1] })
			local chinookChalk1 = Reinforcements.ReinforceWithTransport(Greece, "tran.reinforcement", SouthChinookChalk, SouthChinookPath, { SouthChinookPath[1] })
		end
	end)

	local foot3Triggered
	Trigger.OnEnteredFootprint(ReinforcementFootprint3, function(actor, id)
		if actor.Owner == Greece and not foot3Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot3Triggered = true

			Media.PlaySound("chrono2.aud")
			Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoBeam1.Location })
			Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoBeam1.Location + CVec.New(1,1) })

			Reinforcements.Reinforce(USSR, MoneyTrucks, { AlliesEntry.Location, TruckStop1.Location }, 0)
			Utils.Do(StartingPlanes, function(yaks)
				InitializeAttackAircraft(yaks, Greece)
			end)

			if not USSRHouse.IsDead then
				local houseSquad = Reinforcements.Reinforce(USSR, SovietHouseSquad, { VillageSpawnUSSR.Location }, 0)
				Utils.Do(houseSquad, IdleHunt)
				Trigger.OnAllKilled(houseSquad, function()
					if not AlliesHouse.IsDead then
						Media.PlaySoundNotification(Greece, "AlertBleep")
						Media.DisplayMessage("Friendlies coming out!", "Medic")
						Reinforcements.Reinforce(Greece, AlliedHouseSquad, { VillageSpawnAllies.Location, VillageRally.Location }, 0)
					end
				end)
			end
		end
	end)

	local foot4Triggered
	Trigger.OnEnteredFootprint(ReinforcementFootprint4, function(actor, id)
		if actor.Owner == Greece and not foot4Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot4Triggered = true

			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
			Paradrop3.TargetParatroopers(ParaLZ3.CenterPosition, Angle.New(935))
		end
	end)

	local foot5Triggered
	Trigger.OnEnteredFootprint(ReinforcementFootprint5, function(actor, id)
		if actor.Owner == Greece and not foot5Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot5Triggered = true

			Media.PlaySound("chrono2.aud")
			Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoBeam2.Location })
			Actor.Create("ctnk", true, { Owner = Greece, Location = ChronoBeam2.Location + CVec.New(1,1) })

			Utils.Do(LightTanks, IdleHunt)
		end
	end)

	local foot6Triggered
	Trigger.OnEnteredFootprint(ReinforcementFootprint6, function(actor, id)
		if actor.Owner == Greece and not foot6Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot6Triggered = true

			Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
			local chinookChalk2 = Reinforcements.ReinforceWithTransport(Greece, "tran.reinforcement", NorthChinookChalk, NorthChinookPath, { NorthChinookPath[1] })
			InfantryProduction()
			TankProduction()
		end
	end)

	Trigger.OnEnteredProximityTrigger(ScientistEscape.CenterPosition, WDist.FromCells(3), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			ScientistsFreed = true

			Media.PlaySpeechNotification(Greece, "TargetFreed")
			VIPs = Reinforcements.Reinforce(Greece, Scientists, { ScientistEscape.Location, ScientistRally.Location }, 2)
			Actor.Create("flare", true, { Owner = Greece, Location = DefaultCameraPosition.Location })

			Trigger.OnAnyKilled(VIPs, function()
				Greece.MarkFailedObjective(RescueScientists)
			end)

			-- Add the footprint trigger in a frame end task (delay 0) to avoid crashes
			local left = #VIPs
			Trigger.AfterDelay(0, function()
				Trigger.OnEnteredFootprint(EvacuateFootprint, function(a, id)
					if a.Type == "chan" or a.Type == "einstein" then
						a.Owner = GoodGuy
						a.Stop()
						a.Move(AlliesEntry.Location)

						-- in case units get stuck
						Trigger.OnIdle(a, function()
							a.Move(AlliesEntry.Location)
						end)
					end
				end)

				Trigger.OnEnteredFootprint({ AlliesEntry.Location }, function(a, id)
					if a.Type == "chan" or a.Type == "einstein" then
						a.Stop()
						a.Destroy()

						left = left - 1
						if left == 0 then
							if not Greece.IsObjectiveCompleted(RescueScientists) and not Greece.IsObjectiveFailed(RescueScientists) then
								Greece.MarkCompletedObjective(RescueScientists)
							end
						end
					else
						a.Stop()
						a.Destroy()
					end
				end)
			end)

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
				Media.DisplayMessage("Commander, we're detecting Soviet transports headed your way. Get those scientists back to the extraction point in the southeast!", "LANDCOM 16")
				local northTeam = Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", NorthWaterTeam, NorthWaterPath1, { NorthWaterPath1[1] })
			end)

			Trigger.AfterDelay(DateTime.Seconds(10), function()
				Media.PlaySpeechNotification(Greece, "SovietForcesApproaching")
				local mammoths1 = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", BoatofMammoths, NorthWaterPath2, { NorthWaterPath2[1] })[2]
				Utils.Do(mammoths1, function(unit)
					Trigger.OnAddedToWorld(unit, IdleHunt)
				end)
				local mammoths2 = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", BoatofMammoths, NorthWaterPath3, { NorthWaterPath3[1] })[2]
				Utils.Do(mammoths2, function(unit)
					Trigger.OnAddedToWorld(unit, IdleHunt)
				end)
			end)

			local footMammoth2Triggered
			Trigger.OnEnteredFootprint(MammothAttackFootprint, function(actor, id)
				if actor.Type == "chan" and not footMammoth2Triggered then
					Trigger.RemoveFootprintTrigger(id)
					footMammoth2Triggered = true

					Trigger.AfterDelay(DateTime.Seconds(10), function()
						Media.PlaySpeechNotification(Greece, "SovietForcesApproaching")
						local mammoths3 = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", BoatofMammoths, SouthWaterPath1, { SouthWaterPath1[1] })[2]
						Utils.Do(mammoths3, function(unit)
							Trigger.OnAddedToWorld(unit, IdleHunt)
						end)
						local mammoths4 = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", BoatofMammoths, SouthWaterPath2, { SouthWaterPath2[1] })[2]
						Utils.Do(mammoths4, function(unit)
							Trigger.OnAddedToWorld(unit, IdleHunt)
						end)
					end)
				end
			end)
		end
	end)
end

EnemyActions = function()
	Utils.Do(StartingPlanes, function(a)
		a.ReturnToBase()
	end)

	local firstDrop = Paradrop1.TargetParatroopers(ParaLZ1.CenterPosition, Angle.New(214))
	Utils.Do(firstDrop, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)

	local footMammothTriggered
	Trigger.OnEnteredFootprint(MammothAttackFootprint, function(actor, id)
		if actor.Owner == Greece and not footMammothTriggered then
			Trigger.RemoveFootprintTrigger(id)
			footMammothTriggered = true

			Utils.Do(BridgeMammoths, IdleHunt)

			local secondDrop = Paradrop2.TargetParatroopers(ParaLZ2.CenterPosition, Angle.New(808))
			Utils.Do(secondDrop, function(a)
				Trigger.OnPassengerExited(a, function(t, p)
					IdleHunt(p)
				end)
			end)
		end
	end)

	Trigger.OnKilled(Tesla2, function()
		Utils.Do(MediumTanks, function(tank)
			if not tank.IsDead then
				IdleHunt(tank)
			end
		end)
	end)
end

EnemyInfantry = { "e1", "e1", "e3" }
Tanks = { "1tnk", "3tnk" }
AttackGroupSize = 4
ProductionDelay = DateTime.Seconds(10)
IdlingUnits = { }

InfantryProduction = function()
	if (Tent1.IsDead or Tent1.Owner ~= USSR) and (Tent2.IsDead or Tent2.Owner ~= USSR) then
		return
	end

	local toBuild = { Utils.Random(EnemyInfantry) }

	USSR.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(ProductionDelay, InfantryProduction)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendAttack()
		end
	end)
end

TankProduction = function()
	if WarFactory.IsDead or WarFactory.Owner ~= USSR then
		return
	end

	local toBuild = { Utils.Random(Tanks) }

	USSR.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(ProductionDelay, TankProduction)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendAttack()
		end
	end)
end

SendAttack = function()
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		local number = Utils.RandomInteger(1, #IdlingUnits)

		if IdlingUnits[number] and not IdlingUnits[number].IsDead then
			units[i] = IdlingUnits[number]
			table.remove(IdlingUnits, number)
		end
	end

	Utils.Do(units, function(unit)
		if not unit.IsDead then
			IdleHunt(unit)
		end
	end)
end

LoseTriggers = function()
	Trigger.OnKilled(ForwardCommand, function()
		if not ScientistsFreed then
			Greece.MarkFailedObjective(RescueScientists)
			Media.DisplayMessage("The scientists were in the Command Center!", "LANDCOM 16")
		end
	end)

	Trigger.OnKilled(Chronosphere, function()
		Greece.MarkFailedObjective(RescueScientists)
	end)

	Trigger.OnCapture(Chronosphere, function()
		Chronosphere.Kill()
	end)
end

ticked = TimerTicks
Tick = function()
	if Greece.HasNoRequiredUnits() and UnitsArrived then
		USSR.MarkCompletedObjective(SovietObj)
	end

	if ticked > 0 then
		UserInterface.SetMissionText("Chronosphere explodes in " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked == 0 then
		UserInterface.SetMissionText("We're too late!", USSR.Color)
		USSR.MarkCompletedObjective(SovietObj)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	GoodGuy = Player.GetPlayer("GoodGuy")

	InitObjectives(Greece)

	SovietObj = USSR.AddObjective("Defeat Allies.")
	RescueScientists = Greece.AddObjective("Rescue the scientists and escort them back to the\nextraction point.")

	Camera.Position = DefaultCameraPosition.CenterPosition
	Paradrop1 = Actor.Create("paradrop1", false, { Owner = USSR })
	Paradrop2 = Actor.Create("paradrop2", false, { Owner = USSR })
	Paradrop3 = Actor.Create("paradrop3", false, { Owner = Greece })
	TimerColor = Greece.Color
	Tent1.IsPrimaryBuilding = true
	AlliedReinforcements()
	EnemyActions()
	LoseTriggers()

	Trigger.AfterDelay(DateTime.Minutes(24), function()
		Media.PlaySpeechNotification(Greece, "ThirtyMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(34), function()
		Media.PlaySpeechNotification(Greece, "TwentyMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(44), function()
		Media.PlaySpeechNotification(Greece, "TenMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(49), function()
		Media.PlaySpeechNotification(Greece, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(50), function()
		Media.PlaySpeechNotification(Greece, "WarningFourMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(51), function()
		Media.PlaySpeechNotification(Greece, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(52), function()
		Media.PlaySpeechNotification(Greece, "WarningTwoMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(53), function()
		Media.PlaySpeechNotification(Greece, "WarningOneMinuteRemaining")
	end)
end
