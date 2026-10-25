function WorldLoaded()
	Camera.Position = Unit.CenterPosition
	Player.GetPlayer('GDI').AddPrimaryObjective('Second mission reached: progression succeeded')
	print('PROGRESSION-SECOND difficulty=' .. Map.LobbyOption('difficulty'))
	Trigger.AfterDelay(10, function() print('PROGRESSION-SECOND-TICKING') end)
end
