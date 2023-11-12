using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Shapes;
using Shapes.Lines;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    Rectangle photo;
    List<List<List<Vector2>>> dancersByFrame = new();
    string dirPath;

    Dancer lead;
    Dancer follow;

    float focal;
    public float GetFocal => focal;

    StaticLink leadSpear;
    StaticLink followSpear;

    void Awake()
    {
        Polygon focalSphere = Instantiate(PolygonFactory.Instance.icosahedron0);
        focalSphere.gameObject.SetActive(true);
        focalSphere.gameObject.AddComponent<SphereCollider>();
        focalSphere.transform.SetParent(transform, false);
        focalSphere.transform.localScale = Vector3.one * .01f;
        focalSphere.GetComponent<SphereCollider>().isTrigger = true;
        focalSphere.GetComponent<SphereCollider>().radius = 1f;

        leadSpear = Instantiate(StaticLink.prototypeStaticLink);
        leadSpear.gameObject.SetActive(false);
        leadSpear.transform.SetParent(transform, false);
        leadSpear.SetColor(Color.red);

        followSpear = Instantiate(StaticLink.prototypeStaticLink);
        followSpear.gameObject.SetActive(false);
        followSpear.transform.SetParent(transform, false);
        followSpear.SetColor(Color.red);
    }

    public void Init(string dirPath, [ItemCanBeNull] List<List<List<Vector2>>> posesPerFrame)
    {
        this.dirPath = dirPath;
        dancersByFrame = posesPerFrame;

        // first time
        photo = Instantiate(NewCube.textureRectPoly, transform, false);
        photo.gameObject.SetActive(true);
        photo.transform.SetParent(transform, false);
        photo.AddCollider();

        photo.transform.Rotate(Vector3.right, -90);

        lead = photo.gameObject.AddComponent<Dancer>();
        follow = photo.gameObject.AddComponent<Dancer>();
    }

    public void SetFrame(int frameNumber)
    {
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
        List<List<Vector2>> frame = dancersByFrame[frameNumber];

        int leadIndex = DancerBestIndex(frame, lead);

        List<List<Vector2>> secondFrameCheck = new();
        if (leadIndex != -1)
        {
            lead.SetVisible(true);
            lead.Set2DPose(frame[leadIndex]);
            lead.lastNosePosition = frame[leadIndex][(int)Joints.Nose];
            secondFrameCheck.AddRange(frame.Where((_, i) => i != leadIndex));
        }
        else
        {
            secondFrameCheck = frame;
            lead.SetVisible(false);
        }

        int followIndex = DancerBestIndex(secondFrameCheck, follow);

        if (followIndex != -1)
        {
            follow.SetVisible(true);
            follow.Set2DPose(secondFrameCheck[followIndex]);
            follow.lastNosePosition = secondFrameCheck[followIndex][(int)Joints.Nose];
        }
        else
        {
            follow.SetVisible(false);
        }
    }

    /// <summary>
    /// Attempt to sort based on nose distance continuity
    /// </summary>
    static int DancerBestIndex(List<List<Vector2>> frame, Dancer dancer)
    {
        Dictionary<int, Vector2> allNoses = new();
        int noseCount = 0;
        foreach (List<Vector2> pose2D in frame)
        {
            Vector2 nose = pose2D[(int)Joints.Nose];
            Vector2 lAnkle = pose2D[(int)Joints.L_Ankle];
            Vector2 rAnkle = pose2D[(int)Joints.R_Ankle];
            float height = nose.y -
                           Math.Min(lAnkle.y, rAnkle.y);
            if (height < 150)
            {
                noseCount++;
                continue; // filter out sitting
            }

            allNoses.Add(noseCount, nose);
            noseCount++;
        }

        int dancerIdx = -1;
        float minD = 1000;
        foreach ((int idx, Vector2 nose) in allNoses)
        {
            float distance = Vector2.Distance(nose, dancer.lastNosePosition);
            if (distance < minD)
            {
                if (dancer.lastNosePosition.magnitude <= float.Epsilon || // very first
                    distance < 50)   // don't let big frame jumps
                {
                    minD = distance;
                    dancerIdx = idx;
                }
            }
        }

        return dancerIdx;
    }


    public void DrawSpear(int jointNumber)
    {
        Polygon leadTarget = lead.GetJoint(jointNumber);
        leadSpear.LinkFromTo(transform, leadTarget.transform);
        leadSpear.UpdateLink();
        leadSpear.SetLength(10);
        leadSpear.gameObject.SetActive(leadTarget.gameObject.activeInHierarchy);

        Polygon followTarget = follow.GetJoint(jointNumber);
        followSpear.LinkFromTo(transform, followTarget.transform);
        followSpear.UpdateLink();
        followSpear.SetLength(10);
        followSpear.gameObject.SetActive(followTarget.gameObject.activeInHierarchy);
    }

    public Tuple<Ray?, Ray?>[] PoseRays()
    {
        Tuple<Ray?, Ray?>[] returnList = new Tuple<Ray?, Ray?>[17];

        for (int i = 0; i < returnList.Length; i++)
        {
            Ray? ray1 = null;
            Ray? ray2 = null;
            Polygon leadPoseMarker = lead.GetJoint(i);
            if (leadPoseMarker.gameObject.activeInHierarchy)
            {
                ray1 = new Ray(
                    transform.position,
                    Vector3.Normalize(leadPoseMarker.transform.position - transform.position));
            }

            Polygon followPoseMarker = follow.GetJoint(i);
            if (followPoseMarker.gameObject.activeInHierarchy)
            {
                ray2 = new Ray(
                    transform.position,
                    Vector3.Normalize(followPoseMarker.transform.position - transform.position));
            }

            returnList[i] = new Tuple<Ray?, Ray?>(ray1, ray2);
        }

        return returnList;
    }

    public void MovePhotoToDistance(float d)
    {
        focal = d;
        photo.transform.localPosition = new Vector3(0, 0, focal);
    }
}