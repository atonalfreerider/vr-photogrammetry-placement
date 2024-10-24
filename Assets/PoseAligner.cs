#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pose;
using Shapes;
using Shapes.Lines;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

public class PoseAligner : MonoBehaviour
{
    public int FrameMax = 0;
    public string JsonDir;
    
    readonly List<Figure> defined3DFigures = new();

    Polygon? currentHighlightedMarker;
    int currentSpearNumber = 0;
    public Mover? mover;

    List<List<Vector3>> Read(string jsonPath)
    {
        string jsonString = File.ReadAllText(jsonPath);
        List<List<Figure.Float3>> allPoses = JsonConvert.DeserializeObject<List<List<Figure.Float3>>>(jsonString);
        List<List<Vector3>> allPosesVector3 = allPoses
            .Select(pose => pose.Select(float3 => new Vector3(float3.x, float3.y, float3.z)).ToList()).ToList();

        return allPosesVector3;
    }

    public void Init(Dictionary<int, CameraSetup> cameras)
    {
        string followPath = Path.Combine(JsonDir, "aline-3d-spin.json");
        string leadPath = Path.Combine(JsonDir, "carlos-3d-spin.json");
        
        List<List<Vector3>> followPoses = Read(followPath);
        List<List<Vector3>> leadPoses = Read(leadPath);
        
        FrameMax = followPoses.Count;
        
        for (int i = 0; i < 2; i++)
        {
            Figure figure3D = new GameObject($"figure3D-{i}").AddComponent<Figure>();
            figure3D.transform.SetParent(transform, false);
            figure3D.SetVisible(true);
            defined3DFigures.Add(figure3D);
            figure3D.Init(i == 0 ? followPoses : leadPoses, PoseType.Smpl);
        }
    }

