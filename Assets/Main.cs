#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IO;
using Newtonsoft.Json;
using Pose;
using Shapes;
using Shapes.Lines;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using VRTKLite.SDK;

public class Main : MonoBehaviour
{
    [Header("Parameters")] public string PhotoFolderPath;

    Mover mover;
    public SDKManager SDKManager;
    public PoseAligner? PoseAligner;

    string positionsJsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<int, CameraSetup> cameras = new();
    readonly Dictionary<int, Polygon> worldAnchors = new();
    int currentFrameNumber = 0;

    public static Main Instance;
    readonly List<StaticLink> cameraLinks = new();

    public List<CameraSetup> GetCameras() => cameras.Values.ToList();

    InteractionMode interactionMode = InteractionMode.PhotoAlignment;

    public int GetCurrentFrameNumber() => currentFrameNumber;

    public enum InteractionMode
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
        SDKManager.LoadedVRSetupChanged += OnVrSetupChange;
    }

    void Start()
    {
        CameraSetup.GroundingFeatures worldAnchorVector2 =
            JsonConvert.DeserializeObject<CameraSetup.GroundingFeatures>(File.ReadAllText(Path.Combine(PhotoFolderPath,
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

            List<CameraSetup> cameraSetups = new();
            foreach (DirectoryInfo dir in root.EnumerateDirectories())
            {
                CameraSetup cameraSetup = new GameObject(dir.Name).AddComponent<CameraSetup>();
                cameraSetup.Init(dir.FullName, PoseAligner != null);
                cameras.Add(int.Parse(dir.Name), cameraSetup);

                cameraSetup.transform.SetParent(transform, false);
                cameraSetup.transform.Translate(Vector3.back * count * .01f);
                cameraSetup.SetFrame(0, true);
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

        Debug.Log(GlobalEntropy());

        SetInteractionMode(InteractionMode.PhotoAlignment);

        if (PoseAligner != null)
        {
            PoseAligner.Init(cameras);
        }

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(0, false);
        }

        if (PoseAligner != null)
        {
            //PoseAligner.Draw3DPoses(cameras.Values.ToList());
            PoseAligner.DrawAllTrails(132);
            PoseAligner.Draw3DPoseAtFrame(0);
        }
    }

    void OnVrSetupChange(GameObject setup)
    {
        if (setup.name == "GenericXR")
        {
            mover = setup.transform.GetChild(0).GetChild(1).GetComponent<Mover>();
        }
        else if (setup.name == "Simulator")
        {
            mover = setup.GetComponent<Mover>();
        }

        if (PoseAligner != null) PoseAligner.mover = mover;
    }

    public void Advance()
    {
        currentFrameNumber++;
        if (currentFrameNumber > SqliteInput.FrameMax - 1)
        {
            currentFrameNumber = SqliteInput.FrameMax - 1;
        }

        Debug.Log(currentFrameNumber);
        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(currentFrameNumber, false);
        }

        if (PoseAligner != null)
        {
            //PoseAligner.Draw3DPoses(cameras.Values.ToList());
            PoseAligner.Draw3DPoseAtFrame(currentFrameNumber);
        }
    }

    public void Reverse()
    {
        currentFrameNumber--;
        if (currentFrameNumber < 0) currentFrameNumber = 0;
        
        Debug.Log(currentFrameNumber);
        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            cameraSetup.SetFrame(currentFrameNumber, false);
        }

        if (PoseAligner != null)
        {
            //PoseAligner.Draw3DPoses(cameras.Values.ToList());
            PoseAligner.Draw3DPoseAtFrame(currentFrameNumber);
        }
    }

    public void UpdateCameraLinks()
    {
        foreach (StaticLink cameraLink in cameraLinks)
        {
            cameraLink.UpdateLink();
        }
    }

    void SetInteractionMode(InteractionMode mode)
    {
        interactionMode = mode;
        Debug.Log($"Interaction mode: {interactionMode}");
        switch (interactionMode)
        {
            case InteractionMode.PhotoAlignment:
                foreach (CameraSetup cameraSetup in cameras.Values)
                {
                    cameraSetup.SetCollider(true);
                    if (cameraSetup.PoseOverlay != null)
                    {
                        cameraSetup.PoseOverlay.SetMarkers(false);
                    }
                }

                break;
            case InteractionMode.PoseAlignment:
                foreach (CameraSetup cameraSetup in cameras.Values)
                {
                    cameraSetup.SetCollider(false);
                    if (cameraSetup.PoseOverlay != null)
                    {
                        cameraSetup.PoseOverlay.SetMarkers(true);
                    }
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

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mover.Grab();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            mover.Release();
        }

        if (PoseAligner != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                DrawNextSpear();
                MarkFigure1();
            }

            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                DrawPreviousSpear();
                MarkFigure2();
            }
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SetInteractionMode(InteractionMode.PhotoAlignment);
        }
        
        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            SetInteractionMode(InteractionMode.PoseAlignment);
        }
    }

    #region PoseAligner

    public void MarkFigure1()
    {
        if (interactionMode != InteractionMode.PoseAlignment) return;
        PoseAligner.MarkRole(0, mover, interactionMode, currentFrameNumber);
    }

    public void MarkFigure2()
    {
        if (interactionMode != InteractionMode.PoseAlignment) return;
        PoseAligner.MarkRole(1, mover, interactionMode, currentFrameNumber);
    }

    public void DrawNextSpear()
    {
        if (interactionMode != InteractionMode.PhotoAlignment) return;
        PoseAligner.DrawNextSpear(cameras);
    }

    public void DrawPreviousSpear()
    {
        if (interactionMode != InteractionMode.PhotoAlignment) return;
        PoseAligner.DrawPreviousSpear(cameras);
    }

    #endregion

    float GlobalEntropy()
    {
        return cameras.Values.Sum(cameraSetup => cameraSetup.Entropy(cameras, worldAnchors));
    }
}