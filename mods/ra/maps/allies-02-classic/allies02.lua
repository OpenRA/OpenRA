JeepReinforcements = { "e1", "e1", "e1", "jeep" }
JeepReinforcementsInterval = 15
TruckNames = { "truk", "truk", "truk" }
TruckInterval = 25
TruckDelay = 75
FirstJeepReinforcementsDelay = 125
SecondJeepReinforcementsDelay = 250

SendMcvReinforcements = function()
	Media.PlaySpeechNotification("ReinforcementsArrived")
	local mcv = Actor.Create("mcv", { Owner = player, Location = ReinforcementsEntryPoint.Location })
	Actor.Move(mcv, McvDeployPoint.Location)
	Actor.DeployTransform(mcv)
end

SendJeepReinforcements = function()
	Media.PlaySpeechNotification("ReinforcementsArrived")
	Reinforcements.Reinforce(player, JeepReinforcements, ReinforcementsEntryPoint.Location, ReinforcementsRallyPoint.Location, JeepReinforcementsInterval)
end

RunInitialActivities = function()
	Actor.Harvest(Harvester)
end

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil)
	Media.PlayMovieFullscreen("montpass.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player })
	Media.PlayMovieFullscreen("frozen.vqa")
end

Tick = function()
	Mission.TickTakeOre(ussr)

	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if not trucksSent and Mission.RequiredUnitsAreDestroyed(ussr) and Mission.RequiredUnitsAreDestroyed(badGuy) then
		SendTrucks()
		trucksSent = true
	end
end

SendTrucks = function()
	Media.PlaySpeechNotification("ConvoyApproaching")
	OpenRA.RunAfterDelay(TruckDelay, function()
		local trucks = Reinforcements.Reinforce(france, TruckNames, TruckEntryPoint.Location, TruckRallyPoint.Location, TruckInterval,
			function(truck)
				Actor.Move(truck, TruckExitPoint.Location)
				Actor.RemoveSelf(truck)
			end)
		local trucksTeam = Team.New(trucks)
		Team.AddEventHandler(trucksTeam.OnAllRemovedFromWorld, MissionAccomplished)
		Team.AddEventHandler(trucksTeam.OnAnyKilled, MissionFailed)
	end)
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("Greece")
	france = OpenRA.GetPlayer("France")
	ussr = OpenRA.GetPlayer("USSR")
	badGuy = OpenRA.GetPlayer("BadGuy")
	
	RunInitialActivities()
	
	SendMcvReinforcements()
	OpenRA.RunAfterDelay(FirstJeepReinforcementsDelay, SendJeepReinforcements)
	OpenRA.RunAfterDelay(SecondJeepReinforcementsDelay, SendJeepReinforcements)
	
	OpenRA.SetViewportCenterPosition(ReinforcementsEntryPoint.CenterPosition)
	
	Media.PlayMovieFullscreen("ally2.vqa", function() Media.PlayMovieFullscreen("mcv.vqa", Media.PlayRandomMusic) end)
end