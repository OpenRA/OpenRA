-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

return {
  -- unfortunately no good lexer
  -- ASM comments start with ;
  -- and TCL doesnt allow coloring
  -- behind keywords

  exts = {"glp","vp","fp"},
  lexer = wxstc.wxSTC_LEX_TCL,
  --apitype = "cg",
  linecomment = "#",
  lexerstyleconvert = {
    text = {4,5,7,8,9,10,11,
    },

    lexerdef = {0,},
    comment = {1,2,20,21,
    },
    --stringtxt = {wxstc.wxSTC_SQL_STRING,
    -- wxstc.wxSTC_SQL_CHARACTER,
    -- },
    --stringeol = {wxstc.wxSTC_SQL_STRINGEOL,},
    --preprocessor= {,},
    operator = {6,},
    number = {3,
    },

    keywords0 = {12,},
    keywords1 = {13,},
    keywords2 = {14,},
    keywords3 = {15,},
  },

  keywords = {
    [[EMIT ENDPRIM
    ABS ADD AND BRK CAL CEIL CMP CONT COS DIV DP2 DP2A DP3 DP4 DPH DST ELSE
    ENDIF ENDREP EX2 FLR FRC I2F IF KIL LG2 LIT LRP MAD MAX MIN MOD MOV MUL
    NOT NRM OR PK2H PK2US PK4B PK4UB POW RCC RCP REP RET RFL ROUND RSQ SAD
    SCS SEQ SFL SGE SGT SHL SHR SIN SLE SLT SNE SSG STR SUB SWZ TEX TRUNC LOOP
    TXB TXD TXF TXL TXP TXQ UP2H UP2US UP4B UP4UB X2D XOR XPD REP.S REP.F REP.U
    ENDLOOP SUBROUTINENUM CALI

    ABS_SAT ADD_SAT CEIL_SAT CMP_SAT COS_SAT DIV_SAT DP2_SAT DP2A_SAT DP3_SAT
    DP4_SAT DPH_SAT DST_SAT EX2_SAT FLR_SAT FRC_SAT LG2_SAT LIT_SAT LRP_SAT
    MAD_SAT MAX_SAT MIN_SAT MOV_SAT MUL_SAT NRM_SAT POW_SAT RCC_SAT RCP_SAT
    RFL_SAT ROUND_SAT RSQ_SAT SCS_SAT SEQ_SAT SFL_SAT SGE_SAT SGT_SAT SIN_SAT
    SLE_SAT SLT_SAT SNE_SAT SSG_SAT STR_SAT SUB_SAT SWZ_SAT TEX_SAT TRUNC_SAT
    TXB_SAT TXD_SAT TXF_SAT TXL_SAT TXP_SAT UP2H_SAT UP2US_SAT UP4B_SAT UP4UB_SAT
    X2D_SAT XPD_SAT

    ]],
    [[

    ATTRIB PARAM TEMP ADDRESS OUTPUT ALIAS OPTION TEXTURE
    PRIMITIVE_IN PRIMITIVE_OUT VERTICES_OUT POINTS LINES LINES_ADJACENCY
    TRIANGLES TRIANGLES_ADJACENCY
    LINE_STRIP TRIANGLE_STRIP
    EQ GE GT LE LT NE TR FL EQ0 GE0 GT0 LE0 LT0 NE0 TR0 FL0 EQ1 GE1 GT1 LE1 LT1
    NE1 TR1 FL1 NAN NAN0 NAN1 LEG LEG0 LEG1 CF CF0 CF1 NCF NCF0 NCF1 OF OF0 OF1
    NOF NOF0 NOF1 AB AB0 AB1 BLE BLE0 BLE1 SF SF0 SF1 NSF NSF0 NSF1
    END SUBROUTINETYPE SUBROUTINE

    ]],
    [[

    vertex position weight normal color primary secondary fogcoord texcoord
    matrixindex attrib
    program env local fragment
    state material ambient diffuse specular emission shininess front back
    light attenuation spot direction half
    lightmodel scene lightprod
    texgen eye object s t r q
    fog params
    clip plane
    point size attenuation
    matrix modelview projection mvp texture palette row transpose inverse invtrans
    result pointsize 1D 2D 3D CUBE RECT

    ]],
    [[

    x y z w xxxx xxxy xxxz xxxw
    xxyx xxyy xxyz xxyw xxzx xxzy xxzz xxzw xxwx xxwy
    xxwz xxww xyxx xyxy xyxz xyxw xyyx xyyy xyyz xyyw
    xyzx xyzy xyzz xyzw xywx xywy xywz xyww xzxx xzxy
    xzxz xzxw xzyx xzyy xzyz xzyw xzzx xzzy xzzz xzzw
    xzwx xzwy xzwz xzww xwxx xwxy xwxz xwxw xwyx xwyy
    xwyz xwyw xwzx xwzy xwzz xwzw xwwx xwwy xwwz xwww
    yxxx yxxy yxxz yxxw yxyx yxyy yxyz yxyw yxzx yxzy
    yxzz yxzw yxwx yxwy yxwz yxww yyxx yyxy yyxz yyxw
    yyyx yyyy yyyz yyyw yyzx yyzy yyzz yyzw yywx yywy
    yywz yyww yzxx yzxy yzxz yzxw yzyx yzyy yzyz yzyw
    yzzx yzzy yzzz yzzw yzwx yzwy yzwz yzww ywxx ywxy
    ywxz ywxw ywyx ywyy ywyz ywyw ywzx ywzy ywzz ywzw
    ywwx ywwy ywwz ywww zxxx zxxy zxxz zxxw zxyx zxyy
    zxyz zxyw zxzx zxzy zxzz zxzw zxwx zxwy zxwz zxww
    zyxx zyxy zyxz zyxw zyyx zyyy zyyz zyyw zyzx zyzy
    zyzz zyzw zywx zywy zywz zyww zzxx zzxy zzxz zzxw
    zzyx zzyy zzyz zzyw zzzx zzzy zzzz zzzw zzwx zzwy
    zzwz zzww zwxx zwxy zwxz zwxw zwyx zwyy zwyz zwyw
    zwzx zwzy zwzz zwzw zwwx zwwy zwwz zwww wxxx wxxy
    wxxz wxxw wxyx wxyy wxyz wxyw wxzx wxzy wxzz wxzw
    wxwx wxwy wxwz wxww wyxx wyxy wyxz wyxw wyyx wyyy
    wyyz wyyw wyzx wyzy wyzz wyzw wywx wywy wywz wyww
    wzxx wzxy wzxz wzxw wzyx wzyy wzyz wzyw wzzx wzzy
    wzzz wzzw wzwx wzwy wzwz wzww wwxx wwxy wwxz wwxw
    wwyx wwyy wwyz wwyw wwzx wwzy wwzz wwzw wwwx wwwy
    wwwz wwww xy xz yz xyz xw yw xyw zw
    xzw yzw xyzw ]],

  },
}
