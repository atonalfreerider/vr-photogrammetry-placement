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

    string positionsJsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<int, CameraSetup> cameras = new();
    readonly Dictionary<int, Polygon> worldAnchors = new();
    int currentFrameNumber = 0;
    int currentSpearNumber = 0;
    public static Main Instance;
    readonly List<StaticLink> cameraLinks = new();
    
    InteractionMode interactionMode = InteractionMode.PoseAlignment;

    Dancer lead;
    Dancer follow;

    Polygon leadGroundFoot;
    Polygon followGroundFoot;

    enum InteractionMode
    {
        PhotoAlignment = 0,
        PoseAlignment = 1
    }
    
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
        CameraSetup.GroundingFeatures worldAnchorVector2 =
            JsonConvert.DeserializeObject<CameraSetup.GroundingFeatures>(File.ReadAllText(Path.Combine( PhotoFolderPath,
                "worldAnchors.json")));

        Vector2 floorCenter = new Vector2(
            worldAnchorVector2.groundingCoordsX.First(),
            worldAnchorVector2.groundingCoordsY.First());
        
        
        Polygon origin = Instantiate(PolygonFactory.Instance.icosahedron0);
        origin.gameObject.SetActive(true);
        origin.transform.SetParent(transform, false);
        origin.transform.localScale = Vector3.one * .01f;
        origin.transform.localPosition = new Vector3(floorCenter.x, 0, floorCenter.y);
        origin.SetColor(Color.blue);
        origin.name = "Origin";
        
        worldAnchors.Add(0, origin);
        
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
                cameras.Add(int.Parse(dir.Name), cameraSetup);

                cameraSetup.transform.SetParent(transform, false);
                cameraSetup.transform.Translate(Vector3.back * count * .01f);
                cameraSetup.SetFrame(0);
                cameraSetups.Add(cameraSetup);
                count++;
            }

            GameObject cameraLinkContainer = new GameObject("CameraLinks");
            cameraLinkContainer.transform.SetParent(transform, false);
            foreach (CameraSetup cameraSetup in cameraSetups)
            {
                foreach (CameraSetup setup in cameraSetups)
                {
                    if (setup.name == cameraSetup.name) continue;

                    StaticLink cameraLink = Instantiate(StaticLink.prototypeStaticLink);
                    cameraLink.transform.SetParent(cameraLinkContainer.transform, false);
                    cameraLink.LinkFromTo(cameraSetup.transform, setup.transform);
                    cameraLink.gameObject.SetActive(true);
                    cameraLink.SetColor(Color.magenta);
                    cameraLinks.Add(cameraLink);
                }
            }
        }
        else
        {
            // TODO load from resources
        }

        string json;
        if (File.Exists(positionsJsonPath))
        {
            json = File.ReadAllText(positionsJsonPath);
        }
        else
        {
            TextAsset jsonPositions = Resources.Load<TextAsset>("positions");
            json = jsonPositions != null ? jsonPositions.text : "{}";
        }

        Dictionary<int, PositionAndRotation> fromJsonPics =
            JsonConvert.DeserializeObject<Dictionary<int, PositionAndRotation>>(json);

        foreach (KeyValuePair<int, PositionAndRotation> positionAndRotation in fromJsonPics)
        {
            CameraSetup cameraSetup = cameras[positionAndRotation.Key];
            cameraSetup.transform.localPosition =
                positionAndRotation.Value.positionVector3;
            cameraSetup.transform.localRotation =
                positionAndRotation.Value.rotationQuaternion;
            cameraSetup.MovePhotoToDistance(positionAndRotation.Value.focal);
            cameraSetup.DrawWorldSpears();
        }

        UpdateCameraLinks();

        lead = new GameObject("lead").AddComponent<Dancer>();
        follow = new GameObject("follow").AddComponent<Dancer>();
        lead.transform.SetParent(transform, false);
        follow.transform.SetParent(transform, false);

        Draw3DPose();
        
        Debug.Log(GlobalEntropy());
        
        SetInteractionMode(InteractionMode.PoseAlignment);
    }

    public void Advance()
    {
        currentFrameNumber++;
        if (currentFrameNumber > SqliteInput.FrameMax - 1)
        {
            currentFrameNumber = SqliteInput.FrameMax - 1;
        }

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(currentFrameNumber);
        }

        Draw3DPose();
    }

    public void Reverse()
    {
        currentFrameNumber--;
        if (currentFrameNumber < 0) currentFrameNumber = 0;
        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(currentFrameNumber);
        }

        Draw3DPose();
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
        if (interactionMode != InteractionMode.PhotoAlignment) return;
        
        currentSpearNumber++;
        if (currentSpearNumber > 17) currentSpearNumber = 17;

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.DrawSpear(currentSpearNumber);
        }
    }

    public void DrawPreviousSpear()
    {
        if (interactionMode != InteractionMode.PhotoAlignment) return;
        
        currentSpearNumber--;
        if (currentSpearNumber < 0) currentSpearNumber = 0;

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.DrawSpear(currentSpearNumber);
        }
    }

    void Draw3DPose()
    {
        // blind group rays based on arbitrary 50/50 could be lead or follow
        List<Ray>[] allRays = new List<Ray>[17];
        for (int i = 0; i < allRays.Length; i++)
        {
            allRays[i] = new List<Ray>();
        }

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            Tuple<Ray?, Ray?>[] camRays = cameraSetup.PoseRays();
            int jointCount = 0;
            foreach (Tuple<Ray?, Ray?> camRay in camRays)
            {
                if (camRay.Item1 != null)
                {
                    allRays[jointCount].Add(camRay.Item1.Value);
                }

                if (camRay.Item2 != null)
                {
                    allRays[jointCount].Add(camRay.Item2.Value);
                }

                jointCount++;
            }
        }

        int jointCount2 = 0;
        List<Vector3?> leadPose = new();
        List<Vector3?> followPose = new();
        foreach (List<Ray> jointRays in allRays)
        {
            // find the locus of each of these lists
            Vector3 jointLocus = RayMidpointFinder.FindMinimumMidpoint(jointRays);

            // re-sort on the basis if they are closer to one locus or another
            Tuple<List<Ray>, List<Ray>> sortedRays = SortRays(jointRays, jointLocus);

            // finally set target for re-sorted lists
            if (sortedRays.Item1.Count > 1)
            {
                Vector3 leadJointLocus = RayMidpointFinder.FindMinimumMidpoint(sortedRays.Item1);
                leadPose.Add(leadJointLocus - transform.position);
            }
            else
            {
                leadPose.Add(null);
            }
            
            if (sortedRays.Item2.Count > 1)
            {
                Vector3 followJointLocus = RayMidpointFinder.FindMinimumMidpoint(sortedRays.Item2);
                followPose.Add(followJointLocus - transform.position);
            }
            else
            {
                followPose.Add(null);
            }

            jointCount2++;
        }

        lead.Set3DPose(leadPose);
        follow.Set3DPose(followPose);
    }

    static Tuple<List<Ray>, List<Ray>> SortRays(List<Ray> rays, Vector3 target)
    {
        List<Ray> leftRays = new List<Ray>();
        List<Ray> rightRays = new List<Ray>();

        foreach (Ray ray in rays)
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

    void SetInteractionMode(InteractionMode mode)
    {
        interactionMode = mode;
        switch (interactionMode)
        {
            case InteractionMode.PhotoAlignment:
                foreach (CameraSetup cameraSetup in cameras.Values)
                {
                    cameraSetup.SetCollider(true);
                    cameraSetup.SetMarkers(false);
                }
                break;
            case InteractionMode.PoseAlignment:
                foreach (CameraSetup cameraSetup in cameras.Values)
                {
                    cameraSetup.SetCollider(false);
                    cameraSetup.SetMarkers(true);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Serializer serializer = new Serializer(positionsJsonPath);
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

    float GlobalEntropy()
    {
        return cameras.Values.Sum(cameraSetup => cameraSetup.Entropy(cameras, worldAnchors));
    }
}