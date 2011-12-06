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
      local file = "zbstudio/res/" .. key .. ".png"
      if not wx.wxFileName(file):FileExists() then return wx.wxNullBitmap end
      local icon = icons[key] or wx.wxBitmap(file)
      icons[key] = icon
      return icon
    end
    wx.wxArtProvider.Push(artProvider)

    ide.config.interpreter = "luadeb";
  end,

  postinit = function ()
    local icon = wx.wxIcon()
    icon:LoadFile("zbstudio/res/zbstudio.ico",wx.wxBITMAP_TYPE_ICO)
    ide.frame:SetIcon(icon)

    local pos = ide.frame.menuBar:FindMenu("&Project")
    local menu = ide.frame.menuBar:GetMenu(pos)
    local itemid = menu:FindItem("Lua &interpreter")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end
    itemid = menu:FindItem("Project directory")
    if itemid ~= wx.wxNOT_FOUND then menu:Destroy(itemid) end

    pos = ide.frame.menuBar:FindMenu("&View")
    menu = ide.frame.menuBar:GetMenu(pos)
    local items = {5, 4, 1, 0}
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
