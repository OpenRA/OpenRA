-- Copyright (C) Paul Kulchenko 2011-2012
-- styles for comment markup

local styles = ide.config.styles
local comment = styles.comment
local MD_MARK_ITAL = '_' -- italic
local MD_MARK_BOLD = '**' -- bold
local MD_MARK_LINK = '[' -- link
local MD_MARK_LINT = ')' -- link terminator
local MD_MARK_HEAD = '#' -- header
local MD_MARK_CODE = '`' -- code
local MD_MARK_BOXD = '|' -- highlight
local MD_MARK_LSEP = '](' -- link separator (between text and link)
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

-- allow other editor features to recognize this special markup
function MarkupIsSpecial(style) return style == 31 end

local function q(s) return s:gsub('(.)','%%%1') end

local MD_MARK_PTRN = ''  -- combination of all markup marks that can start styling
for key,value in pairs(markup) do
  styles[key] = value
  if key ~= MD_MARK_MARK then MD_MARK_PTRN = MD_MARK_PTRN .. q(key) end
end

function MarkupHotspotClick(pos, editor)
  -- check if this is "our" hotspot event
  if bit.band(editor:GetStyleAt(pos),31) ~= markup[MD_MARK_LINK].st then
    -- not "our" style, so nothing to do for us here
    return
  end
  local line = editor:LineFromPosition(pos)
  local tx = editor:GetLine(line)
  pos = pos + #MD_MARK_LINK - editor:PositionFromLine(line) -- turn into relative position

  -- extract the URL/command on the right side of the separator
  local _,_,text = string.find(tx, q(MD_MARK_LSEP).."([^%s]-)"..q(MD_MARK_LINT), pos)
  if text then
    local filepath = ide.openDocuments[editor:GetId()].filePath
    local _,_,shell = string.find(text, [[^macro:shell%((.*%S)%)$]])
    local _,_,http = string.find(text, [[^(http:%S+)$]])
    local _,_,command = string.find(text, [[^macro:(%w+)$]])
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
        (newwindow or SaveModifiedDialog(editor, true) ~= wx.wxID_CANCEL) then
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
    local st,_,sep,more = string.find(tx, "(["..MD_MARK_PTRN.."])(.)", start)
    if not st then return end

    -- check if this is a first character of a multi-character separator
    if not markup[sep] then sep = sep .. (more or '') end

    local s,e,cap
    local qsep = q(sep)
    local nonsep = ("[^%s]"):format(qsep)
    local nonspace = ("[^%s]"):format(qsep.."%s")
    if sep == MD_MARK_HEAD then
      -- always search from the start of the line
      -- [%w%p] set is needed to avoid continuing this markup to the next line
      s,e,cap = string.find(tx,"^("..q(MD_MARK_HEAD)..".+[%w%p])")
    elseif sep == MD_MARK_LINK then
      -- allow everything except spaces in the second part
      s,e,cap = string.find(tx,"^("..q(MD_MARK_LINK)..nonspace..".-"..nonspace
                                   ..q(MD_MARK_LSEP).."[^%s][^%s]-"
                                   ..q(MD_MARK_LINT)..")", st)
    elseif markup[sep] then
      -- try 2+ characters between separators first
      -- if not found, try a single character
      s,e,cap = string.find(tx,"^("..qsep..nonspace..nonsep.."-"..nonspace..qsep..")", st)
      if not s then s,e,cap = string.find(tx,"^("..qsep..nonspace..qsep..")", st) end
    end
    if s and -- selected markup is surrounded by spaces or punctuation
      (s == start or tx:sub(s-1, s-1):match("[%s%p]")) and
      (e-s == #tx-1 or tx:sub(e+1, e+1):match("[%s%p]"))
      then return s,e,cap,sep end
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
        -- only style comments and only those that are not at the beginning
        -- of the file to avoid styling shebang (#!) lines
        if iscomment[s] and p > 0 then
          local smark = #mark
          local emark = #mark -- assumes end mark is the same length as start mark
          if mark == MD_MARK_HEAD then
            -- grab multiple MD_MARK_HEAD if present
            local _,_,full = string.find(w,"^("..q(MD_MARK_HEAD).."+)")
            smark,emark = #full,0
          elseif mark == MD_MARK_LINK then
            local lsep = w:find(q(MD_MARK_LSEP))
            if lsep then emark = #w-lsep+#MD_MARK_LINT end
          end
          editor:StartStyling(p, 31)
          editor:SetStyling(smark, markup[MD_MARK_MARK].st)
          editor:SetStyling(t-f+1-smark-emark, markup[mark].st or markup[MD_MARK_MARK].st)
          editor:SetStyling(emark, markup[MD_MARK_MARK].st)
        end

        off = off + t
      end
      from = t and (t+1)
    end
  end
end

-- this could work by calling MarkupStyle directly from EVT_UPDATEUI,
-- but the styling didn't work correctly as the style on block comments
-- (which is used to identify where the markup should be applied)
-- was not always correct during UPDATEUI event.
-- to rectify this, we style immediately (by calling MarkupStyle
-- from UPDATEUI), but also store the starting point and re-style during
-- the next UPDATEUI/IDLE event when the block comment style is correct.
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
