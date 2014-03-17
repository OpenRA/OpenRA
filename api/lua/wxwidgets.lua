local function populateAPI(t)
  local api = {}
  for k,v in pairs(t) do
    api[k] = {
      type = (type(v) == "function" and "function" or "value"),
      description = "",
      returns = "",
    }
  end
  return api
end

return {
  wx = {
    type = "lib",
    description = "wx lib",
    childs = populateAPI(wx),
  },
  wxstc = {
    type = "lib",
    description = "wxSTC lib",
    childs = populateAPI(wxstc),
  },
}