    public void DrawNextSpear(Dictionary<int, CameraSetup> cameras)
    {
        currentSpearNumber++;
        if (currentSpearNumber > Enum.GetNames(typeof(CocoJoint)).Length)
            currentSpearNumber = Enum.GetNames(typeof(CocoJoint)).Length;

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if (cameraSetup.PoseOverlay == null) continue;
            cameraSetup.PoseOverlay.DrawSpear(currentSpearNumber);
        }
    }

    public void DrawPreviousSpear(Dictionary<int, CameraSetup> cameras)
    {
        currentSpearNumber--;
        if (currentSpearNumber < 0) currentSpearNumber = 0;

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if (cameraSetup.PoseOverlay == null) continue;
            cameraSetup.PoseOverlay.DrawSpear(currentSpearNumber);
        }
    }

    public static void MarkRole(
        int role,
        Mover mover,
        Main.InteractionMode interactionMode,
        int currentFrameNumber)
    {
        RaycastHit? hit = mover.CastRay();
        if (interactionMode != Main.InteractionMode.PoseAlignment ||
            hit == null ||
            hit.Value.collider == null ||
            hit.Value.collider.GetComponent<Polygon>() == null) return;

        Figure myFigure = hit.Value.collider.GetComponent<Polygon>().myFigure;
        CameraSetup myCameraSetup = myFigure.transform.parent.GetComponent<CameraSetup>();
        if (myCameraSetup.PoseOverlay == null) return;
        myCameraSetup.PoseOverlay.CopyPoseAtFrameTo(myFigure, role, currentFrameNumber);
    }

    public void Draw3DPoses(List<CameraSetup> cameras)
    {
        for (int i = 0; i < defined3DFigures.Count; i++)
        {
            Draw3DPose(cameras, i);
        }
    }

    public void Draw3DPoseAtFrame(int frameNumber)
    {
        foreach (Figure defined3DFigure in defined3DFigures)
        {
            defined3DFigure.Set3DPoseAt(frameNumber);
        }
    }

    void Draw3DPose(List<CameraSetup> cameras, int figureCount)
    {
        List<Ray>[] allRaysPointingToJoints = new List<Ray>[Enum.GetNames(typeof(CocoJoint)).Length];
        for (int i = 0; i < allRaysPointingToJoints.Length; i++)
        {
            allRaysPointingToJoints[i] = new List<Ray>();
        }

        foreach (CameraSetup cameraSetup in cameras)
        {
            if (cameraSetup.PoseOverlay == null) continue;
            Ray?[] poseRays = cameraSetup.PoseOverlay.PoseRays(figureCount);
            int jointCount = 0;
            foreach (Ray? poseRay in poseRays)
            {
                if (poseRay != null)
                {
                    allRaysPointingToJoints[jointCount].Add(poseRay.Value);
                }

                jointCount++;
            }
        }

        List<Vector3> figure3DPose = allRaysPointingToJoints.Select(RayMidpointFinder.FindMinimumMidpoint).ToList();
        defined3DFigures[figureCount].Set3DPose(figure3DPose);
    }

    static IEnumerable<Vector3> BezierTrackForJointForFigure(
        List<CameraSetup> cameras, 
        int joint,
        int figureCount,
        int frameCount)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < frameCount; i++)
        {
            List<Ray> allRaysForFrameAtJoint = new List<Ray>();
            foreach (CameraSetup cameraSetup in cameras)
            {
                if (cameraSetup.PoseOverlay == null) continue;
                Ray ray = cameraSetup.PoseOverlay.RayFromJointFromFigureAtFrame(joint, figureCount, i);
                allRaysForFrameAtJoint.Add(ray);
            }

            points.Add(RayMidpointFinder.FindMinimumMidpoint(allRaysForFrameAtJoint));
        }

        return Line.MovingAverageSmoothing(points, 4);
    }

    void Update()
    {
        RaycastHit? hit = mover != null ? mover.CastRay() : null;
        if (hit != null && hit.Value.collider != null && hit.Value.collider.GetComponent<Polygon>())
        {
            Polygon hitPolygon = hit.Value.collider.GetComponent<Polygon>();

            if (currentHighlightedMarker != null)
            {
                currentHighlightedMarker.UnHighlight();
            }

            currentHighlightedMarker = hitPolygon;
            currentHighlightedMarker.Highlight();

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                hitPolygon.myFigure.FlipPose(Main.Instance.GetCurrentFrameNumber());
            }

            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                hitPolygon.myFigure.SetMarkersToPoseAt(Main.Instance.GetCurrentFrameNumber() - 1);
                hitPolygon.myFigure.Set2DPoseToCurrentMarkerPositionsAt(Main.Instance.GetCurrentFrameNumber());
            }
        }
        else if (currentHighlightedMarker != null)
        {
            currentHighlightedMarker.UnHighlight();
            currentHighlightedMarker = null;
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {


            List<List<Vector2>?> figure0Poses = new();
            List<List<Vector2>?> figure1Poses = new();
            int frameCount = 0;
            List<CameraSetup> cameras = Main.Instance.GetCameras();
            foreach (Tuple<Figure, Figure> figures in cameras.Select(
                         cameraSetup => cameraSetup.PoseOverlay.GetFigures()))
            {
                figure0Poses.AddRange(figures.Item1.posesByFrame2D);
                figure1Poses.AddRange(figures.Item2.posesByFrame2D);
                if (frameCount < figures.Item1.posesByFrame2D.Count)
                {
                    frameCount = figures.Item1.posesByFrame2D.Count;
                }
            }

            // TODO serialize to json

            int countNotNull = figure0Poses.Concat(figure1Poses).Count(pose => pose != null);

            string path = "";
            Debug.Log($"Saved to {path} Wrote {countNotNull} poses");
        }

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            foreach (Figure defined3DFigure in defined3DFigures)
            {
                defined3DFigure.SerializeFinal3DPosesTo(JsonDir);
            }
        }
    }

    public void DrawAllTrails(int frameCount)
    {
        List<CameraSetup> cameras = Main.Instance.GetCameras();
        for (int i = 0; i < defined3DFigures.Count; i++)
        {
            List<List<Vector3>> allCurves = new();
            for (int j = 0; j < Enum.GetNames(typeof(CocoJoint)).Length; j++)
            {
                List<Vector3> curve = BezierTrackForJointForFigure(cameras, j, i, frameCount).ToList();
                allCurves.Add(curve);
            }

            float rightCalfDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.R_Knee], allCurves[(int)CocoJoint.R_Ankle]);
            float leftCalfDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.L_Knee], allCurves[(int)CocoJoint.L_Ankle]);
            
            float averageCalfDistance = (rightCalfDistance + leftCalfDistance) / 2;
            
            float rightThighDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.R_Hip], allCurves[(int)CocoJoint.R_Knee]);
            float leftThighDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.L_Hip], allCurves[(int)CocoJoint.L_Knee]);
            
            float averageThighDistance = (rightThighDistance + leftThighDistance) / 2;
            
            float rightArmDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.R_Shoulder], allCurves[(int)CocoJoint.R_Elbow]);
            float leftArmDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.L_Shoulder], allCurves[(int)CocoJoint.L_Elbow]);
            
            float averageUpperArmDistance = (rightArmDistance + leftArmDistance) / 2;
            
            float rightForearmDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.R_Elbow], allCurves[(int)CocoJoint.R_Wrist]);
            float leftForearmDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.L_Elbow], allCurves[(int)CocoJoint.L_Wrist]);
            
            float averageForearmDistance = (rightForearmDistance + leftForearmDistance) / 2;
            
            float pelvisDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.R_Hip], allCurves[(int)CocoJoint.L_Hip]);
            float shouldersDistance = AverageDistanceBetweenCurves(allCurves[(int)CocoJoint.R_Shoulder], allCurves[(int)CocoJoint.L_Shoulder]);
            
            Figure defined3DFigure = defined3DFigures[i];
            
            defined3DFigure.LimbLengths.Add(CocoLimbs.R_Calf, averageCalfDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.L_Calf, averageCalfDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.R_Thigh, averageThighDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.L_Thigh, averageThighDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.R_Upper_Arm, averageUpperArmDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.L_Upper_Arm, averageUpperArmDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.R_Forearm, averageForearmDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.L_Forearm, averageForearmDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.Pelvis, pelvisDistance);
            defined3DFigure.LimbLengths.Add(CocoLimbs.Shoulders, shouldersDistance);

            for (int k = 0; k < frameCount; k++)
            {
                List<Vector3> final3DPose = new();
                for(int j = 0; j < Enum.GetNames(typeof(CocoJoint)).Length; j++)
                {
                    Vector3 jointPos = Vector3.zero;
                    final3DPose.Add(jointPos);
                }
                
                Vector3 rAnklePos = allCurves[(int)CocoJoint.R_Ankle][k];
                Vector3 lAnklePos = allCurves[(int)CocoJoint.L_Ankle][k];
                
                Vector3 rKneePos = allCurves[(int)CocoJoint.R_Knee][k];
                Vector3 lKneePos = allCurves[(int)CocoJoint.L_Knee][k];
                
                Vector3 rHipPos = allCurves[(int)CocoJoint.R_Hip][k];
                Vector3 lHipPos = allCurves[(int)CocoJoint.L_Hip][k];

                if (rAnklePos.y < lAnklePos.y)
                {
                    // right ankle is lower to the ground
                    final3DPose[(int)CocoJoint.R_Ankle] = rAnklePos;
                    rKneePos = rAnklePos + (rKneePos - rAnklePos).normalized * averageCalfDistance;
                    final3DPose[(int)CocoJoint.R_Knee] = rKneePos;
                    rHipPos = rKneePos + (rHipPos - rKneePos).normalized * averageThighDistance;
                    final3DPose[(int)CocoJoint.R_Hip] = rHipPos;
                    lHipPos = rHipPos + (lHipPos - rHipPos).normalized * pelvisDistance;
                    final3DPose[(int)CocoJoint.L_Hip] = lHipPos;
                    lKneePos = lHipPos + (lKneePos - lHipPos).normalized * averageThighDistance;
                    final3DPose[(int)CocoJoint.L_Knee] = lKneePos;
                    lAnklePos = lKneePos + (lAnklePos - lKneePos).normalized * averageCalfDistance;
                    final3DPose[(int)CocoJoint.L_Ankle] = lAnklePos;
                }
                else
                {
                    final3DPose[(int)CocoJoint.L_Ankle] = lAnklePos;
                    lKneePos = lAnklePos + (lKneePos - lAnklePos).normalized * averageCalfDistance;
                    final3DPose[(int)CocoJoint.L_Knee] = lKneePos;
                    lHipPos = lKneePos + (lHipPos - lKneePos).normalized * averageThighDistance;
                    final3DPose[(int)CocoJoint.L_Hip] = lHipPos;
                    rHipPos = lHipPos + (rHipPos - lHipPos).normalized * pelvisDistance;
                    final3DPose[(int)CocoJoint.R_Hip] = rHipPos;
                    rKneePos = rHipPos + (rKneePos - rHipPos).normalized * averageThighDistance;
                    final3DPose[(int)CocoJoint.R_Knee] = rKneePos;
                    rAnklePos = rKneePos + (rAnklePos - rKneePos).normalized * averageCalfDistance;
                    final3DPose[(int)CocoJoint.R_Ankle] = rAnklePos;
                }
                
                Vector3 rShoulderPos = allCurves[(int)CocoJoint.R_Shoulder][k];
                Vector3 lShoulderPos = allCurves[(int)CocoJoint.L_Shoulder][k];
                
                Vector3 rElbowPos = allCurves[(int)CocoJoint.R_Elbow][k];
                Vector3 lElbowPos = allCurves[(int)CocoJoint.L_Elbow][k];
                
                Vector3 rWristPos = allCurves[(int)CocoJoint.R_Wrist][k];
                Vector3 lWristPos = allCurves[(int)CocoJoint.L_Wrist][k];
                
                final3DPose[(int)CocoJoint.R_Shoulder] = rShoulderPos;
                rElbowPos = rShoulderPos + (rElbowPos - rShoulderPos).normalized * averageUpperArmDistance;
                final3DPose[(int)CocoJoint.R_Elbow] = rElbowPos;
                rWristPos = rElbowPos + (rWristPos - rElbowPos).normalized * averageForearmDistance;
                final3DPose[(int)CocoJoint.R_Wrist] = rWristPos;
                
                final3DPose[(int)CocoJoint.L_Shoulder] = lShoulderPos;
                lElbowPos = lShoulderPos + (lElbowPos - lShoulderPos).normalized * averageUpperArmDistance;
                final3DPose[(int)CocoJoint.L_Elbow] = lElbowPos;
                lWristPos = lElbowPos + (lWristPos - lElbowPos).normalized * averageForearmDistance;
                final3DPose[(int)CocoJoint.L_Wrist] = lWristPos;
                
                final3DPose[(int)CocoJoint.Nose] = allCurves[(int)CocoJoint.Nose][k];
                final3DPose[(int)CocoJoint.L_Eye] = allCurves[(int)CocoJoint.L_Eye][k];
                final3DPose[(int)CocoJoint.R_Eye] = allCurves[(int)CocoJoint.R_Eye][k];
                final3DPose[(int)CocoJoint.L_Ear] = allCurves[(int)CocoJoint.L_Ear][k];
                final3DPose[(int)CocoJoint.R_Ear] = allCurves[(int)CocoJoint.R_Ear][k];

                defined3DFigure.AddFinal3DPose(final3DPose);
            }
        }
    }
    
    static float AverageDistanceBetweenCurves(IReadOnlyCollection<Vector3> curve1, IReadOnlyList<Vector3> curve2){
        return curve1
            .Select((t, i) => Vector3.Distance(t, curve2[i]))
            .Sum() / curve1.Count;
    }
}