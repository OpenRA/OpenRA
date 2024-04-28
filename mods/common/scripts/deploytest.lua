WorldLoaded = function()
	SetDeployTrigger()
end

SetDeployTrigger = function()
	Media.DisplayMessage("Deploy globally.", "Test")
	Utils.Do(Map.ActorsInWorld , function(unit)
		if unit.HasProperty("SwitchToDeploy") then
			unit.SwitchToDeploy()
			if not barked then
				barked = true
			end
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), SetUndeployTrigger)
end

SetUndeployTrigger = function()
	Media.DisplayMessage("Undeploy globally.", "Test")
	Utils.Do(Map.ActorsInWorld , function(unit)
		if unit.HasProperty("SwitchToUndeploy") then
			unit.SwitchToUndeploy()
		end
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), SetDeployTrigger)
end
