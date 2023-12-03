using System.Collections.Generic;
using System.Linq;
using Render;
using Shapes;
using UnityEngine;

public class Photogrammetry : MonoBehaviour
{
    const float length = 2.5f;
    const float StepSize = .005f;

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
        colorDictionary.Add(5, new Color(.7f, .5f, .35f));
        colorDictionary.Add(6, new Color(.8f, .1f, .1f));
        colorDictionary.Add(7, new Color(.6f, .1f, .1f));
        colorDictionary.Add(8, new Color(.4f, .1f, .1f));

        for (int i = 0; i < colorDictionary.Count; i++)
        {
            meshDictionary.Add(i, new List<MeshFilter>());
            MeshCombiner meshCombiner = gameObject.AddComponent<MeshCombiner>();
            meshCombinerDictionary.Add(i, meshCombiner);
        }
    }

    public void Run(List<CameraSetup> cameras, List<Collider> allPoses)
    {
        List<Vector3> cameraPositions = cameras.Select(x => x.transform.position).ToList();

        for (float x = -length; x < length; x += StepSize)
        {
            for (float y = 0; y < length; y += StepSize)
            {
                for (float z = -length; z < length; z += StepSize)
                {
                    Vector3 point = new Vector3(x, y, z);

                    // only attempt to render a pixel that is inside of an avatar
                    Collider? myCollider = null;
                    foreach (Collider collider in allPoses)
                    {
                        if (collider.bounds.Contains(point))
                        {
                            myCollider = collider;
                            break;
                        }
                    }

                    if (myCollider == null) continue;
                    
                    List<Collider> collidersToAvoid = allPoses.Where(x => x != myCollider).ToList();

                    // filter out cameras that are blocked by another limb or avatar
                    List<int> visibleCameraIndices = new();
                    int camCount = 0;
                    foreach (CameraSetup cameraSetup in cameras)
                    {
                        Ray ray = new Ray(point, point - cameraSetup.transform.position);
                        RaycastHit[] hits = Physics.RaycastAll(ray);
                        if (hits.Any())
                        {
                            List<Collider> hitColliders = hits.Select(x => x.collider).ToList();
                            bool isBroken = false;
                            foreach (Collider hitCollider in hitColliders)
                            {
                                if (myCollider == hitCollider) continue;
                                if (collidersToAvoid.Contains(hitCollider))
                                {
                                    isBroken = true;
                                    camCount++;
                                    break;
                                }
                            }

                            if (isBroken) continue;

                            visibleCameraIndices.Add(camCount);
                            camCount++;
                        }
                        else
                        {
                            camCount++;
                        }
                    }

                    // compare color distance of each camera to create pixel
                    foreach (int visibleCameraIndex in visibleCameraIndices)
                    {
                        int myMatches = 0;
                        Color? color = cameras[visibleCameraIndex].ColorFromIntersection(point);
                        if(color == null) continue;
                        foreach (int otherCamIndex in visibleCameraIndices)
                        {
                            if(visibleCameraIndex == otherCamIndex) continue;
                            Color? otherColor = cameras[otherCamIndex].ColorFromIntersection(point);
                            if(otherColor == null) continue;
                            
                            float colorDistance = ColorDistance(color.Value, otherColor.Value);
                            if (colorDistance < .35f)
                            {
                                myMatches++;
                            }
                        }
                        
                        if (myMatches > 1)
                        {
                            // draw pixel
                            Polygon pixel = DrawPixel(point, myMatches);
                            pixel.transform.LookAt(cameraPositions[visibleCameraIndex]);
                            pixel.transform.Rotate(Vector3.right, 90);

                            // sort into combiner group
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
        Polygon triangle = Instantiate(PolygonFactory.Instance.quad);
        triangle.gameObject.SetActive(true);
        triangle.transform.SetParent(transform, false);
        triangle.transform.localPosition = pos;
        triangle.transform.localScale = Vector3.one * (size * .002f);
        return triangle;
    }
}