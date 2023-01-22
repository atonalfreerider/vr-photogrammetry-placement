using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mediapipe.BlazePose;
using UnityEngine;
using UnityEngine.Video;

public class PoseFromVideo : MonoBehaviour
{
    public string videoFolder;

    readonly Dictionary<string, BlazePoseDetecter> detectorByCam = new();
    readonly Dictionary<string, RenderTexture> videoFrameByCam = new();
    readonly Dictionary<string, VideoPlayer> playerByCam = new();
    readonly Dictionary<string, GameObject> systemContainerByCam = new();

    // Lines count of body's topology.
    const int BODY_LINE_NUM = 35;

    readonly Dictionary<string, Dictionary<int, GameObject>> posePointsByCam = new();
    readonly Dictionary<string, GameObject> bodyContainersByCam = new();

    void Start()
    {
        DirectoryInfo root = new DirectoryInfo(videoFolder);
        int numfiles = root.EnumerateFiles("*.mp4").Count();

        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.mp4"))
        {
            BlazePoseDetecter detector =  new BlazePoseDetecter();
            detectorByCam.Add(file.FullName, detector);
            
            GameObject systemContainer = new(file.FullName);
            systemContainerByCam.Add(file.FullName, systemContainer);

            GameObject photo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            photo.transform.SetParent(systemContainer.transform);
            photo.transform.Rotate(Vector3.right * 90);
            photo.transform.Rotate(Vector3.forward * 180);
            photo.transform.Translate(Vector3.down * 12f);

            photo.transform.localScale = new Vector3(1.92f, 1, 1.08f);

            VideoPlayer videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.url = file.FullName;
            playerByCam.Add(file.FullName, videoPlayer);

            videoPlayer.Prepare();

            RenderTexture renderTexture = new RenderTexture(new RenderTextureDescriptor(1920, 1080,
                RenderTextureFormat.Default));
            videoFrameByCam.Add(file.FullName, renderTexture);

            renderTexture.Create();


            videoPlayer.targetTexture = renderTexture;

            photo.GetComponent<Renderer>().material.SetTexture("_MainTex", renderTexture);

            videoPlayer.playOnAwake = false;
            videoPlayer.Pause();

            GameObject bodyContainer = new(file.FullName);
            bodyContainersByCam.Add(file.FullName, bodyContainer);
            bodyContainer.transform.SetParent(systemContainer.transform);
            Dictionary<int, GameObject> posePoints = new();
            posePointsByCam.Add(file.FullName, posePoints);
            for (int i = 0; i < BODY_LINE_NUM; i++)
            {
                GameObject focalSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                focalSphere.transform.localScale = Vector3.one * .1f;
                focalSphere.GetComponent<Renderer>().material.color = Viridis.ViridisColor((float)count / numfiles);

                Destroy(focalSphere.GetComponent<SphereCollider>());

                posePoints.Add(i, focalSphere);
                focalSphere.transform.SetParent(bodyContainer.transform);
            }

            count++;
        }

        StartCoroutine(Step());
    }

    IEnumerator Step()
    {
        foreach (KeyValuePair<string, VideoPlayer> kvp in playerByCam)
        {
            kvp.Value.StepForward();

            ExtractFrame(kvp.Key);

            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(.2f);
        StartCoroutine(Step());
    }


    void ExtractFrame(string filePath)
    {
        // extract the frame number from each video and write each frame to the output folder

        RenderTexture renderTexture = videoFrameByCam[filePath];
        BlazePoseDetecter poseDetector = detectorByCam[filePath];

        poseDetector.ProcessImage(renderTexture);

        Dictionary<int, GameObject> posePoints = posePointsByCam[filePath];
        for (int i = 0; i < poseDetector.vertexCount; i++)
        {
            Vector4 posLandmark = poseDetector.GetPoseWorldLandmark(i);
            posePoints[i].transform.localPosition = posLandmark;
        }
    }

    void Align()
    {
        
    }

    void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, BlazePoseDetecter> kvp in detectorByCam)
        {
            kvp.Value.Dispose();
        }
    }
}