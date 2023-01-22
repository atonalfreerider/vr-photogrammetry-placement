using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mediapipe.BlazePose;
using PclSharp;
using PclSharp.Std;
using PclSharp.Struct;
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

    Coroutine stepper;

    void Start()
    {
        DirectoryInfo root = new DirectoryInfo(videoFolder);
        int numfiles = root.EnumerateFiles("*.mp4").Count();

        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.mp4"))
        {
            BlazePoseDetecter detector = new BlazePoseDetecter();
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

        stepper = StartCoroutine(Step());
    }

    IEnumerator Step()
    {
        int count = 0;
        string baseFilePath = playerByCam.First().Key;
        foreach (KeyValuePair<string, VideoPlayer> kvp in playerByCam)
        {
            kvp.Value.StepForward();

            ExtractFrame(kvp.Key);
            if (count > 0)
            {
                Matrix4x4 alignmentMatrix = Align(baseFilePath, kvp.Key);
                //alignmentMatrix = NerfSerializer.ChangeHand(alignmentMatrix);
                systemContainerByCam[kvp.Key].transform.FromMatrix(alignmentMatrix);
            }

            count++;
        }

        //yield return new WaitForSeconds(.2f);
        yield return null;
        stepper = StartCoroutine(Step());
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

    Matrix4x4 Align(string baseFile, string alignFile)
    {
        PclSharp.Registration.IterativeClosestPointOfPointXYZ_PointXYZ icp = new();

        icp.MaximumIterations = 1000;
        icp.MaxCorrespondenceDistance = 0.5d;
        icp.TransformationEpsilon = 1e-8d;
        //icp.TransformationRotationEpsilon = 1e-8d;
        icp.EuclideanFitnessEpsilon = 1e-8d;
        icp.RANSACOutlierRejectionThreshold = 0.25d;

        PointCloudOfXYZ basePointCloud = new();
        VectorOfInt baseIndices = new();

        // do each
        foreach (KeyValuePair<int, GameObject> pointAndIndex in posePointsByCam[baseFile])
        {
            baseIndices.Add(pointAndIndex.Key);
            Vector3 pos = pointAndIndex.Value.transform.localPosition;
            PointXYZ point = new PointXYZ
            {
                X = pos.x,
                Y = pos.y,
                Z = pos.z
            };
            basePointCloud.Add(point);
        }

        icp.SetInputCloud(basePointCloud);
        icp.SetIndices(baseIndices);

        PointCloudOfXYZ toAlign = new();

        // do each
        foreach (KeyValuePair<int, GameObject> pointAndIndex in posePointsByCam[alignFile])
        {
            Vector3 pos = pointAndIndex.Value.transform.localPosition;
            PointXYZ point = new PointXYZ
            {
                X = pos.x,
                Y = pos.y,
                Z = pos.z
            };
            toAlign.Add(point);
        }

        icp.InputTarget = toAlign;

        PointCloudOfXYZ aligned = new();
        icp.Align(aligned);

        PclSharp.Eigen.Matrix4f trans = icp.FinalTransformation;

        Matrix4x4 matrix = new Matrix4x4
        {
            m00 = trans[0, 0],
            m01 = trans[0, 1],
            m02 = trans[0, 2],
            m03 = trans[0, 3],
            m10 = trans[1, 0],
            m11 = trans[1, 1],
            m12 = trans[1, 2],
            m13 = trans[1, 3],
            m20 = trans[2, 0],
            m21 = trans[2, 1],
            m22 = trans[2, 2],
            m23 = trans[2, 3],
            m30 = trans[3, 0],
            m31 = trans[3, 1],
            m32 = trans[3, 2],
            m33 = trans[3, 3]
        };

        icp.Dispose();
        basePointCloud.Dispose();
        baseIndices.Dispose();
        toAlign.Dispose();
        aligned.Dispose();
        trans.Dispose();

        return matrix;
    }

    void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, BlazePoseDetecter> kvp in detectorByCam)
        {
            kvp.Value.Dispose();
        }
    }
}

public static class TransformExtensions
{
    public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
    {
        transform.localScale = matrix.ExtractScale();
        transform.rotation = matrix.ExtractRotation();
        transform.position = matrix.ExtractPosition();
    }
}

public static class MatrixExtensions
{
    public static Quaternion ExtractRotation(this Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractPosition(this Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public static Vector3 ExtractScale(this Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
}