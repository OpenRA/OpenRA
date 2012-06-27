local icons = {}
local CreateBitmap = function(id, client, size)
  local width = size:GetWidth()
  local key = width .. "/" .. id
  local fileClient = "zbstudio/res/" .. key .. "-" .. client .. ".png"
  local fileKey = "zbstudio/res/" .. key .. ".png"
  local file
  if wx.wxFileName(fileClient):FileExists() then file = fileClient
  elseif wx.wxFileName(fileKey):FileExists() then file = fileKey
  else return wx.wxNullBitmap end
  local icon = icons[file] or wx.wxBitmap(file)
  icons[file] = icon
  return icon
end
local ide = ide
local app = {
  createbitmap = CreateBitmap,
  loadfilters = {
    tools = function(file) return false end,
    specs = function(file) return true end,
    interpreters = function(file) return true end,
  },

  preinit = function ()
    local artProvider = wx.wxLuaArtProvider()
    artProvider.CreateBitmap = function(self, ...) return CreateBitmap(...) end
    wx.wxArtProvider.Push(artProvider)

    ide.config.interpreter = "luadeb"
    ide.config.unhidewxwindow = true -- allow unhiding of wx windows
    ide.config.allowinteractivescript = true -- allow interaction in the output window

    -- this needs to be in pre-init to load the styles
    dofile("src/editor/markup.lua")
  end,

  postinit = function ()
    dofile("zbstudio/menu_help.lua")
    dofile("src/editor/inspect.lua")

    local bundle = wx.wxIconBundle()
    local files = FileSysGet("zbstudio/res/", wx.wxFILE)
    local icons = 0
    for i,file in ipairs(files) do
      if GetFileExt(file) == "ico" then
        icons = icons + 1
        bundle:AddIcon(file, wx.wxBITMAP_TYPE_ICO)
      end
    end
    if icons > 0 then ide.frame:SetIcons(bundle) end

    local menuBar = ide.frame.menuBar
    local menu = menuBar:GetMenu(menuBar:FindMenu("&Project"))
    local itemid = menu:FindItem("Project Directory")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end

    menu = menuBar:GetMenu(menuBar:FindMenu("&View"))
    itemid = menu:FindItem("&Load Config Style...")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end

    menuBar:Check(ID_CLEAROUTPUT, true)

    -- load welcome.lua from myprograms/ if exists
    local fn = wx.wxFileName("myprograms/welcome.lua")
    if fn:FileExists() and
      (not ide.config.path.projectdir
        or string.len(ide.config.path.projectdir) == 0) then
      fn:Normalize() -- make absolute path
      LoadFile(fn:GetFullPath(),nil,true)
      ProjectUpdateProjectDir(fn:GetPath(wx.wxPATH_GET_VOLUME))
    end
  end,
  
  stringtable = {
    editor = "ZeroBrane Studio",
    about = "About ZeroBrane Studio",
    editormessage = "ZeroBrane Studio Message",
    statuswelcome = "Welcome to ZeroBrane Studio",
    settingsapp = "ZeroBraneStudio",
    settingsvendor = "ZeroBraneLLC",
  },
  
}

return app
