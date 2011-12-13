-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  exts = {"prt","shd","mtl"},
  lexer = wxstc.wxSTC_LEX_POV,
  apitype = "luxres",
  linecomment = "//",
  lexerstyleconvert = {
    text = {wxstc.wxSTC_POV_IDENTIFIER,},

    lexerdef = {wxstc.wxSTC_POV_DEFAULT,},
    comment = {wxstc.wxSTC_POV_COMMENT,
      wxstc.wxSTC_POV_COMMENTLINE,},
    stringtxt = {wxstc.wxSTC_POV_STRING,},
    stringeol = {wxstc.wxSTC_POV_STRINGEOL,},
    --preprocessor= {wxstc.wxSTC_POV_PREPROCESSOR,},
    operator = {wxstc.wxSTC_POV_OPERATOR,},
    number = {wxstc.wxSTC_POV_NUMBER,},

    keywords0 = {wxstc.wxSTC_POV_WORD2,},
    keywords1 = {wxstc.wxSTC_POV_WORD3,},
    keywords2 = {wxstc.wxSTC_POV_WORD4,},
    keywords3 = {wxstc.wxSTC_POV_WORD5,},
    keywords4 = {wxstc.wxSTC_POV_WORD6,},
    keywords5 = {wxstc.wxSTC_POV_WORD7,},
    keywords6 = {wxstc.wxSTC_POV_WORD8,},
  },

  keywords = {
    -- word0 doesnt exist in lexer
    "",
    [[RenderFlag Color Texture Forces SubSystem Emitter Particle Technique GpuProgram Shader
    NewPass DrawPass ]],

    [[control floatmod texcontrol shdcontrol texconst texcenter texscale texrotate texmove texgenplane texclamp
    frames delay loop param alpha rfalpha layer alphaTEX alphafunc texmatrixcolum
    blendmode texcoord count type size width height axis model rate
    alphaTEX alphamode stateflag velocity endtime maxoffsetdist velocityvar flipdirection spread restarttime
    restarts life size sizeage3 sizevar lifevar rotate rotatevar rotateoffset rotateage3
    RGBAvar pointparams speedage3 numcolor numtex originoffset gravity wind trail normal
    translated instancemesh rotateagetex speedagetex sizeagetex RGBAagetex RGBAvar TEXPROJ TEXCUBE TEXALPHA
    TEX TEXDOTZ VTEXPROJ VTEXCUBE VTEXALPHA VTEX VTEXDOTZ SHD RGBA BASE
    SKIN FOGGED TEXCOMBINE1D MATERIAL TEXCOMBINE2D_16 TEXCOMBINE2D_32 ]],

    [[VID_REPLACE VID_DECAL VID_DECAL_PREV VID_DECAL_VERTEX VID_DECAL_CONST VID_MODULATE VID_ADD VID_AMODADD VID_AMODADD_PREV VID_AMODADD_VERTEX
    VID_AMODADD_CONST VID_AMODADD_CONSTMOD VID_NORMALMAPTAN VID_DECAL_CONSTMOD VID_LIGHTPOS VID_LIGHTCOLOR VID_LIGHTDIR VID_CAMPOS VID_CAMDIR VID_ARRAY
    VID_LIGHTAMBIENT VID_VALUE VID_LIGHTRANGE VID_RANDOM VID_TIME VID_TEXCONST VID_TEXSIZE VID_TEXSIZEINV VID_TEXLMSCALE VID_TEXMAT0
    VID_TEXMAT1 VID_TEXMAT2 VID_TEXMAT3 VID_TEXGEN0 VID_TEXGEN1 VID_TEXGEN2 VID_TEXGEN3 RENDER_BLEND RENDER_NOVERTEXCOLOR RENDER_ALPHATEST
    RENDER_STENCILTEST RENDER_NODEPTHTEST RENDER_NOCULL RENDER_FRONTCULL RENDER_NOCOLORMASK RENDER_NODEPTHMASK RENDER_STENCILMASK RENDER_LIT ADD SIN
    COS ZIGZAG USER_TEX LIGHTMAP ATTENUATE3D NORMALIZE SKYBOX SPECULAR DIFFUSE DUMMY
    VID_DEFAULT VID_LOWDETAIL VID_ARB_V VID_ARB_V_TEX4 VID_ARB_V_TEX8 VID_ARB_TEXCOMB VID_ARB_TEXCOMB_TEX4 VID_ARB_VF VID_ARB_VF_TEX4 VID_ARB_VF_TEX8
    VID_CG_SM3_TEX8 VID_CG_SM3 VID_CG_SM4 VID_CG_SM4_GS GL_NEVER GL_ALWAYS GL_LESS GL_GREATER GL_LEQUAL GL_GEQUAL
    GL_EQUAL GL_NOTEQUAL VID_POINT VID_CIRCLE VID_SPHERE VID_RECTANGLE VID_MODEL VID_QUAD VID_TRIANGLE VID_HSPHERE
    VID_DIR VID_ODIR CAP_MODADD CAP_COMBINE4 CAP_TEX3D CAP_TEXFLOAT ]],

    [[reflectmap blendinvertalpha spheremap screenmap interpolate skyreflectmap nocolorarray lit unlit sunlit
    nocull nodepthmask alphamask eyelinmap normalmap sunreflectmap sunnormalmap vertexcolored tangents normals
    colorpass nodepthtest sort novistest depthmask nodraw depthcompare depthvalue nomipmap fog
    rotatevelocity dieonfrontplane camrotfix noagedeath pointsmooth noage eventimed sequence combinedraw skymatrix
    lightmapscale rgbscale2 rgbscale4 alphascale2 alphascale4 lightreflectmap0 lightreflectmap1 lightreflectmap2 lightreflectmap3 lightnormalmap0
    lightnormalmap1 lightnormalmap2 lightnormalmap3 lowCgProfile VPROG VCG FCG GCG FPROG FFIXED
    VFIXED GFIXED ]],

    [[luxinia_ParticleSys_v120 luxinia_Shader_v310 luxinia_Material_v110 IF ELSEIF ELSE ]],

  },
}
