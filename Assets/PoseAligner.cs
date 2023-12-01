#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Pose;
using Shapes;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

[RequireComponent(typeof(SqliteInput))]
[RequireComponent(typeof(SqliteOutput))]
public class PoseAligner : MonoBehaviour
{
    readonly List<Figure> defined3DFigures = new();

    Polygon? currentHighlightedMarker;
    int currentSpearNumber = 0;
    public Mover? mover;

    public void Init(Dictionary<int, CameraSetup> cameras)
    {
        for (int i = 0; i < 2; i++)
        {
            Figure figure0 = new GameObject($"figure{i}").AddComponent<Figure>();
            figure0.transform.SetParent(transform, false);
            defined3DFigures.Add(figure0);
        }


        int count = 0;
        SqliteInput sqliteInput = GetComponent<SqliteInput>();

        List<List<List<List<Vector2>>>> figuredByCamera = sqliteInput.ReadFrameFromDb();
        List<List<SqliteInput.DbFigure>> allDefinedFiguresByCamera = new List<List<SqliteInput.DbFigure>>();

        for (int i = 0; i < 2; i++)
        {
            allDefinedFiguresByCamera.Add(sqliteInput.ReadFiguresFromAllCameras(i));
        }

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if (cameraSetup.PoseOverlay != null)
            {
                List<SqliteInput.DbFigure> figuresToPass = new();
                foreach (List<SqliteInput.DbFigure> figure in allDefinedFiguresByCamera)
                {
                    figuresToPass.Add(figure[count]);
                }

                cameraSetup.PoseOverlay.InitFigures(
                    cameraSetup.GetPhotoGameObject,
                    figuredByCamera[count],
                    figuresToPass);
            }

            count++;
        }
    }

    public void DrawNextSpear(Dictionary<int, CameraSetup> cameras)
    {
        currentSpearNumber++;
        if (currentSpearNumber > Enum.GetNames(typeof(Joints)).Length)
            currentSpearNumber = Enum.GetNames(typeof(Joints)).Length;

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

    public void Draw3DPoses(Dictionary<int, CameraSetup> cameras)
    {
        for (int i = 0; i < defined3DFigures.Count; i++)
        {
            Draw3DPose(cameras, i);
        }
    }

    void Draw3DPose(Dictionary<int, CameraSetup> cameras, int figureCount)
    {
        List<Ray>[] allRays = new List<Ray>[Enum.GetNames(typeof(Joints)).Length];
        for (int i = 0; i < allRays.Length; i++)
        {
            allRays[i] = new List<Ray>();
        }

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if (cameraSetup.PoseOverlay == null) continue;
            Ray?[] camRays = cameraSetup.PoseOverlay.PoseRays(figureCount);
            int jointCount = 0;
            foreach (Ray? camRay in camRays)
            {
                if (camRay != null)
                {
                    allRays[jointCount].Add(camRay.Value);
                }

                jointCount++;
            }
        }

        List<Vector3?> figure0Pose = new();
        foreach (List<Ray> jointRays in allRays)
        {
            // find the locus of each of these lists
            Vector3 jointLocus = RayMidpointFinder.FindMinimumMidpoint(jointRays);

            figure0Pose.Add(jointLocus - transform.position);
        }

        defined3DFigures[figureCount].Set3DPose(figure0Pose);
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
        }
        else if (currentHighlightedMarker != null)
        {
            currentHighlightedMarker.UnHighlight();
            currentHighlightedMarker = null;
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            SqliteOutput sqliteOutput = GetComponent<SqliteOutput>();

            List<List<Vector2>?> figure0Poses = new();
            List<List<Vector2>?> figure1Poses = new();
            int frameCount = 0;
            List<CameraSetup> cameras = Main.Instance.GetCameras();
            foreach (Tuple<Figure, Figure> figures in cameras.Select(
                         cameraSetup => cameraSetup.PoseOverlay.GetFigures()))
            {
                figure0Poses.AddRange(figures.Item1.posesByFrame);
                figure1Poses.AddRange(figures.Item2.posesByFrame);
                if (frameCount < figures.Item1.posesByFrame.Count)
                {
                    frameCount = figures.Item1.posesByFrame.Count;
                }
            }

            sqliteOutput.Serialize(0, figure0Poses, frameCount);
            sqliteOutput.Serialize(1, figure1Poses, frameCount);

            int countNotNull = figure0Poses.Concat(figure1Poses).Count(pose => pose != null);

            Debug.Log("Saved to " + sqliteOutput.DbPath + " Wrote " + countNotNull + " poses");
        }
    }
}