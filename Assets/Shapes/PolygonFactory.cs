using Shapes.Lines;
using UnityEngine;

namespace Shapes
{
    public class PolygonFactory : MonoBehaviour
    {
        public static PolygonFactory Instance;
        public PolygonPool PolygonPool;
        public Material mainMat;
        public Polygon icosahedron0, tri;
        
        void Awake()
        {
            Instance = this;
            BuildPolygons();
        }

        // INIT
        void BuildPolygons()
        {
            NewCube.InitCube(this);
            StaticLink.InitStaticLink(this);
            
            icosahedron0 = NewPoly(mainMat);
            Polyhedra.VertsAndFaces ivaf = Polyhedra.NewIcoVertsAndFaces(1, 0);
            Polyhedra.NewPolyhedron(icosahedron0, ivaf.verts, ivaf.faces, false);
            icosahedron0.name = "icosahedron0";
            icosahedron0.SetColor(Color.white);
            icosahedron0.transform.SetParent(transform, false);
            icosahedron0.gameObject.SetActive(false);
            
            tri = NewPoly(mainMat);
            tri.DrawRegPoly(1, 3, 0, 0, 0, true);
            tri.name = "triangle";
            tri.SetColor(Color.white);
            tri.transform.SetParent(transform, false);
            tri.gameObject.SetActive(false);
        }

        public static Polygon NewPoly(Material passMat)
        {
            Polygon newPoly = new GameObject("Polygon").AddComponent<Polygon>();
            AddMesh(newPoly.gameObject, newPoly, passMat);
            newPoly.rend = newPoly.gameObject.GetComponent<Renderer>();

            return newPoly;
        }
        
        public static Rectangle NewRectPoly(Material passMat)
        {
            Rectangle newPoly = new GameObject("RectPolygon").AddComponent<Rectangle>();
            AddMesh(newPoly.gameObject, newPoly, passMat);
            newPoly.rend = newPoly.gameObject.GetComponent<Renderer>();

            return newPoly;
        }

        public static void AddMesh(GameObject polyGO, Polygon basePoly, Material passMat)
        {
            // add mesh;
            MeshFilter filter = polyGO.AddComponent<MeshFilter>();
            filter.sharedMesh = new Mesh();
            MeshRenderer meshRend = polyGO.AddComponent<MeshRenderer>();
            meshRend.sharedMaterial = passMat;
            meshRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRend.receiveShadows = false;
            meshRend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            basePoly.meshFilter = filter;
        }
    }
}