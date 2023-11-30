#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using Shapes.Lines;
using TMPro;
using UnityEngine;
using Util;

namespace Pose
{
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

    public class Figure : MonoBehaviour
    {
        readonly List<Polygon> poseMarkers = new();
        readonly List<StaticLink> jointLinks = new();

        public int role;

        // only set when figure is fully defined
        public List<List<Vector2>?> posesByFrame = new();

        void Awake()
        {
            string[] enumNames = Enum.GetNames(typeof(Joints));
            for (int j = 0; j < enumNames.Length; j++)
            {
                Polygon sphere = Instantiate(PolygonFactory.Instance.icosahedron0);
                sphere.AddCollider(new Vector3(3, 1000, 3)); // compensate for flat photo container
                sphere.gameObject.SetActive(false);
                sphere.transform.localScale = Vector3.one * .005f;
                sphere.transform.SetParent(transform, false);
                sphere.name = enumNames[j];
                sphere.myFigure = this;
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

        public void DrawNames()
        {
            foreach (Polygon poseMarker in poseMarkers)
            {
                TextBox textBox = TextBox.Create(poseMarker.name);
                textBox.Size = 20;
                textBox.Color = poseMarker.DefaultColor;
                textBox.transform.SetParent(poseMarker.transform, false);
                textBox.transform.Rotate(Vector3.right, 90);
                textBox.transform.Translate(Vector3.right * .005f);
                textBox.gameObject.SetActive(true);
            }
        }

        StaticLink LinkFromTo(int index1, int index2, IReadOnlyList<Polygon> joints)
        {
            StaticLink staticLink = Instantiate(StaticLink.prototypeStaticLink);
            staticLink.gameObject.SetActive(true);
            staticLink.transform.SetParent(transform, false);
            staticLink.LinkFromTo(joints[index1].transform, joints[index2].transform);
            staticLink.SetColor(Viridis.ViridisColor((float)index1 / Enum.GetNames(typeof(Joints)).Length));
            return staticLink;
        }

        public List<Vector2> Get2DPose(Main.ImgMetadata? imgMetadata)
        {
            if (imgMetadata == null) return new List<Vector2>();
            return poseMarkers.Select(poseMarker => new Vector2(
                poseMarker.transform.localPosition.x * imgMetadata.Width,
                poseMarker.transform.localPosition.z * imgMetadata.Height)).ToList();
        }

        public void SetRole(int setRole)
        {
            role = setRole;
            Color figureColor = role switch
            {
                0 => Color.red,
                1 => Color.magenta,
                -1 => Color.grey,
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (Polygon poseMarker in poseMarkers)
            {
                poseMarker.SetColor(figureColor);
            }

            foreach (StaticLink staticLink in jointLinks)
            {
                staticLink.SetColor(figureColor);
            }
        }

        public void SetPositions(int currentFrame, Main.ImgMetadata? imgMetadata)
        {
            if (imgMetadata == null) return;
            List<Vector2>? pose = posesByFrame[currentFrame];
            if (pose == null)
            {
                pose = new List<Vector2>();
                posesByFrame[currentFrame] = pose;
            }

            for (int i = 0; i < pose.Count; i++)
            {
                pose[i] = new Vector2(
                    (int)Math.Round(poseMarkers[i].transform.localPosition.x * imgMetadata.Width),
                    (int)Math.Round(poseMarkers[i].transform.localPosition.z * imgMetadata.Height));
            }

            UpdateLinks();
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

        public void Set2DPose(List<Vector2> pose, Main.ImgMetadata? imgMetadata)
        {
            if (imgMetadata == null) return;
            for (int j = 0; j < pose.Count; j++)
            {
                Polygon sphere = poseMarkers[j];
                sphere.gameObject.SetActive(true);

                sphere.transform.localPosition = new Vector3(
                    pose[j].x / imgMetadata.Width,
                    0,
                    pose[j].y / imgMetadata.Height);
            }

            UpdateLinks();
        }

        /// <summary>
        /// Only used when figure is fully defined
        /// </summary>
        public void Set2DPose(int frameNumber, Main.ImgMetadata? imgMetadata)
        {
            if (imgMetadata == null) return;
            List<Vector2>? pose = posesByFrame[frameNumber];

            if (pose == null) return;

            for (int j = 0; j < pose.Count; j++)
            {
                Polygon sphere = poseMarkers[j];
                sphere.gameObject.SetActive(true);

                sphere.transform.localPosition = new Vector3(
                    pose[j].x / imgMetadata.Width,
                    0,
                    pose[j].y / imgMetadata.Height);
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

        public bool HasPoseValueAt(int frameNumber)
        {
            return posesByFrame[frameNumber] != null;
        }
    }
}