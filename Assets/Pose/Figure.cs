#nullable enable
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
    #region POSE ENUMS

    public enum PoseType
    {
        Coco = 0,
        Halpe = 1,
        Smpl = 2
    }

    public enum CocoJoint
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

    public enum SmplJoint
    {
        Pelvis = 0,
        L_Hip = 1,
        R_Hip = 2,
        Spine1 = 3,
        L_Knee = 4,
        R_Knee = 5,
        Spine2 = 6,
        L_Ankle = 7,
        R_Ankle = 8,
        Spine3 = 9,
        L_Foot = 10,
        R_Foot = 11,
        Neck = 12,
        L_Collar = 13,
        R_Collar = 14,
        Head = 15,
        L_Shoulder = 16,
        R_Shoulder = 17,
        L_Elbow = 18,
        R_Elbow = 19,
        L_Wrist = 20,
        R_Wrist = 21,
        L_Hand = 22,
        R_Hand = 23
    }

    public enum CocoLimbs
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

    public enum SmplLimbs
    {
        L_Calf = 60, // L_Ankle to L_Knee
        R_Calf = 77, // R_Ankle to R_Knee
        L_Thigh = 17, // L_Hip to L_Knee
        R_Thigh = 27, // R_Hip to R_Knee
        L_HipToPelvis = 2, // L_Hip to Pelvis
        R_HipToPelvis = 6, // R_Hip to Pelvis
        L_UpperArm = 340, // L_Shoulder to L_Elbow
        R_UpperArm = 378, // R_Shoulder to R_Elbow
        L_Forearm = 418, // L_Elbow to L_Wrist
        R_Forearm = 460, // R_Elbow to R_Wrist
        PelvisToSpine1 = 9, // Pelvis to Spine1
        Spine3ToSpine2 = 96, // Spine3 to Spine2
        Spine2ToSpine1 = 45, // Spine2 to Spine1
        Spine3ToNeck = 153, // Spine3 to Neck
        NeckToHead = 237, // Neck to Head
        L_Foot = 107, // L_Ankle to L_Foot
        R_Foot = 129, // R_Ankle to R_Foot
        L_Hand = 526, // L_Hand to L_Wrist
        R_Hand = 573, // R_Hand to R_Wrist
        L_CollarToShoulder = 285, // L_Shoulder to L_Collar
        R_CollarToShoulder = 320, // R_Shoulder to R_Collar
        L_CollarToNeck = 194, // L_Collar to Neck
        R_CollarToNeck = 222 // R_Collar to Neck
    }

    #endregion

    public class Figure : MonoBehaviour
    {
        List<List<Vector3>> posesByFrame3D = new();
        readonly Dictionary<int, Polygon> jointPolys = new();
        StaticLink spinePoly;
        StaticLink followSpineExtension;
        StaticLink followHeadAxis;
        PoseType poseType;

        readonly List<Polygon> poseMarkers = new();
        readonly List<StaticLink> jointLinks = new();

        public int role;

        // only set when figure is fully defined
        public List<List<Vector2>?> posesByFrame2D = new();
        public Dictionary<CocoLimbs, float> LimbLengths = new();

        Main.ImgMetadata? imgMetadata => transform.parent.GetComponent<CameraSetup>().imgMeta;

        public void Init(List<List<Vector3>> posesByFrame, PoseType poseType)
        {
            posesByFrame3D = posesByFrame;
            this.poseType = poseType;
            switch (poseType)
            {
                case PoseType.Coco:
                    BuildCoco();
                    break;
                case PoseType.Halpe:
                    break;
                case PoseType.Smpl:
                    BuildSmpl();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(poseType), poseType, null);
            }
        }

        void BuildSmpl()
        {
            for (int j = 0; j < Enum.GetNames(typeof(SmplJoint)).Length; j++)
            {
                Polygon joint = Instantiate(PolygonFactory.Instance.icosahedron0);
                joint.gameObject.SetActive(true);
                joint.name = ((SmplJoint)j).ToString();
                joint.transform.SetParent(transform, false);
                joint.transform.localScale = Vector3.one * .02f;
                joint.SetColor(Cividis.CividisColor(.7f));
                jointPolys.Add(j, joint);
            }

            foreach (SmplLimbs limb in Enum.GetValues(typeof(SmplLimbs)))
            {
                uint[] pair = Szudzik.uintSzudzik2tupleReverse((uint)limb);
                StaticLink limbLink = LinkFromTo((int)pair[0], (int)pair[1]);
                jointLinks.Add(limbLink);
            }
        }

        void BuildCoco()
        {
            for (int j = 5; j < Enum.GetNames(typeof(CocoJoint)).Length; j++) // ignore head
            {
                Polygon joint = Instantiate(PolygonFactory.Instance.icosahedron0);
                joint.gameObject.SetActive(true);
                joint.name = ((CocoJoint)j).ToString();
                joint.transform.SetParent(transform, false);
                joint.transform.localScale = Vector3.one * .02f;
                joint.SetColor(Cividis.CividisColor(.7f));
                jointPolys.Add(j, joint);
            }

            foreach (CocoLimbs limb in Enum.GetValues(typeof(CocoLimbs)))
            {
                uint[] pair = Szudzik.uintSzudzik2tupleReverse((uint)limb);
                StaticLink limbLink = LinkFromTo((int)pair[0], (int)pair[1]);
                jointLinks.Add(limbLink);
            }
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

        StaticLink LinkFromTo(int index1, int index2)
        {
            StaticLink staticLink = Instantiate(StaticLink.prototypeStaticLink);
            staticLink.gameObject.SetActive(true);
            staticLink.name = poseType switch
            {
                PoseType.Coco => $"{((CocoJoint)index1).ToString()}-{((CocoJoint)index2).ToString()}",
                PoseType.Smpl => $"{((SmplJoint)index1).ToString()}-{((SmplJoint)index2).ToString()}",
                _ => throw new ArgumentOutOfRangeException(nameof(poseType), poseType,
                    null) // Handle unexpected poseType values
            };


            staticLink.SetColor(Cividis.CividisColor(.8f));
            staticLink.transform.SetParent(transform, false);
            staticLink.LinkFromTo(jointPolys[index1].transform, jointPolys[index2].transform);
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
            if (imgMetadata == null || currentFrame >= posesByFrame2D.Count) return;
            List<Vector2>? pose = posesByFrame2D[currentFrame];
            if (pose == null)
            {
                pose = new List<Vector2>();
                posesByFrame2D[currentFrame] = pose;
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
            foreach (KeyValuePair<int, Polygon> keyValuePair in jointPolys)
            {
                keyValuePair.Value.transform.localPosition = pose[keyValuePair.Key];
            }

            UpdateLinks();
        }

        public void Set3DPoseAt(int frameNumber)
        {
            Set3DPose(posesByFrame3D[frameNumber]);
        }

        public void AddFinal3DPose(List<Vector3> pose)
        {
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
            List<Vector2>? pose = posesByFrame2D[frameNumber];

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
            if (frame >= posesByFrame2D.Count) return Vector3.zero;
            List<Vector2>? pose = posesByFrame2D[frame];
            if (pose == null) return Vector3.zero;

            // project onto photo
            return transform.TransformPoint(new Vector3(
                pose[joint].x / imgMetadata.Width,
                0,
                pose[joint].y / imgMetadata.Height));
        }

        public bool HasPoseValueAt(int frameNumber)
        {
            return posesByFrame2D[frameNumber] != null;
        }

        public void FlipPose(int frameNumber)
        {
            List<Vector3> pose = poseMarkers.Select(x => x.transform.localPosition).ToList();

            poseMarkers[(int)CocoJoint.L_Shoulder].transform.localPosition = pose[(int)CocoJoint.R_Shoulder];
            poseMarkers[(int)CocoJoint.R_Shoulder].transform.localPosition = pose[(int)CocoJoint.L_Shoulder];
            poseMarkers[(int)CocoJoint.L_Elbow].transform.localPosition = pose[(int)CocoJoint.R_Elbow];
            poseMarkers[(int)CocoJoint.R_Elbow].transform.localPosition = pose[(int)CocoJoint.L_Elbow];
            poseMarkers[(int)CocoJoint.L_Wrist].transform.localPosition = pose[(int)CocoJoint.R_Wrist];
            poseMarkers[(int)CocoJoint.R_Wrist].transform.localPosition = pose[(int)CocoJoint.L_Wrist];
            poseMarkers[(int)CocoJoint.L_Hip].transform.localPosition = pose[(int)CocoJoint.R_Hip];
            poseMarkers[(int)CocoJoint.R_Hip].transform.localPosition = pose[(int)CocoJoint.L_Hip];
            poseMarkers[(int)CocoJoint.L_Knee].transform.localPosition = pose[(int)CocoJoint.R_Knee];
            poseMarkers[(int)CocoJoint.R_Knee].transform.localPosition = pose[(int)CocoJoint.L_Knee];
            poseMarkers[(int)CocoJoint.L_Ankle].transform.localPosition = pose[(int)CocoJoint.R_Ankle];
            poseMarkers[(int)CocoJoint.R_Ankle].transform.localPosition = pose[(int)CocoJoint.L_Ankle];

            Set2DPoseToCurrentMarkerPositionsAt(frameNumber);
        }

        public void SerializeFinal3DPosesTo(string jsonDirectory)
        {
            List<List<Float3>> finalFloat3Poses = posesByFrame3D
                .Select(pose => pose
                    .Select(v => new Float3(v.x, v.y, v.z)).ToList()).ToList();
            string jsonString = JsonConvert.SerializeObject(finalFloat3Poses, Formatting.Indented);
            string jsonPath = Path.Combine(jsonDirectory, $"figure{role}.json");
            File.WriteAllText(jsonPath, jsonString);

            Debug.Log($"Serialized {posesByFrame3D.Count} poses to {jsonPath}");
        }

        Figure ReadAllPosesFrom(string jsonPath, string role, PoseType poseType)
        {
            Figure figure = new GameObject(role).AddComponent<Figure>();

            string jsonString = File.ReadAllText(jsonPath);
            List<List<Float3>> allPoses = JsonConvert.DeserializeObject<List<List<Float3>>>(jsonString);
            List<List<Vector3>> allPosesVector3 = allPoses
                .Select(pose => pose.Select(float3 => new Vector3(float3.x, float3.y, float3.z)).ToList()).ToList();


            int FRAME_MAX = allPosesVector3.Count;

            return figure;
        }

        [Serializable]
        public class Float3
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