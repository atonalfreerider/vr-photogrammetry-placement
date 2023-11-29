using System;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using Shapes.Lines;
using UnityEngine;

public enum Joints
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

public enum Role{
    Lead = 0,
    Follow = 1,
    Unknown = 2
}

public class Dancer : MonoBehaviour
{
    readonly List<Polygon> poseMarkers = new();
    readonly List<StaticLink> jointLinks = new();

    Role role;
    
    // only set when dancer is fully defined
    public List<List<Vector2>> posesByFrame = new(); 

    void Awake()
    {
        for (int j = 0; j < 17; j++)
        {
            Polygon sphere = Instantiate(PolygonFactory.Instance.icosahedron0);
            sphere.AddCollider(new Vector3(3, 1000, 3)); // compensate for flat photo container
            sphere.gameObject.SetActive(false);
            sphere.transform.localScale = Vector3.one * .005f;
            sphere.transform.SetParent(transform, false);
            sphere.name = ((Joints)j).ToString();
            sphere.MyDancer = this;
            poseMarkers.Add(sphere);
        }

        jointLinks.Add(LinkFromTo((int)Joints.Nose, (int)Joints.L_Eye, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.Nose, (int)Joints.R_Eye, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Eye, (int)Joints.R_Eye, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Eye, (int)Joints.L_Ear, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Ear, (int)Joints.L_Shoulder, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Eye, (int)Joints.R_Ear, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Ear, (int)Joints.R_Shoulder, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Hip, (int)Joints.R_Knee, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Knee, (int)Joints.R_Ankle, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Hip, (int)Joints.L_Knee, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Knee, (int)Joints.L_Ankle, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.R_Elbow, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Elbow, (int)Joints.R_Wrist, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Shoulder, (int)Joints.L_Elbow, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Elbow, (int)Joints.L_Wrist, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.L_Shoulder, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Hip, (int)Joints.L_Hip, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.R_Shoulder, (int)Joints.R_Hip, poseMarkers));
        jointLinks.Add(LinkFromTo((int)Joints.L_Shoulder, (int)Joints.L_Hip, poseMarkers));
    }

    StaticLink LinkFromTo(int index1, int index2, IReadOnlyList<Polygon> joints)
    {
        StaticLink staticLink = Instantiate(StaticLink.prototypeStaticLink);
        staticLink.gameObject.SetActive(true);
        staticLink.transform.SetParent(transform, false);
        staticLink.LinkFromTo(joints[index1].transform, joints[index2].transform);
        staticLink.SetColor(Viridis.ViridisColor(index1 / 17f));
        return staticLink;
    }
    
    public List<Vector2> Get2DPose()
    {
        return poseMarkers.Select(poseMarker => new Vector2(
            poseMarker.transform.localPosition.x * 640f,
            poseMarker.transform.localPosition.z * 360f)).ToList();
    }

    public void SetRole(Role setRole)
    {
        role = setRole;
        Color dancerColor = role switch
        {
            Role.Lead => Color.red,
            Role.Follow => Color.magenta,
            Role.Unknown => Color.grey,
            _ => throw new ArgumentOutOfRangeException()
        };

        foreach (Polygon poseMarker in poseMarkers)
        {
            poseMarker.SetColor(dancerColor);
        }
        
        foreach (StaticLink staticLink in jointLinks)
        {
            staticLink.SetColor(dancerColor);
        }
    }

    void UpdateLinks()
    {
        foreach (StaticLink staticLink in jointLinks)
        {
            staticLink.gameObject.SetActive(true);
            staticLink.UpdateLink();
        }
    }

    public void SetVisible(bool show)
    {
        foreach (Polygon poseMarker in poseMarkers)
        {
            poseMarker.gameObject.SetActive(show);
        }

        foreach (StaticLink link in jointLinks)
        {
            link.gameObject.SetActive(show);
        }
    }

    public void Set3DPose(List<Vector3?> pose)
    {
        for (int i = 0; i < poseMarkers.Count; i++)
        {
            if (pose[i].HasValue)
            {
                poseMarkers[i].transform.localPosition = pose[i].Value;
            }
        }

        UpdateLinks();
    }

    public void Set2DPose(List<Vector2> pose)
    {
        for (int j = 0; j < pose.Count; j++)
        {
            Polygon sphere = poseMarkers[j] ;
            sphere.gameObject.SetActive(true);

            sphere.transform.localPosition = new Vector3(pose[j].x / 640f, 0, pose[j].y / 360f);
        }
        
        UpdateLinks();
    }
    
    /// <summary>
    /// Only used when dancer is fully defined
    /// </summary>
    public void Set2DPose(int frameNumber) 
    { 
        List<Vector2> pose = posesByFrame[frameNumber]; 
        for (int j = 0; j < pose.Count; j++) 
        { 
            Polygon sphere = poseMarkers[j] ; 
            sphere.gameObject.SetActive(true); 
 
            sphere.transform.localPosition = new Vector3(pose[j].x / 640f, 0, pose[j].y / 360f); 
        } 
         
        UpdateLinks(); 
    } 
    
    public void SetPoseMarkerColliders(bool isOn)
    {
        foreach (Polygon poseMarker in poseMarkers)
        {
            poseMarker.GetComponent<BoxCollider>().enabled = isOn;
        }
    }
    
    public Polygon GetJoint(int jointNumber)
    {
        return poseMarkers[jointNumber];
    }
}