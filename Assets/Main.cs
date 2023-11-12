using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Shapes;
using Shapes.Lines;
using UnityEngine;
using UnityEngine.InputSystem;

public class Main : MonoBehaviour
{
    [Header("Parameters")] public string PhotoFolderPath;

    string jsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<string, CameraSetup> cameras = new();
    int currentFrameNumber = 0;
    int currentSpearNumber = 0;
    public static Main Instance;
    readonly List<StaticLink> cameraLinks = new();

    Polygon leadTarget;
    Polygon followTarget;

    public class ImgMetadata
    {
        public readonly float FocalLength;
        public readonly int Width;
        public readonly int Height;

        public ImgMetadata(float focalLength, int width, int height)
        {
            FocalLength = 3;
            Width = width;
            Height = height;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(PhotoFolderPath))
        {
            DirectoryInfo root = new DirectoryInfo(PhotoFolderPath);

            int count = 0;
            SqliteInput sqliteInput = GetComponent<SqliteInput>();
            List<List<List<List<Vector2>>>> dancersByCamera = sqliteInput.ReadFrameFromDb();
            List<CameraSetup> cameraSetups = new();
            foreach (DirectoryInfo dir in root.EnumerateDirectories())
            {
                CameraSetup cameraSetup = new GameObject(dir.Name).AddComponent<CameraSetup>();
                cameraSetup.Init(dir.FullName, dancersByCamera[count]);
                cameras.Add(dir.Name, cameraSetup);

                cameraSetup.transform.SetParent(transform, false);
                cameraSetup.transform.Translate(Vector3.back * count * .01f);
                cameraSetup.SetFrame(0);
                cameraSetups.Add(cameraSetup);
                count++;
            }

            foreach (CameraSetup cameraSetup in cameraSetups)
            {
                foreach (CameraSetup setup in cameraSetups)
                {
                    if (setup.name == cameraSetup.name) continue;

                    StaticLink link = Instantiate(StaticLink.prototypeStaticLink);
                    link.transform.SetParent(transform, false);
                    link.LinkFromTo(cameraSetup.transform, setup.transform);
                    link.gameObject.SetActive(true);
                    link.SetColor(Color.magenta);
                    cameraLinks.Add(link);
                }
            }
        }
        else
        {
            // TODO load from resources
        }

        string json;
        if (File.Exists(jsonPath))
        {
            json = File.ReadAllText(jsonPath);
        }
        else
        {
            TextAsset jsonPositions = Resources.Load<TextAsset>("positions");
            json = jsonPositions != null ? jsonPositions.text : "{}";
        }

        Dictionary<string, PositionAndRotation> fromJsonPics =
            JsonConvert.DeserializeObject<Dictionary<string, PositionAndRotation>>(json);

        foreach (KeyValuePair<string, PositionAndRotation> positionAndRotation in fromJsonPics)
        {
            CameraSetup cameraSetup = cameras[positionAndRotation.Key];
            cameraSetup.transform.localPosition =
                positionAndRotation.Value.positionVector3;
            cameraSetup.transform.localRotation =
                positionAndRotation.Value.rotationQuaternion;
            cameraSetup.MovePhotoToDistance(positionAndRotation.Value.focal);
        }

        UpdateCameraLinks();

        leadTarget = Instantiate(PolygonFactory.Instance.icosahedron0);
        leadTarget.gameObject.SetActive(false);
        leadTarget.transform.SetParent(transform, false);
        leadTarget.SetColor(Color.red);
        leadTarget.transform.localScale = Vector3.one * .02f;

        followTarget = Instantiate(PolygonFactory.Instance.icosahedron0);
        followTarget.gameObject.SetActive(false);
        followTarget.transform.SetParent(transform, false);
        followTarget.SetColor(Color.red);
        followTarget.transform.localScale = Vector3.one * .02f;
    }

    public void Advance()
    {
        currentFrameNumber++;
        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(currentFrameNumber);
        }
    }

    public void Reverse()
    {
        currentFrameNumber--;
        if (currentFrameNumber < 0) currentFrameNumber = 0;
        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(currentFrameNumber);
        }
    }

    public void UpdateCameraLinks()
    {
        foreach (StaticLink cameraLink in cameraLinks)
        {
            cameraLink.UpdateLink();
        }
    }

    public void DrawNextSpear()
    {
        currentSpearNumber++;
        if (currentSpearNumber > 17) currentSpearNumber = 17;

        UpdateSpears();
    }

    public void DrawPreviousSpear()
    {
        currentSpearNumber--;
        if (currentSpearNumber < 0) currentSpearNumber = 0;
        
        UpdateSpears();
    }

    void UpdateSpears()
    {
        // blind group rays based on arbitrary 50/50 could be lead or follow
        List<Ray> allRays = new();
        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            Ray? ray1 = cameraSetup.DrawSpear(currentSpearNumber, true);
            if (ray1.HasValue)
            {
                allRays.Add(ray1.Value);
            }

            Ray? ray2 = cameraSetup.DrawSpear(currentSpearNumber, false);
            if (ray2.HasValue)
            {
                allRays.Add(ray2.Value);
            }
        }
       
        // find the locus of each of these lists
        Vector3 locus = RayMidpointFinder.FindMinimumMidpoint(allRays);

        // re-sort on the basis if they are closer to one locus or another
        Tuple<List<Ray>, List<Ray>> sortedRays = SortRays(allRays, locus);
        
        // finally set target for re-sorted lists
        Vector3 minMidpoint = RayMidpointFinder.FindMinimumMidpoint(sortedRays.Item1);
        leadTarget.transform.position = minMidpoint;
        leadTarget.gameObject.SetActive(true);
        
        Vector3 minFollowMidpoint = RayMidpointFinder.FindMinimumMidpoint(sortedRays.Item2);
        followTarget.transform.localPosition = minFollowMidpoint;
        followTarget.gameObject.SetActive(true);
    }

    static Tuple<List<Ray>,List<Ray>> SortRays(List<Ray> rays, Vector3 target)
    {
        List<Ray> leftRays = new List<Ray>();
        List<Ray> rightRays = new List<Ray>();

        foreach (var ray in rays)
        {
            Vector3 toRay = ray.origin - target;
            Vector3 cross = Vector3.Cross(Vector3.up, toRay);

            // Determine which side of the target the ray is on
            if (Vector3.Dot(cross, ray.direction) > 0)
            {
                rightRays.Add(ray);
            }
            else
            {
                leftRays.Add(ray);
            }
        }

        return new Tuple<List<Ray>, List<Ray>>(leftRays, rightRays);
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Serializer serializer = new Serializer(jsonPath);
            serializer.Serialize(cameras);
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            Advance();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            Reverse();
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            DrawNextSpear();
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            DrawPreviousSpear();
        }
    }
}