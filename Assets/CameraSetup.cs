using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Shapes;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    Rectangle photo;
    List<List<List<Vector2>>> dancersByFrame = new();
    string dirPath;
    readonly List<GameObject> currentPoseMarkers = new();
    public float Focal = 5;

    void Awake()
    {
        GameObject focalSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        focalSphere.transform.SetParent(transform, true);
        focalSphere.transform.localScale = Vector3.one * .1f;
        focalSphere.GetComponent<SphereCollider>().isTrigger = true;
        focalSphere.GetComponent<SphereCollider>().radius = 3f;
    }

    public void Init(string dirPath, [ItemCanBeNull] List<List<List<Vector2>>> posesPerFrame)
    {
        this.dirPath = dirPath;
        dancersByFrame = posesPerFrame;
    }
    
    public void SetFrame(int frameNumber)
    {
        if (photo == null)
        {
            photo = Instantiate(NewCube.textureRectPoly, transform, false);
            photo.gameObject.SetActive(true);
            photo.transform.SetParent(transform, false);
            
            photo.transform.Rotate(Vector3.right, -90);
            photo.transform.Translate(Vector3.down * Focal * .1f);
        }

        photo.gameObject.name = frameNumber.ToString();

        Main.ImgMetadata imgMeta = null;
        string imageName = Path.Combine(dirPath, $"{frameNumber:000}.jpg");
        if (File.Exists(imageName))
        {
            Texture2D texture =
                new(100, 100, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point };
            texture.LoadImage(File.ReadAllBytes(imageName));
            imgMeta = new Main.ImgMetadata(5, texture.width, texture.height);
        }
        
        photo.LoadTexture(File.ReadAllBytes(imageName));
        photo.transform.localScale = new Vector3(imgMeta.Width, 1, imgMeta.Height) * .001f;
        
        LoadPose(frameNumber);
    }

    void LoadPose(int frameNumber)
    {
        foreach (GameObject currentPoseMarker in currentPoseMarkers)
        {
            Destroy(currentPoseMarker);
        }
        
        currentPoseMarkers.Clear();
        
        List<List<Vector2>> frame = dancersByFrame[frameNumber];
        // initiate dance skeletons
        foreach (List<Vector2> dancer in frame)
        {
            for (int j = 0; j < dancer.Count; j++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.gameObject.SetActive(true);
                sphere.transform.SetParent(photo.transform, false);
                
                sphere.transform.localPosition = new Vector3(dancer[j].x / 640f, 0,  dancer[j].y / 360f);
                currentPoseMarkers.Add(sphere);
                
                sphere.transform.LookAt(transform);
                
                sphere.transform.SetParent(transform, true);
                
                sphere.transform.localScale = new Vector3(.005f, .005f, .5f);
            }
        }
    }
}