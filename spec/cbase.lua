function CMarkSymbols(code, pos, vars)
  local idtmpl = "[A-Za-z_][A-Za-z0-9_ ]*"
  local funcdeftmpl = "("..idtmpl.."%s+%*?"..idtmpl..")%s*%([A-Za-z_][A-Za-z0-9_ %*,]*%)%s*%{"
  local isfndef = function(str, pos)
    local s,e,pref,cap
    while true do
      s,e,pref,cap = string.find(str,"([\r\n]%s*)"..funcdeftmpl, pos)
      if (not s) then
        s,e,pref,cap = string.find(str,"^(%s*)"..funcdeftmpl, pos)
      end
      if (cap and (string.find(cap,"^return") or string.find(cap,"else"))) then
        pos = s+#pref+#cap
      else
        break
      end
    end
    if s then return s+#pref,s+#pref+#cap-1,cap end
  end

  return coroutine.wrap(function()
      -- return a dummy token to produce faster result for quick typing
      coroutine.yield("String", "dummy", 1, {})
      while true do
        local fpos, lpos, name  = isfndef(code, pos)
        if not fpos then return end
        coroutine.yield("Function", name, fpos, {}, 1)
        pos = fpos + #name
      end
  end)
end

return nil -- not a real spec
