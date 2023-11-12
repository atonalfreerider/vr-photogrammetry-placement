using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Shapes.Lines;
using UnityEngine;
using UnityEngine.InputSystem;

public class Main : MonoBehaviour
{
    [Header("Parameters")]
    public string PhotoFolderPath;

    string jsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<string, CameraSetup> cameras = new();
    int currentFrameNumber = 0;
    public static Main Instance;
    readonly List<StaticLink> cameraLinks = new();

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
                    if(setup.name == cameraSetup.name) continue;
                    
                    StaticLink link = Instantiate(StaticLink.prototypeStaticLink);
                    link.transform.SetParent(transform, false);
                    link.LinkFromTo(cameraSetup.transform, setup.transform);
                    link.gameObject.SetActive(true);
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
            cameraSetup.Focal = positionAndRotation.Value.focal;
        }

        UpdateCameraLinks();
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
    }
}