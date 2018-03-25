Samsites = {samsite1, samsite2, samsite3}

NodBase = {NodSilo1, NodSilo2, NodSilo3, NodSilo4, NodAirfield, NodHand, NodPP1, NodPP2, NodPP3, NodPP4, NodRef, NodTurr1, NodTurr2, NodCY, NodHQ}

AttackTriggerUnits = {rifleat1, rifleat2, rifleat3, rifleat4, rifleat5, rifleat6, rifleat7, rockat1, rockat2, rockat3, rockat4, rockat5, rockat6, rockat7, rockat8, flameat1, flameat2, flameat3, flameat4, vecat1, vecat2, vecat3, vecat4}

AttackTriggerPos = {CPos.New(14,25),CPos.New(15,25),CPos.New(16,25),CPos.New(17,25),CPos.New(18,25),CPos.New(19,25),CPos.New(20,25),CPos.New(21,25),CPos.New(22,25),CPos.New(23,25),CPos.New(24,25),CPos.New(25,25),CPos.New(26,25)} 

AttackPosition = HQ.Location

Difficulty = Map.LobbyOption("difficulty")

------------------------
-- The following can/should be moved to a global campaign script at some point --
InitObjectives = function(player)
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Lose")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Win")
		end)
	end)
end

---------------


WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Camera.Position = HQ.CenterPosition

	InitObjectives(GDI)

-- Not sure what the original mission statements were. Can/Should be changes accordingly
	gdiMainObjective = GDI.AddPrimaryObjective("Destroy any Nod structures and units.")
	gdiAirSupportObjective = GDI.AddSecondaryObjective("Destroy the SAM sites to receive air support.")
	nodObjective = Nod.AddPrimaryObjective("Kill all enemies!")
	
	Trigger.OnAllKilledOrCaptured(Samsites, function()
		GDI.MarkCompletedObjective(gdiAirSupportObjective)
		Actor.Create("airstrike.proxy", true, {Owner = GDI})
	end)

	Trigger.OnEnteredFootprint(AttackTriggerPos, function(a, id)
		if a.Owner == GDI then
			Utils.Do(AttackTriggerUnits, function(unit)
				if not unit.IsDead then
					unit.AttackMove(AttackPosition)
					IdleHunt(unit)
				end
			Trigger.RemoveFootprintTrigger(id)
			end)
		end
	end)
	Trigger.AfterDelay(0, ActivateAI)
end

Tick = function()
	--HACK: Make sure the enemy has enough resources. With enough balancing/smarter AI play this should probably be removed at some point
	Nod.Cash = 4000
	
	if GDI.HasNoRequiredUnits()  then
		Nod.MarkCompletedObjective(nodObjective)
	end
	if Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(gdiMainObjective)
	end
end
