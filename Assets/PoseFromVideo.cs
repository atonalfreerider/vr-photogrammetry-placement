using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mediapipe.BlazePose;
using UnityEngine;
using UnityEngine.Video;
using VRTKLite.SDK;

public class PoseFromVideo : MonoBehaviour
{
    BlazePoseDetecter poseDetector;
    public string videoFolder;

    readonly Dictionary<string, RenderTexture> videoFrameByCam = new();
    readonly Dictionary<string, VideoPlayer> playerByCam = new();

    [SerializeField, Range(0, 1)] float humanExistThreshold = 0.5f;

    public Shader visualizerShader;

    Camera mainCamera;

    public SDKManager sdkManager;
    Material material;

    // Lines count of body's topology.
    const int BODY_LINE_NUM = 35;

    // Pairs of vertex indices of the lines that make up body's topology.
    // Defined by the figure in https://google.github.io/mediapipe/solutions/pose.
    readonly List<Vector4> linePair = new()
    {
        new Vector4(0, 1), new Vector4(1, 2), new Vector4(2, 3), new Vector4(3, 7), new Vector4(0, 4),
        new Vector4(4, 5), new Vector4(5, 6), new Vector4(6, 8), new Vector4(9, 10), new Vector4(11, 12),
        new Vector4(11, 13), new Vector4(13, 15), new Vector4(15, 17), new Vector4(17, 19), new Vector4(19, 15),
        new Vector4(15, 21), new Vector4(12, 14), new Vector4(14, 16), new Vector4(16, 18), new Vector4(18, 20),
        new Vector4(20, 16), new Vector4(16, 22), new Vector4(11, 23), new Vector4(12, 24), new Vector4(23, 24),
        new Vector4(23, 25), new Vector4(25, 27), new Vector4(27, 29), new Vector4(29, 31), new Vector4(31, 27),
        new Vector4(24, 26), new Vector4(26, 28), new Vector4(28, 30), new Vector4(30, 32), new Vector4(32, 28)
    };

    void Awake()
    {
        sdkManager.LoadedVRSetupChanged += OnVRSetupChanged;
        poseDetector = new BlazePoseDetecter();
        material = new Material(visualizerShader);
    }

    void Start()
    {
        DirectoryInfo root = new DirectoryInfo(videoFolder);

        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.mp4"))
        {
            GameObject photo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            photo.transform.Rotate(Vector3.right * 90);
            photo.transform.Rotate(Vector3.forward * 180);
            photo.transform.Translate(Vector3.down * 20);

            photo.transform.Translate(Vector3.forward * count * 12);
            count++;

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
        }
    }

    void OnVRSetupChanged(GameObject newSetup)
    {
        switch (newSetup.name)
        {
            case "GenericXR":
            {
                mainCamera = newSetup
                    .transform
                    .Find("head")
                    .GetComponent<Camera>();
                break;
            }
            case "Simulator":
            {
                mainCamera = newSetup
                    .transform
                    .Find("Camera")
                    .GetComponent<Camera>();
                break;
            }
        }

        StartCoroutine(Step());
    }

    IEnumerator Step()
    {
        foreach (KeyValuePair<string, VideoPlayer> kvp in playerByCam)
        {
            kvp.Value.StepForward();

            ExtractFrame(kvp.Key);
        }

        yield return null;
        StartCoroutine(Step());
    }


    void ExtractFrame(string filePath)
    {
        // extract the frame number from each video and write each frame to the output folder

        RenderTexture renderTexture = videoFrameByCam[filePath];

        poseDetector.ProcessImage(renderTexture);

    }

    void OnRenderObject()
    {
        // Use predicted pose world landmark results on the ComputeBuffer (GPU) memory.
        material.SetBuffer("_worldVertices", poseDetector.worldLandmarkBuffer);
        // Set pose landmark counts.
        material.SetInt("_keypointCount", poseDetector.vertexCount);
        material.SetFloat("_humanExistThreshold", humanExistThreshold);
        material.SetVectorArray("_linePair", linePair);
        material.SetMatrix("_invViewMatrix", mainCamera.worldToCameraMatrix.inverse);

        // Draw 35 world body topology lines.
        material.SetPass(2);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, BODY_LINE_NUM);

        // Draw 33 world landmark points.
        material.SetPass(3);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, poseDetector.vertexCount);
    }
}