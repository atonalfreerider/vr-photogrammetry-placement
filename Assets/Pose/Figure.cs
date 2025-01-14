﻿#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Shapes;
using Shapes.Lines;
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

    public enum Limbs
    {
        // Precomputed with Szudzik pairing to correspond with joint indices
        R_Upper_Arm = 70,
        L_Upper_Arm = 54,
        R_Forearm = 108,
        L_Forearm = 88,
        R_Thigh = 208,
        L_Thigh = 180,
        R_Calf = 270,
        L_Calf = 238,
        Pelvis = 167,
        Shoulders = 47
    }

    public class Figure : MonoBehaviour
    {
        readonly List<Polygon> poseMarkers = new();
        readonly List<StaticLink> jointLinks = new();

        public int role;

        // only set when figure is fully defined
        public List<List<Vector2>?> posesByFrame = new();
        public Dictionary<Limbs, float> LimbLengths = new();

        readonly List<List<Vector3>> finalPoses = new();

        Main.ImgMetadata? imgMetadata => transform.parent.GetComponent<CameraSetup>().imgMeta;

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

        public List<Vector2> Get2DPoseFromCurrentMarkers()
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

        public void Set2DPoseToCurrentMarkerPositionsAt(int currentFrame)
        {
            if (imgMetadata == null || currentFrame >= posesByFrame.Count) return;
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

        public void Set3DPose(List<Vector3> pose)
        {
            for (int i = 0; i < poseMarkers.Count; i++)
            {
                poseMarkers[i].transform.localPosition = pose[i];
            }

            UpdateLinks();
        }

        public void Set3DPoseAt(int frameNumber)
        {
            if (finalPoses.Count <= frameNumber) return;
            Set3DPose(finalPoses[frameNumber]);
        }

        public void AddFinal3DPose(List<Vector3> pose)
        {
            finalPoses.Add(pose);
        }

        public void SetMarkersToPose(List<Vector2> pose)
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
        public void SetMarkersToPoseAt(int frameNumber)
        {
            if (imgMetadata == null) return;
            List<Vector2>? pose = posesByFrame[frameNumber];

            if (pose == null) return;

            SetMarkersToPose(pose);
        }

        public void SetPoseMarkerColliders(bool isOn)
        {
            foreach (Polygon poseMarker in poseMarkers)
            {
                poseMarker.GetComponent<BoxCollider>().enabled = isOn;
            }
        }

        public Polygon GetCurrentPoseJoint(int jointNumber)
        {
            return poseMarkers[jointNumber];
        }

        public Vector3 GetJointAtFrame(int joint, int frame)
        {
            if (frame >= posesByFrame.Count) return Vector3.zero;
            List<Vector2>? pose = posesByFrame[frame];
            if (pose == null) return Vector3.zero;

            // project onto photo
            return transform.TransformPoint(new Vector3(
                pose[joint].x / imgMetadata.Width,
                0,
                pose[joint].y / imgMetadata.Height));
        }

        public bool HasPoseValueAt(int frameNumber)
        {
            return posesByFrame[frameNumber] != null;
        }

        public void FlipPose(int frameNumber)
        {
            List<Vector3> pose = poseMarkers.Select(x => x.transform.localPosition).ToList();

            poseMarkers[(int)Joints.L_Shoulder].transform.localPosition = pose[(int)Joints.R_Shoulder];
            poseMarkers[(int)Joints.R_Shoulder].transform.localPosition = pose[(int)Joints.L_Shoulder];
            poseMarkers[(int)Joints.L_Elbow].transform.localPosition = pose[(int)Joints.R_Elbow];
            poseMarkers[(int)Joints.R_Elbow].transform.localPosition = pose[(int)Joints.L_Elbow];
            poseMarkers[(int)Joints.L_Wrist].transform.localPosition = pose[(int)Joints.R_Wrist];
            poseMarkers[(int)Joints.R_Wrist].transform.localPosition = pose[(int)Joints.L_Wrist];
            poseMarkers[(int)Joints.L_Hip].transform.localPosition = pose[(int)Joints.R_Hip];
            poseMarkers[(int)Joints.R_Hip].transform.localPosition = pose[(int)Joints.L_Hip];
            poseMarkers[(int)Joints.L_Knee].transform.localPosition = pose[(int)Joints.R_Knee];
            poseMarkers[(int)Joints.R_Knee].transform.localPosition = pose[(int)Joints.L_Knee];
            poseMarkers[(int)Joints.L_Ankle].transform.localPosition = pose[(int)Joints.R_Ankle];
            poseMarkers[(int)Joints.R_Ankle].transform.localPosition = pose[(int)Joints.L_Ankle];

            Set2DPoseToCurrentMarkerPositionsAt(frameNumber);
        }

        public void SerializeFinal3DPosesTo(string jsonDirectory)
        {
            List<List<Float3>> finalFloat3Poses = finalPoses
                .Select(pose => pose
                    .Select(v => new Float3(v.x, v.y, v.z)).ToList()).ToList();
            string jsonString = JsonConvert.SerializeObject(finalFloat3Poses, Formatting.Indented);
            string jsonPath = Path.Combine(jsonDirectory, $"figure{role}.json");
            File.WriteAllText(jsonPath, jsonString);
            
            Debug.Log($"Serialized {finalPoses.Count} poses to {jsonPath}");
        }
        
        [Serializable]
        class Float3
        {
            public float x;
            public float y;
            public float z;
            
            public Float3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }
    }
}