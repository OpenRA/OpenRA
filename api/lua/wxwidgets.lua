local api = {}
for key in pairs(wx) do
	api[key] = {
		type = (type(wx[key]) == "function" and "function" or "value"),
		description = "",
		returns = "",
	}
end

return {
	wx = {
		type = "lib",
		description = "WX lib",
		childs = api
	}
}