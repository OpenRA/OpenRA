------------------------------------------------------------------------------
-- LuaSec 0.4.1
-- Copyright (C) 2006-2011 Bruno Silvestre
--
------------------------------------------------------------------------------

module("ssl", package.seeall)

require("ssl.core")
require("ssl.context")


_VERSION   = "0.4.1"
_COPYRIGHT = "LuaSec 0.4.1 - Copyright (C) 2006-2011 Bruno Silvestre\n" .. 
             "LuaSocket 2.0.2 - Copyright (C) 2004-2007 Diego Nehab"

-- Export functions
rawconnection = core.rawconnection
rawcontext    = context.rawcontext

--
--
--
local function optexec(func, param, ctx)
  if param then
    if type(param) == "table" then
      return func(ctx, unpack(param))
    else
      return func(ctx, param)
    end
  end
  return true
end

--
--
--
function newcontext(cfg)
   local succ, msg, ctx
   -- Create the context
   ctx, msg = context.create(cfg.protocol)
   if not ctx then return nil, msg end
   -- Mode
   succ, msg = context.setmode(ctx, cfg.mode)
   if not succ then return nil, msg end
   -- Load the key
   if cfg.key then
      succ, msg = context.loadkey(ctx, cfg.key, cfg.password)
      if not succ then return nil, msg end
   end
   -- Load the certificate
   if cfg.certificate then
      succ, msg = context.loadcert(ctx, cfg.certificate)
      if not succ then return nil, msg end
   end
   -- Load the CA certificates
   if cfg.cafile or cfg.capath then
      succ, msg = context.locations(ctx, cfg.cafile, cfg.capath)
      if not succ then return nil, msg end
   end
   -- Set the verification options
   succ, msg = optexec(context.setverify, cfg.verify, ctx)
   if not succ then return nil, msg end
   -- Set SSL options
   succ, msg = optexec(context.setoptions, cfg.options, ctx)
   if not succ then return nil, msg end
   -- Set the depth for certificate verification
   if cfg.depth then
      succ, msg = context.setdepth(ctx, cfg.depth)
      if not succ then return nil, msg end
   end
   return ctx
end

--
--
--
function wrap(sock, cfg)
   local ctx, msg
   if type(cfg) == "table" then
      ctx, msg = newcontext(cfg)
      if not ctx then return nil, msg end
   else
      ctx = cfg
   end
   local s, msg = core.create(ctx)
   if s then
      core.setfd(s, sock:getfd())
      sock:setfd(core.invalidfd)
      return s
   end
   return nil, msg 
end
