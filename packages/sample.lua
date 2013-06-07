local P = {
  name = "Sample plugin",
  description = "Sample plugin to demonstrate various event types.",
  author = "Paul Kulchenko",
  onFileLoad = function(self, editor) end,
  onRegister = function(self) end,
}

--[[ Uncomment this to see event names printed in the Output window
  for k in pairs(P) do
    if k:find("^on") then
      P[k] = k:find("^onFile")
        and function(self, ed) print(self:GetName(), k, ide:GetDocument(ed):GetFilePath()) end
        or function(self, ed) print(self:GetName(), k) end
    end
  end
--]]

return P
