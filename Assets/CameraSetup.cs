#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Shapes;
using Shapes.Lines;
using UI;
using UnityEngine;
using UnityEngine.Video;

public class CameraSetup : MonoBehaviour
{
    public PoseOverlay? PoseOverlay;
    GameObject photo;
    Polygon focalSphere;
    string dirPath;

    const float PixelToMeterRatio = .001f;
    const int TextureScale = 1;
    public Main.ImgMetadata? imgMeta;

    float focal;
    public float GetFocal => focal;

    readonly Dictionary<int, Polygon> cameraMarkers = new();
    readonly Dictionary<int, Polygon> worldAnchorMarkers = new();

    Plane currentPlane => new(photo.transform.up, photo.transform.position);
    public GameObject GetPhotoGameObject => photo.gameObject;
    VideoPlayer videoPlayer;

    void Awake()
    {
        focalSphere = Instantiate(PolygonFactory.Instance.icosahedron0);
        focalSphere.gameObject.SetActive(true);
        focalSphere.gameObject.AddComponent<SphereCollider>();
        focalSphere.transform.SetParent(transform, false);
        focalSphere.transform.localScale = Vector3.one * .01f;
        focalSphere.GetComponent<SphereCollider>().isTrigger = true;
        focalSphere.GetComponent<SphereCollider>().radius = 1f;
    }

    public void Init(string initDirPath, bool addPoseOverlay)
    {
        // first time
        photo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        photo.AddComponent<BoxCollider>();
        photo.name = "PHOTO: " + dirPath;
        photo.SetActive(true);
        photo.transform.SetParent(transform, false);
        photo.transform.Translate(Vector3.forward * 1);
        
        videoPlayer = photo.AddComponent<VideoPlayer>();
        videoPlayer.targetCameraAlpha = .5f;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
        videoPlayer.playOnAwake = true;
        videoPlayer.prepareCompleted += source =>
        {
            photo.transform.localScale = new Vector3(
                source.width * PixelToMeterRatio * TextureScale,
                source.height * PixelToMeterRatio * TextureScale,
                1);
            photo.GetComponent<Renderer>().material.mainTexture = videoPlayer.texture;
        };
        dirPath = initDirPath;
        videoPlayer.url = Directory.EnumerateFiles(initDirPath, "*.mp4").First();

        focalSphere.name = "SPHERE: " + dirPath;

        string jsonPath = Path.Combine(dirPath, "grounding.json");
        if (File.Exists(jsonPath))
        {
            LoadGroundingFeatures(jsonPath);
        }

        if (addPoseOverlay)
        {
            PoseOverlay = gameObject.AddComponent<PoseOverlay>();
            PoseOverlay.InitFigures(dirPath);
        }
    }

    public void SetFrame(int frameNumber, bool isFirst)
    {
        videoPlayer.frame = frameNumber;
        
        if (!isFirst && PoseOverlay != null)
        {
            PoseOverlay.LoadPose(frameNumber, photo.gameObject);
        }
    }

    public void MovePhotoToDistance(float d)
    {
        focal = d;
        photo.transform.localPosition = new Vector3(0, 0, focal);
    }

    public void DrawWorldSpears()
    {
        foreach (KeyValuePair<int, Polygon> keyValuePair in worldAnchorMarkers)
        {
            Polygon worldAnchor = keyValuePair.Value;

            StaticLink staticLink = Instantiate(StaticLink.prototypeStaticLink);
            staticLink.gameObject.SetActive(true);
            staticLink.transform.SetParent(transform, false);
            staticLink.LinkFromTo(transform, worldAnchor.transform);
            staticLink.UpdateLink();
            staticLink.SetLength(10f);
            staticLink.SetColor(Color.blue);
        }
    }

    public void SetCollider(bool isOn)
    {
        photo.GetComponent<BoxCollider>().enabled = isOn;
        focalSphere.GetComponent<SphereCollider>().enabled = isOn;
    }

    public float Entropy(Dictionary<int, CameraSetup> otherCameras, Dictionary<int, Polygon> worldAnchorPositions)
    {
        float entropy = 0;
        foreach ((int idx, Polygon cameraMarker) in cameraMarkers)
        {
            CameraSetup otherSetup = otherCameras[idx];
            Vector3 measure = otherSetup.transform.position - transform.position;
            Vector3 anch = cameraMarker.transform.position - transform.position;
            entropy += Vector3.Angle(anch, measure);
        }

        foreach ((int idx, Polygon worldMarker) in worldAnchorMarkers)
        {
            Vector3 vectorToAnchor = worldAnchorPositions[idx].transform.position - transform.position;
            Vector3 vectorToMarker = worldMarker.transform.position - transform.position;
            entropy += Vector3.Angle(vectorToAnchor, vectorToMarker);
        }

        return entropy;
    }

    void LoadGroundingFeatures(string jsonPath)
    {
        GroundingFeatures groundingFeatures = JsonConvert.DeserializeObject<GroundingFeatures>(
            File.ReadAllText(jsonPath));

        for (int i = 0; i < groundingFeatures.groundingCoordsX.Count; i++)
        {
            Vector2 coord = new Vector2(
                groundingFeatures.groundingCoordsX[i],
                groundingFeatures.groundingCoordsY[i]);

            int index = groundingFeatures.indices[i];
            bool isCamera = groundingFeatures.isCamera[i];

            Polygon sphere = Instantiate(PolygonFactory.Instance.icosahedron0);
            sphere.gameObject.SetActive(true);
            sphere.transform.SetParent(photo.transform, false);
            sphere.transform.localScale = Vector3.one * .01f;
            sphere.transform.localPosition = new Vector3(coord.x, 0, coord.y);

            if (isCamera)
            {
                cameraMarkers.Add(index, sphere);
                sphere.SetColor(Color.yellow);
            }
            else
            {
                worldAnchorMarkers.Add(index, sphere);
                sphere.SetColor(Color.blue);
            }
        }
    }

    public Vector3? Intersection(Ray ray)
    {
        Vector3? rayPlaneIntersection = Raycast.PlaneIntersection(
            currentPlane,
            ray);
        if (rayPlaneIntersection.HasValue)
        {
            Vector3 intersection = photo.transform.InverseTransformPoint(rayPlaneIntersection.Value);
            return intersection;
        }

        return null;
    }

    [Serializable]
    public class GroundingFeatures
    {
        public List<float> groundingCoordsX;
        public List<float> groundingCoordsY;
        public List<int> indices;
        public List<bool> isCamera;

        public GroundingFeatures(List<float> groundingCoordsX, List<float> groundingCoordsY, List<int> indices,
            List<bool> isCamera)
        {
            this.groundingCoordsX = groundingCoordsX;
            this.groundingCoordsY = groundingCoordsY;
            this.indices = indices;
            this.isCamera = isCamera;
        }
    }
}