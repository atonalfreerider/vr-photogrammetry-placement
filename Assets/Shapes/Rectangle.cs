using UnityEngine;

namespace Shapes
{
    public class Rectangle : Polygon
    {
        // the current width and height of the rectangle
        public float curW;
        public float curH;

        public void DrawRect(float w, float h, float d)
        {
            curH = h;
            curW = w;

            Vector3[] skinList = new Vector3[4];

            skinList[0] = new Vector3(-w * .5f, 0, h * .5f);
            skinList[1] = new Vector3(w * .5f, 0, h * .5f);
            skinList[2] = new Vector3(w * .5f, 0, -h * .5f);
            skinList[3] = new Vector3(-w * .5f, 0, -h * .5f);

            int[] indList = {0, 1, 2, 0, 2, 3};

            if (d <= float.Epsilon)
            {
                // a rectangle with 0 depth - only draw two sides
                Draw3DPoly(skinList, MirrorIndices(indList, 0));
            }
            else
            {
                Extrude(skinList, indList, d, false, true, 0);
            }
        }

        public void LoadTexture(byte[] image)
        {
            // Don't use mipmaps because we want to show all the high-density data contained in the texture.
            Texture2D texture =
                new(100, 100, TextureFormat.RGBA32, false)
                    {filterMode = FilterMode.Point};

            texture.LoadImage(image);
            rend.material.SetTexture(Shader.PropertyToID("_MainTex"), texture);
            rend.material.color = new Color(1, 1, 1, .5f);
        }
    }

    public static class NewCube
    {
        // rectangles
        public static Rectangle textureRectPoly;

        public static void InitCube(PolygonFactory polygonFactory)
        {
            textureRectPoly = PolygonFactory.NewRectPoly(PolygonFactory.Instance.mainMat);
            textureRectPoly.DrawRect(1, 1, 0);
            Mesh textureMesh = textureRectPoly.meshFilter.mesh;
            Vector3[] vertices = textureMesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(vertices[i].x - .5f, vertices[i].z - .5f);
            }

            textureMesh.uv = uvs;
            textureRectPoly.name = "textureRectPoly";
            textureRectPoly.transform.SetParent(polygonFactory.transform, false);
            textureRectPoly.transform.gameObject.SetActive(false);
        }
    }
}