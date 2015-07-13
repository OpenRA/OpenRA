WorldLoaded = function()
	red = 0.95
	green = 0.85
	blue = 1.25
	ambient = 0.5
end

Tick = function()
	if (red < 1.0) then
		red = red + 0.001
	end

	if (green < 1.0) then
		green = green + 0.001
	end

	if (blue > 1.0) then
		blue = blue - 0.001
	end

	if (ambient < 1.0) then
		ambient = ambient + 0.001
	end

	Effect.ChangeLighting(red, green, blue, ambient)
end
