-- Copyright (C) Paul Kulchenko 2011-2012
-- update styles for comment markup
local styles = ide.config.styles
local comment = styles.comment
local MD_MARK_ITAL = '/' -- italic
local MD_MARK_BOLD = '*' -- bold
local MD_MARK_LINK = '~' -- link
local MD_MARK_HEAD = '!' -- header
local MD_MARK_CODE = '@' -- code
local MD_MARK_LSEP = ';' -- link separator (between text and link)
local MD_MARK_MARK = ' ' -- separator
local markup = {
  [MD_MARK_CODE] = {st=26, fg={127,127,127}, bg=comment.bg, fs=9},
  [MD_MARK_HEAD] = {st=27, fg=comment.fg, bg=comment.bg, fn="Lucida Console", fs=10, b=true},
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

function MarkupHotspotClick(event, editor)
  local pos = event:GetPosition()
  -- check if this is "our" hotspot event
  if bit.band(editor:GetStyleAt(pos),31) ~= markup[MD_MARK_LINK].st then
    -- not "our" style, so nothing to do for us here
    return
  end
  local line = editor:LineFromPosition(pos)
  local tx = editor:GetLine(line)
  pos = 1 + pos - editor:PositionFromLine(line) -- turn into relative position

  -- find the separator on the right side of the position
  local poss = string.find(tx, MD_MARK_LSEP, pos, true)
  local pose = string.find(tx, MD_MARK_LINK, pos, true)

  if (poss and pose) then 
    local text = string.sub(tx, poss+1, pose-1)
    local wxfilepath = GetEditorFileAndCurInfo()
    local name = wxfilepath:GetPath(wx.wxPATH_GET_VOLUME) .. string_Pathsep .. text
    local _,_,macro = string.find(text, "^macro:shell%((.*%S)%)")
    if macro then
      local bottomnotebook = ide.frame.bottomnotebook
      local shellbox = bottomnotebook.shellbox
      local index = bottomnotebook:GetPageIndex(shellbox)
      if index then bottomnotebook:SetSelection(index) end
      ShellExecuteCode(macro)
    elseif wx.wxFileName(name):FileExists() then
      LoadFile(name,nil,true)
    end
  end
  event:Skip()
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
      s,e,cap = string.find(tx,"(%"..MD_MARK_LINK.."[%w%p]"..any.."[%w%p]"..
                                "%"..MD_MARK_LSEP.."[%w%p]"..any.."[%w%p]"..
                                "%"..MD_MARK_LINK..")", start)
    else
      s,e,cap = string.find(tx,"(%"..sep.."[%w%p][^%"..sep.."]-[%w%p]%"..sep..")", start)
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
  local linee = linee or editor:GetLineCount()-1
  local linef = editor:GetLineCount()

  local iscomment = {}
  for i,v in pairs(editor.spec.iscomment) do
    iscomment[i] = v
  end

  for line=lines,linef do
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
