--[[// lxs | Lux Scene
typedef enum lxMeshIndexType_e
{
    LUX_MESH_INDEX_UINT16 , LUX_MESH_INDEX_UINT32 , LUX_MESH_INDICES , }
lxMeshIndexType_t ;
void lxMeshPlane_getCounts ( int segs [ 2 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices ) ;
void lxMeshPlane_initTriangles ( int segs [ 2 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices ) ;
void lxMeshPlane_initOutline ( int segs [ 2 ] , uint32 * indices ) ;
void lxMeshDisc_getCounts ( int segs [ 2 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices ) ;
void lxMeshDisc_initTriangles ( int segs [ 2 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices ) ;
void lxMeshDisc_initOutline ( int segs [ 2 ] , uint32 * indices ) ;
void lxMeshBox_getCounts ( int segs [ 3 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices ) ;
void lxMeshBox_initTriangles ( int segs [ 3 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices ) ;
void lxMeshBox_initOutline ( int segs [ 3 ] , uint32 * indices ) ;
void lxMeshSphere_getCounts ( int segs [ 2 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices ) ;
void lxMeshSphere_initTriangles ( int segs [ 2 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices ) ;
void lxMeshSphere_initOutline ( int segs [ 2 ] , uint32 * indices ) ;
void lxMeshCylinder_getCounts ( int segs [ 3 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices ) ;
void lxMeshCylinder_initTriangles ( int segs [ 3 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices ) ;
void lxMeshCylinder_initOutline ( int segs [ 3 ] , uint32 * indices ) ;
void * lxVertexCacheOptimize_tipsify ( void * indices , int nTriangles , int nVertices , int k , lxMeshIndexType_t type ) ;
void * lxVertexCacheOptimize_forsyth ( void * indices , int nTriangles , int nVertices , int vcache , lxMeshIndexType_t type ) ;
void * lxVertexCacheOptimize_grid_castano ( void * indices , int maxTriangles , int width , int height , int vcache , lxMeshIndexType_t type , int * writtenTriangles ) ;


]]  
--auto-generated api from ffi headers
local api =
  {
  ["LUX_MESH_INDEX_UINT16"] = { type ='value', },
  ["LUX_MESH_INDEX_UINT32"] = { type ='value', },
  ["LUX_MESH_INDICES"] = { type ='value', },
  ["lxMeshPlane_getCounts"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices)", },
  ["lxMeshPlane_initTriangles"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices)", },
  ["lxMeshPlane_initOutline"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , uint32 * indices)", },
  ["lxMeshDisc_getCounts"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices)", },
  ["lxMeshDisc_initTriangles"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices)", },
  ["lxMeshDisc_initOutline"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , uint32 * indices)", },
  ["lxMeshBox_getCounts"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 3 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices)", },
  ["lxMeshBox_initTriangles"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 3 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices)", },
  ["lxMeshBox_initOutline"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 3 ] , uint32 * indices)", },
  ["lxMeshSphere_getCounts"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices)", },
  ["lxMeshSphere_initTriangles"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices)", },
  ["lxMeshSphere_initOutline"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 2 ] , uint32 * indices)", },
  ["lxMeshCylinder_getCounts"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 3 ] , int * numVertices , int * numTriangleIndices , int * numOutlineIndices)", },
  ["lxMeshCylinder_initTriangles"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 3 ] , lxVector3 * pos , lxVector3 * normal , lxVector2 * uv , uint32 * indices)", },
  ["lxMeshCylinder_initOutline"] = { type ='function', 
    description = "", 
    returns = "()",
    valuetype = nil,
    args = "(int segs [ 3 ] , uint32 * indices)", },
  ["lxVertexCacheOptimize_tipsify"] = { type ='function', 
    description = "", 
    returns = "(void *)",
    valuetype = nil,
    args = "(void * indices , int nTriangles , int nVertices , int k , lxMeshIndexType_t type)", },
  ["lxVertexCacheOptimize_forsyth"] = { type ='function', 
    description = "", 
    returns = "(void *)",
    valuetype = nil,
    args = "(void * indices , int nTriangles , int nVertices , int vcache , lxMeshIndexType_t type)", },
  ["lxVertexCacheOptimize_grid_castano"] = { type ='function', 
    description = "", 
    returns = "(void *)",
    valuetype = nil,
    args = "(void * indices , int maxTriangles , int width , int height , int vcache , lxMeshIndexType_t type , int * writtenTriangles)", },
  }
return {
  lxs = {
    type = 'lib',
    description = "Lux Scene",
    childs = api,
  },
}
