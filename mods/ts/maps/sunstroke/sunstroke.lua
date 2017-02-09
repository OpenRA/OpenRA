Tick = function()
	if (Lighting.Red > 1.5) then
		Lighting.Red = Lighting.Red - 0.001
	end

	if (Lighting.Ambient < 0.5) then
		Lighting.Ambient = Lighting.Ambient + 0.001
	end
end
