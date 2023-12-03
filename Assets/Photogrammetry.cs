using System;
using System.Collections.Generic;
using System.Linq;
using Render;
using Shapes;
using UnityEngine;

public class Photogrammetry : MonoBehaviour
{
    const int IterationsPerSide = 100;
    const float StepSize = .01f;

    readonly Dictionary<int, Color> colorDictionary = new();
    readonly Dictionary<int, List<MeshFilter>> meshDictionary = new();
    readonly Dictionary<int, MeshCombiner> meshCombinerDictionary = new();

    void Awake()
    {
        colorDictionary.Add(0, Color.black);
        colorDictionary.Add(1, Color.white);
        colorDictionary.Add(2, new Color(.7f, .7f, 1));
        colorDictionary.Add(3, new Color(.3f, .3f, .7f));
        colorDictionary.Add(4, new Color(.8f, .6f, .5f));
        colorDictionary.Add(5, new Color(1f, .1f, .1f));

        for (int i = 0; i < colorDictionary.Count; i++)
        {
            meshDictionary.Add(i, new List<MeshFilter>());
            MeshCombiner meshCombiner = gameObject.AddComponent<MeshCombiner>();
            meshCombinerDictionary.Add(i, meshCombiner);
        }
    }

    public void Run(List<CameraSetup> cameras)
    {
        float length = IterationsPerSide * StepSize;

        List<Vector3> cameraPositions = cameras.Select(x => x.transform.position).ToList();

        for (float x = 0; x < length; x += StepSize)
        {
            for (float y = 0; y < length * 2; y += StepSize)
            {
                for (float z = 0; z < length; z += StepSize)
                {
                    Vector3 point = new Vector3(x, y, z);
                    List<Color?> colors = cameras
                        .Select(cameraSetup => cameraSetup.ColorFromIntersection(point)).ToList();

                    int i = 0;
                    foreach (Color? color in colors)
                    {
                        if (color == null)
                        {
                            i++;
                            continue;
                        }

                        int myMatches = 0;

                        int j = 0;
                        foreach (Color? color1 in colors)
                        {
                            if (i == j || color1 == null)
                            {
                                j++;
                                continue;
                            }

                            float colorDistance = ColorDistance(color.Value, color1.Value);
                            if (colorDistance < .3f)
                            {
                                myMatches++;
                            }

                            j++;
                        }

                        if (myMatches > 1)
                        {
                            Polygon pixel = DrawPixel(point, myMatches);
                            pixel.transform.LookAt(cameraPositions[i]);
                            pixel.transform.Rotate(Vector3.right, 90);

                            int closestDIndex = 0;
                            float closestD = 10;
                            for (int k = 0; k < colorDictionary.Count; k++)
                            {
                                float sortD = ColorDistance(color.Value, colorDictionary[k]);
                                if (sortD < closestD)
                                {
                                    closestD = sortD;
                                    closestDIndex = k;
                                }
                            }

                            meshDictionary[closestDIndex].Add(pixel.meshFilter);
                        }

                        i++;
                    }
                }
            }
        }

        for (int i = 0; i < colorDictionary.Count; i++)
        {
            MeshCombiner meshCombiner = meshCombinerDictionary[i];
            meshCombiner.Init(meshDictionary[i].ToArray(), transform, colorDictionary[i]);
            meshCombiner.RecreateCombines();
            meshCombiner.SetDisplayStateCombinesAndIndividuals(true, false);
        }
    }

    static float ColorDistance(Color color1, Color color2)
    {
        float r = Mathf.Abs(color1.r - color2.r);
        float g = Mathf.Abs(color1.g - color2.g);
        float b = Mathf.Abs(color1.b - color2.b);
        return r + g + b;
    }

    Polygon DrawPixel(Vector3 pos, float size)
    {
        Polygon triangle = Instantiate(PolygonFactory.Instance.tri);
        triangle.gameObject.SetActive(true);
        triangle.transform.SetParent(transform, false);
        triangle.transform.localPosition = pos;
        triangle.transform.localScale = Vector3.one * size * .002f;
        return triangle;
    }
}