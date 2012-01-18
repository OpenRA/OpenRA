local ide = ide
local app = {

  loadfilters = {
    tools = function(file) return false end,
    specs = function(file) return true end,
    interpreters = function(file) return true end,
  },

  preinit = function ()
    local artProvider = wx.wxLuaArtProvider()
    local icons = {}
    artProvider.CreateBitmap = function(self, id, client, size)
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
    wx.wxArtProvider.Push(artProvider)

    ide.config.interpreter = "luadeb";
  end,

  postinit = function ()
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

    -- start debugger
    ide.debugger.listen()

    local menuBar = ide.frame.menuBar
    local menu = menuBar:GetMenu(menuBar:FindMenu("&Project"))
    local itemid = menu:FindItem("Lua &interpreter")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end
    itemid = menu:FindItem("Project directory")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end

    menu = menuBar:GetMenu(menuBar:FindMenu("&View"))
    itemid = menu:FindItem("&Load Config Style...")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end

    menuBar:Check(ID_CLEAROUTPUT, true)
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
