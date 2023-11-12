using System.Collections.Generic;
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

public class Dancer : MonoBehaviour
{
    readonly List<Polygon> poseMarkers = new();
    readonly List<StaticLink> jointLinks = new();
    public Vector2 lastNosePosition = Vector3.zero;

    void Awake()
    {
        for (int j = 0; j < 17; j++)
        {
            Polygon sphere = Instantiate(PolygonFactory.Instance.icosahedron0);
            sphere.gameObject.SetActive(false);
            sphere.transform.localScale = Vector3.one * .005f;
            sphere.transform.SetParent(transform, false);
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
        foreach (Polygon followPoseMarker in poseMarkers)
        {
            followPoseMarker.gameObject.SetActive(show);
        }

        foreach (StaticLink followLink in jointLinks)
        {
            followLink.gameObject.SetActive(show);
        }
    }

    public void Set3DPose(List<Vector3> pose)
    {
        for(int i = 0 ; i< poseMarkers.Count; i++)
        {
            poseMarkers[i].transform.localPosition = pose[i];
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
    
    public Polygon GetJoint(int jointNumber)
    {
        return poseMarkers[jointNumber];
    }
}