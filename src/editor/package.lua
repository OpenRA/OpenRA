-- Copyright 2013 Paul Kulchenko, ZeroBrane LLC

local ide = ide

function PackageEventHandle(event, ...)
  for file, package in pairs(ide.packages) do
    if type(package[event]) == 'function' then
      local ok, res = pcall(package[event], package, ...)
      if not ok then
        DisplayOutputLn(("%s event failed: %s"):format(event, res))
      end
    end
  end
end

function ide:GetEditor() return GetEditor() end
function ide:GetMenuBar() return self.frame.menuBar end
function ide:GetMainFrame() return self.frame end
function ide:GetDocument(ed) return self.openDocuments[ed:GetId()] end
function ide:GetInterpreter() return self.interpreter end
function ide:GetConfig() return self.config end
