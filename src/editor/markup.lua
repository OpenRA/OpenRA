-- Copyright (C) Paul Kulchenko 2011-2012
-- update styles for comment markup
local styles = ide.config.styles
local comment = styles.comment
local MD_MARK_ITAL = '_' -- italic
local MD_MARK_BOLD = '*' -- bold
local MD_MARK_LINK = '~' -- link
local MD_MARK_HEAD = '!' -- header
local MD_MARK_CODE = '@' -- code
local MD_MARK_BOXD = '|' -- highlight
local MD_MARK_LSEP = ';' -- link separator (between text and link)
local MD_MARK_MARK = ' ' -- separator
local MD_LINK_NEWWINDOW = '+' -- indicator to open a new window for links
local markup = {
  [MD_MARK_BOXD] = {st=25, fg={127,0,127}, bg=comment.bg, b=true},
  [MD_MARK_CODE] = {st=26, fg={127,127,127}, bg=comment.bg, fs=9},
  [MD_MARK_HEAD] = {st=27, fg=comment.fg, bg=comment.bg, fn="Lucida Console", b=true},
  [MD_MARK_LINK] = {st=28, fg=comment.fg, bg=comment.bg, u=true, hs={0, 0, 127}},
  [MD_MARK_BOLD] = {st=29, fg=comment.fg, bg=comment.bg, b=true, fs=11},
  [MD_MARK_ITAL] = {st=30, fg=comment.fg, bg=comment.bg, i=true},
  [MD_MARK_MARK] = {st=31, fg=comment.fg, bg=comment.bg, v=false},
}

local MD_MARK_PTRN = ''  -- combination of all markup marks that can start styling
for key,value in pairs(markup) do
  styles[key] = value
  if key ~= MD_MARK_MARK then MD_MARK_PTRN = MD_MARK_PTRN .. "%" .. key end
end

function MarkupHotspotClick(pos, editor)
  -- check if this is "our" hotspot event
  if bit.band(editor:GetStyleAt(pos),31) ~= markup[MD_MARK_LINK].st then
    -- not "our" style, so nothing to do for us here
    return
  end
  local line = editor:LineFromPosition(pos)
  local tx = editor:GetLine(line)
  pos = pos + 1 - editor:PositionFromLine(line) -- turn into relative position

  -- find the separator on the right side of the position
  local poss = string.find(tx, MD_MARK_LSEP, pos, true)
  local pose = string.find(tx, MD_MARK_LINK, pos, true)

  if (poss and pose) then 
    local text = string.sub(tx, poss+1, pose-1)
    local filepath = ide.openDocuments[editor:GetId()].filePath
    local _,_,shell = string.find(text, [[^macro:shell%((.*%S)%)$]])
    local _,_,http = string.find(text, [[^(http:%S+)$]])
    local _,_,command = string.find(text, [[^macro:(%w+)$]])
    local bottomnotebook = ide.frame.bottomnotebook
    if shell then
      ShellExecuteCode(shell)
    elseif command == 'run' then -- run the current file
      ProjectRun()
    elseif command == 'debug' then -- debug the current file
      ProjectDebug()
    elseif http then -- open the URL in a new browser window
      wx.wxLaunchDefaultBrowser(http, 0)
    else
      -- check if requested to open in a new window
      local newwindow = string.find(text, MD_LINK_NEWWINDOW, 1, true) -- plain search
      if newwindow then text = string.gsub(text, "^%" .. MD_LINK_NEWWINDOW, "") end
      local name = wx.wxFileName(filepath):GetPath(wx.wxPATH_GET_VOLUME
        + wx.wxPATH_GET_SEPARATOR) .. text
      -- load/activate file
      local filename = wx.wxFileName(name)
      filename:Normalize() -- remove .., ., and other similar elements
      if filename:FileExists() and
        (newindow or SaveModifiedDialog(editor, true) ~= wx.wxID_CANCEL) then
        LoadFile(filename,not newwindow and editor or nil,true)
      end
    end
  end
  return true
