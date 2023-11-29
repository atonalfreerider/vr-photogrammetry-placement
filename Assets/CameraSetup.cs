﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Shapes;
using Shapes.Lines;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    Rectangle photo;
    List<List<List<Vector2>>> dancersByFrame = new();
    string dirPath;

    readonly List<Dancer> dancers = new();
    
    Dancer lead;
    Dancer follow;

    float focal;
    public float GetFocal => focal;

    StaticLink leadSpear;
    StaticLink followSpear;
    readonly Dictionary<int, Polygon> cameraMarkers = new();
    readonly Dictionary<int, Polygon> worldAnchorMarkers = new();

    bool poseMarkerCollidersOn = false;

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

    public void Init(string dirPath, List<List<List<Vector2>>> posesPerFrame)
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
        lead.SetRole(Role.Lead);
        follow = photo.gameObject.AddComponent<Dancer>();
        follow.SetRole(Role.Follow);

        if (File.Exists(Path.Combine(this.dirPath, "grounding.json")))
        {
            GroundingFeatures groundingFeatures =
                JsonConvert.DeserializeObject<GroundingFeatures>(File.ReadAllText(Path.Combine(this.dirPath, "grounding.json")));

            for (int i = 0; i < groundingFeatures.groundingCoordsX.Count; i++)
            {
                Vector2 coord = new Vector2(groundingFeatures.groundingCoordsX[i], groundingFeatures.groundingCoordsY[i]);
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
        foreach (Dancer dancer in dancers)
        {
            dancer.SetVisible(false);
        }
        
        List<List<Vector2>> frame = dancersByFrame[frameNumber];
        for (int i = 0; i < frame.Count; i++)
        {
            if (dancers.Count <= i)
            {
                Dancer dancer = photo.gameObject.AddComponent<Dancer>();
                dancer.SetRole(Role.Unknown);
                dancers.Add(dancer);
            }

            Dancer dancerAtI = dancers[i];
            dancerAtI.SetVisible(true);
            dancerAtI.Set2DPose(frame[i]);
            dancerAtI.SetPoseMarkerColliders(poseMarkerCollidersOn);
        }
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

    public void DrawWorldSpears()
    {
        foreach (KeyValuePair<int,Polygon> keyValuePair in worldAnchorMarkers)
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
    }

    public void SetMarkers(bool isOn)
    {
        poseMarkerCollidersOn = isOn;
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
    
    [Serializable]
    public class GroundingFeatures
    {
        public List<float> groundingCoordsX;
        public List<float> groundingCoordsY;
        public List<int> indices;
        public List<bool> isCamera;
        
        public GroundingFeatures(List<float> groundingCoordsX, List<float> groundingCoordsY, List<int> indices, List<bool> isCamera)
        {
            this.groundingCoordsX = groundingCoordsX;
            this.groundingCoordsY = groundingCoordsY;
            this.indices = indices;
            this.isCamera = isCamera;
        }
    }
}