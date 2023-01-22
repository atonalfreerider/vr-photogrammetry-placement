using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mediapipe.BlazePose;
using UnityEngine;
using UnityEngine.Video;

public class PoseFromVideo : MonoBehaviour
{
    BlazePoseDetecter poseDetector;
    public string videoFolder;

    readonly Dictionary<string, RenderTexture> videoFrameByCam = new();
    readonly Dictionary<string, VideoPlayer> playerByCam = new();

    void Start()
    {
        poseDetector = new BlazePoseDetecter();

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

        Single[] buff = new System.Single[poseDetector.outputBuffer.stride];
        poseDetector.outputBuffer.GetData(buff);

        foreach (float VARIABLE in buff)
        {
            Debug.Log(VARIABLE);
        }
    }
}