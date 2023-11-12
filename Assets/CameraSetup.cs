using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Shapes;
using Shapes.Lines;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    Rectangle photo;
    List<List<List<Vector2>>> dancersByFrame = new();
    string dirPath;
    readonly List<Polygon> leadPoseMarkers = new();
    readonly List<StaticLink> leadLinks = new();

    readonly List<Polygon> followPoseMarkers = new();
    readonly List<StaticLink> followLinks = new();

    public float Focal = 5;

    StaticLink leadSpear;
    StaticLink followSpear;

    enum Joints
    {
        Nose = 0,
        L_Eye = 1,
        R_Eye = 2,
        L_Ear = 3,
        R_Ear = 4,
        L_Shoulder = 5,
        R_Shoulder = 6,
        L_Elbow = 7,
        R_Elbow = 8,
        L_Wrist = 9,
        R_Wrist = 10,
        L_Hip = 11,
        R_Hip = 12,
        L_Knee = 13,
        R_Knee = 14,
        L_Ankle = 15,
        R_Ankle = 16
    }

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

        photo.transform.Rotate(Vector3.right, -90);
        photo.transform.Translate(Vector3.down * Focal * .1f);

        InstantiateTwoDancers();
    }

       void InstantiateTwoDancers()
    {
        for (int j = 0; j < 17; j++)
        {
            Polygon sphere = Instantiate(PolygonFactory.Instance.icosahedron0);
            sphere.gameObject.SetActive(false);
            sphere.transform.localScale = Vector3.one * .02f;
            sphere.transform.SetParent(photo.transform, false);
            leadPoseMarkers.Add(sphere);
        }

        leadLinks.Add(LinkFromTo((int)Joints.Nose, (int)Joints.L_Eye, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.Nose, (int)Joints.R_Eye, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Eye, (int)Joints.R_Eye, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Eye, (int)Joints.L_Ear, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Ear, (int)Joints.L_Shoulder, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Eye, (int)Joints.R_Ear, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Ear, (int)Joints.R_Shoulder, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Hip, (int)Joints.R_Knee, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Knee, (int)Joints.R_Ankle, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Hip, (int)Joints.L_Knee, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Knee, (int)Joints.L_Ankle, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.R_Elbow, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Elbow, (int)Joints.R_Wrist, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Shoulder, (int)Joints.L_Elbow, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Elbow, (int)Joints.L_Wrist, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.L_Shoulder, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Hip, (int)Joints.L_Hip, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.R_Hip, leadPoseMarkers));
        leadLinks.Add(LinkFromTo((int)Joints.L_Shoulder, (int)Joints.L_Hip, leadPoseMarkers));

        for (int i = 0; i < 17; i++)
        {
            Polygon sphere = Instantiate(PolygonFactory.Instance.icosahedron0);
            sphere.gameObject.SetActive(false);
            sphere.transform.localScale = Vector3.one * .001f;
            sphere.transform.SetParent(photo.transform, false);
            followPoseMarkers.Add(sphere);
        }

        followLinks.Add(LinkFromTo((int)Joints.Nose, (int)Joints.L_Eye, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.Nose, (int)Joints.R_Eye, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Eye, (int)Joints.R_Eye, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Eye, (int)Joints.L_Ear, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Ear, (int)Joints.L_Shoulder, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Eye, (int)Joints.R_Ear, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Ear, (int)Joints.R_Shoulder, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Hip, (int)Joints.R_Knee, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Knee, (int)Joints.R_Ankle, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Hip, (int)Joints.L_Knee, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Knee, (int)Joints.L_Ankle, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.R_Elbow, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Elbow, (int)Joints.R_Wrist, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Shoulder, (int)Joints.L_Elbow, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Elbow, (int)Joints.L_Wrist, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.L_Shoulder, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Hip, (int)Joints.L_Hip, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.R_Hip, followPoseMarkers));
        followLinks.Add(LinkFromTo((int)Joints.L_Shoulder, (int)Joints.L_Hip, followPoseMarkers));
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

        bool anyActive = false;
        bool isLead = true;
        foreach (List<Vector2> dancer in frame)
        {
            float height = dancer[(int)Joints.Nose].y -
                           Math.Min(dancer[(int)Joints.L_Ankle].y, dancer[(int)Joints.R_Ankle].y);
            if (height < 100) continue;

            anyActive = true;

            for (int j = 0; j < dancer.Count; j++)
            {
                Polygon sphere = isLead ? leadPoseMarkers[j] : followPoseMarkers[j];
                sphere.gameObject.SetActive(true);

                sphere.transform.localPosition = new Vector3(dancer[j].x / 640f, 0, dancer[j].y / 360f);
            }

            isLead = false;
        }

        foreach (StaticLink staticLink in leadLinks)
        {
            staticLink.gameObject.SetActive(true);
            staticLink.UpdateLink();
        }

        foreach (StaticLink staticLink in followLinks)
        {
            staticLink.gameObject.SetActive(true);
            staticLink.UpdateLink();
        }

        if (isLead)
        {
            foreach (Polygon followPoseMarker in followPoseMarkers)
            {
                followPoseMarker.gameObject.SetActive(false);
            }

            foreach (StaticLink followLink in followLinks)
            {
                followLink.gameObject.SetActive(false);
            }
        }

        if (!anyActive)
        {
            foreach (Polygon followPoseMarker in leadPoseMarkers)
            {
                followPoseMarker.gameObject.SetActive(false);
            }

            foreach (StaticLink followLink in leadLinks)
            {
                followLink.gameObject.SetActive(false);
            }
        }
    }

    StaticLink LinkFromTo(int index1, int index2, IReadOnlyList<Polygon> joints)
    {
        StaticLink staticLink = Instantiate(StaticLink.prototypeStaticLink);
        staticLink.gameObject.SetActive(true);
        staticLink.transform.SetParent(transform, false);
        staticLink.LinkFromTo(joints[index1].transform, joints[index2].transform);
        return staticLink;
    }

    public void DrawSpear(int jointNumber)
    {
        leadSpear.LinkFromTo(transform, leadPoseMarkers[jointNumber].transform);
        leadSpear.UpdateLink();        
        leadSpear.SetLength(10);
        leadSpear.gameObject.SetActive(leadPoseMarkers[jointNumber].gameObject.activeInHierarchy);

        followSpear.LinkFromTo(transform, followPoseMarkers[jointNumber].transform);
        followSpear.UpdateLink();
        followSpear.SetLength(10);
        followSpear.gameObject.SetActive(followPoseMarkers[jointNumber].gameObject.activeInHierarchy);
    }
}