end

local function ismarkup (tx)
  local start = 1
  while true do
    -- find a separator first
    local st,_,sep = string.find(tx, "(["..MD_MARK_PTRN.."])", start)
    if not st then return end

    local s,e,cap
    if sep == MD_MARK_HEAD then
      s,e,cap = string.find(tx,"^(%"..MD_MARK_HEAD..".+[%w%p])", start)
    elseif sep == MD_MARK_LINK then
      local any = "[^%"..MD_MARK_LINK.."%"..MD_MARK_LSEP.."]-"
      s,e,cap = string.find(tx,"^(%"..MD_MARK_LINK.."[%w%p]"..any.."[%w%p]"..
                                 "%"..MD_MARK_LSEP.."[%w%p]"..any.."[%w%p]"..
                                 "%"..MD_MARK_LINK..")", st)
    else
      s,e,cap = string.find(tx,"^(%"..sep.."[%w%p][^%"..sep.."]-[%w%p]%"..sep..")", st)
      if not s then s,e,cap = string.find(tx,"^(%"..sep.."[^%s%"..sep.."]%"..sep..")", st) end
    end
    if s then return s,e,cap,sep end
    start = st+1
  end
end

function MarkupStyle(editor, lines, linee)
  local lines = lines or 0
  if (lines < 0) then return end

  -- always style to the end as there may be comments that need re-styling
  -- technically, this should be GetLineCount()-1, but we want to style
  -- beyond the last line to make sure it is styled correctly
  local linee = linee or editor:GetLineCount()

  local iscomment = {}
  for i,v in pairs(editor.spec.iscomment) do
    iscomment[i] = v
  end

  for line=lines,linee do
    local tx = editor:GetLine(line)
    local ls = editor:PositionFromLine(line)

    editor:StartStyling(ls, 0)

    local from = 1
    local off = -1

    while from do
      tx = string.sub(tx,from)
      local f,t,w,mark = ismarkup(tx)

      if (f) then
        local p = ls+f+off
        local s = bit.band(editor:GetStyleAt(p), 31)
        if iscomment[s] then
          editor:StartStyling(p, 31)
          editor:SetStyling(1, markup[MD_MARK_MARK].st)
          local endmark = 1
          if mark == MD_MARK_HEAD then
            endmark = 0
          elseif mark == MD_MARK_LINK then
            local pipe = w:find(MD_MARK_LSEP)
            if pipe then
              endmark = #w-pipe+1
            end
          end
          editor:SetStyling(t-f-endmark, markup[mark].st or markup[MD_MARK_MARK].st)
          editor:SetStyling(endmark, markup[MD_MARK_MARK].st)
        end

        off = off + t
      end
      from = t and (t+1)
    end
  end
end

-- it could work by calling MarkupStyle directly from EVT_UPDATEUI,
-- but the styling didn't work correctly as the style on block comments
-- (which is used to identify where the markup should be applied)
-- was not always correct during UPDATEUI event.
-- to rectify this, we style immediately (by calling MarkupStyle
-- from UPDATEUI), but also store the starting point and re-style
-- during the next UPDATEUI event when the block comment style is correct.
local needStyle = {}
local frame
function MarkupStyleRefresh(editor, ev)
  if not frame then
    frame = ide.frame
    frame:Connect(wx.wxEVT_IDLE,
      function(event) MarkupStyleRefresh(); event:Skip() end)
  end

  if not ev or #ev == 0 then -- no new records, refresh deferred ones
    for ed,line in pairs(needStyle) do
      MarkupStyle(ed, line)
      needStyle[ed] = nil
    end
  else -- store records from the event table to defer refresh
    for _,pos in ipairs(ev) do
      needStyle[editor] = editor:LineFromPosition(pos[1])
    end
  end
end
