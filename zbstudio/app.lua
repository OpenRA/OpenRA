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
    local icon = wx.wxIcon()
    icon:LoadFile("zbstudio/res/zbstudio.ico",wx.wxBITMAP_TYPE_ICO)
    ide.frame:SetIcon(icon)

    -- start debugger
    ide.debugger.listen()

    local pos = ide.frame.menuBar:FindMenu("&Project")
    local menu = ide.frame.menuBar:GetMenu(pos)
    local itemid = menu:FindItem("Lua &interpreter")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end
    itemid = menu:FindItem("Project directory")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end

    pos = ide.frame.menuBar:FindMenu("&View")
    menu = ide.frame.menuBar:GetMenu(pos)
    local items = {3, 2}
    while #items > 0 do
      local itempos = table.remove(items, 1)
      menu:Destroy(menu:FindItemByPosition(itempos))
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
