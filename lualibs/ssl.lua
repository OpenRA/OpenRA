------------------------------------------------------------------------------
-- LuaSec 0.6
-- Copyright (C) 2006-2016 Bruno Silvestre
--
------------------------------------------------------------------------------

if package.config:sub(1,1) == "\\" then -- Windows only
  -- this hack is needed because OpenSSL libraries may be available elsewhere
  -- in PATH and un/loading them may lead to crashes in the application.
  -- loading them using the full path ensures that the right versions are un/loaded.
  -- don't need to request any function as only need to load libraries. -- PK
  local bindir = debug.getinfo(1,'S').source:gsub("[^/\\]+$",""):gsub("^@","").."../bin/"
  package.loadlib(bindir.."libeay32.dll", "")
  package.loadlib(bindir.."ssleay32.dll", "")
end

local core    = require("ssl.core")
local context = require("ssl.context")
local x509    = require("ssl.x509")

local unpack  = table.unpack or unpack

-- We must prevent the contexts to be collected before the connections,
-- otherwise the C registry will be cleared.
local registry = setmetatable({}, {__mode="k"})

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
local function newcontext(cfg)
   local succ, msg, ctx
   -- Create the context
   ctx, msg = context.create(cfg.protocol)
   if not ctx then return nil, msg end
   -- Mode
   succ, msg = context.setmode(ctx, cfg.mode)
   if not succ then return nil, msg end
   -- Load the key
   if cfg.key then
      if cfg.password and
         type(cfg.password) ~= "function" and
         type(cfg.password) ~= "string"
      then
         return nil, "invalid password type"
      end
      succ, msg = context.loadkey(ctx, cfg.key, cfg.password)
      if not succ then return nil, msg end
   end
   -- Load the certificate
   if cfg.certificate then
     succ, msg = context.loadcert(ctx, cfg.certificate)
     if not succ then return nil, msg end
     if cfg.key and context.checkkey then
       succ = context.checkkey(ctx)
       if not succ then return nil, "private key does not match public key" end
     end
   end
   -- Load the CA certificates
   if cfg.cafile or cfg.capath then
      succ, msg = context.locations(ctx, cfg.cafile, cfg.capath)
      if not succ then return nil, msg end
   end
   -- Set SSL ciphers
   if cfg.ciphers then
      succ, msg = context.setcipher(ctx, cfg.ciphers)
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

   -- NOTE: Setting DH parameters and elliptic curves needs to come after
   -- setoptions(), in case the user has specified the single_{dh,ecdh}_use
   -- options.

   -- Set DH parameters
   if cfg.dhparam then
      if type(cfg.dhparam) ~= "function" then
         return nil, "invalid DH parameter type"
      end
      context.setdhparam(ctx, cfg.dhparam)
   end
   -- Set elliptic curve
   if cfg.curve then
      succ, msg = context.setcurve(ctx, cfg.curve)
      if not succ then return nil, msg end
   end
   -- Set extra verification options
   if cfg.verifyext and ctx.setverifyext then
      succ, msg = optexec(ctx.setverifyext, cfg.verifyext, ctx)
      if not succ then return nil, msg end
   end

   return ctx
end

--
--
--
local function wrap(sock, cfg)
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
      sock:setfd(-1)
      registry[s] = ctx
      return s
   end
   return nil, msg 
end

--
-- Extract connection information.
--
local function info(ssl, field)
  local str, comp, err, protocol
  comp, err = core.compression(ssl)
  if err then
    return comp, err
  end
  -- Avoid parser
  if field == "compression" then
    return comp
  end
  local info = {compression = comp}
  str, info.bits, info.algbits, protocol = core.info(ssl)
  if str then
    info.cipher, info.protocol, info.key,
    info.authentication, info.encryption, info.mac =
        string.match(str, 
          "^(%S+)%s+(%S+)%s+Kx=(%S+)%s+Au=(%S+)%s+Enc=(%S+)%s+Mac=(%S+)")
    info.export = (string.match(str, "%sexport%s*$") ~= nil)
  end
  if protocol then
    info.protocol = protocol
  end
  if field then
    return info[field]
  end
  -- Empty?
  return ( (next(info)) and info )
end

--
-- Set method for SSL connections.
--
core.setmethod("info", info)

--------------------------------------------------------------------------------
-- Export module
--

local _M = {
  _VERSION        = "0.6",
  _COPYRIGHT      = core.copyright(),
  loadcertificate = x509.load,
  newcontext      = newcontext,
  wrap            = wrap,
}

return _M
