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
    Figure figure0;
    Figure figure1;

    Polygon figure0GroundFoot;
    Polygon figure1GroundFoot;

    Polygon? currentHighlightedMarker;

    int currentSpearNumber = 0;

    public Mover mover;

    public void Init(Dictionary<int, CameraSetup> cameras)
    {
        figure0 = new GameObject("figure0").AddComponent<Figure>(); 
        figure1 = new GameObject("figure1").AddComponent<Figure>(); 
        figure0.transform.SetParent(transform, false); 
        figure1.transform.SetParent(transform, false); 

        
        int count = 0;
        SqliteInput sqliteInput = GetComponent<SqliteInput>();

        List<List<List<List<Vector2>>>> figuredByCamera = sqliteInput.ReadFrameFromDb();
        List<SqliteInput.DbFigure> figure0ByCamera = sqliteInput.ReadFiguresFromAllCameras(0);
        List<SqliteInput.DbFigure> figure1ByCamera = sqliteInput.ReadFiguresFromAllCameras(1);

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if (cameraSetup.PoseOverlay != null)
            {
                cameraSetup.PoseOverlay.InitFigures(
                    cameraSetup.GetPhotoGameObject,
                    figuredByCamera[count],
                    figure0ByCamera[count],
                    figure1ByCamera[count]);
            }
            count++;
        }
    }

    public void DrawNextSpear(Main.InteractionMode interactionMode, Dictionary<int, CameraSetup> cameras)
    {
        if (interactionMode != Main.InteractionMode.PhotoAlignment) return;

        currentSpearNumber++;
        if (currentSpearNumber > Enum.GetNames(typeof(Joints)).Length)
            currentSpearNumber = Enum.GetNames(typeof(Joints)).Length;

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if(cameraSetup.PoseOverlay == null) continue;
            cameraSetup.PoseOverlay.DrawSpear(currentSpearNumber);
        }
    }

    public void DrawPreviousSpear(Main.InteractionMode interactionMode, Dictionary<int, CameraSetup> cameras)
    {
        if (interactionMode != Main.InteractionMode.PhotoAlignment) return;

        currentSpearNumber--;
        if (currentSpearNumber < 0) currentSpearNumber = 0;

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if(cameraSetup.PoseOverlay == null) continue;
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
            hit == null) return;

        Figure myFigure = hit.Value.collider.GetComponent<Polygon>().myFigure;
        CameraSetup myCameraSetup = myFigure.transform.parent.GetComponent<CameraSetup>();
        if(myCameraSetup.PoseOverlay == null) return;
        myCameraSetup.PoseOverlay.CopyPoseAtFrameTo(myFigure, role, currentFrameNumber, myCameraSetup.imgMeta);
    }

    public void Draw3DPose(Dictionary<int, CameraSetup> cameras)
    {
        // blind group rays based on arbitrary 50/50 could be figure0 or figure1
        List<Ray>[] allRays = new List<Ray>[Enum.GetNames(typeof(Joints)).Length];
        for (int i = 0; i < allRays.Length; i++)
        {
            allRays[i] = new List<Ray>();
        }

        foreach (CameraSetup cameraSetup in cameras.Values)
        {
            if(cameraSetup.PoseOverlay == null) continue;
            Tuple<Ray?, Ray?>[] camRays = cameraSetup.PoseOverlay.PoseRays();
            int jointCount = 0;
            foreach (Tuple<Ray?, Ray?> camRay in camRays)
            {
                if (camRay.Item1 != null)
                {
                    allRays[jointCount].Add(camRay.Item1.Value);
                }

                if (camRay.Item2 != null)
                {
                    allRays[jointCount].Add(camRay.Item2.Value);
                }

                jointCount++;
            }
        }

        int jointCount2 = 0;
        List<Vector3?> figure0Pose = new();
        List<Vector3?> figure1Pose = new();
        foreach (List<Ray> jointRays in allRays)
        {
            // find the locus of each of these lists
            Vector3 jointLocus = RayMidpointFinder.FindMinimumMidpoint(jointRays);

            // re-sort on the basis if they are closer to one locus or another
            Tuple<List<Ray>, List<Ray>> sortedRays = SortRays(jointRays, jointLocus);

            // finally set target for re-sorted lists
            if (sortedRays.Item1.Count > 1)
            {
                Vector3 figure0JointLocus = RayMidpointFinder.FindMinimumMidpoint(sortedRays.Item1);
                figure0Pose.Add(figure0JointLocus - transform.position);
            }
            else
            {
                figure0Pose.Add(null);
            }

            if (sortedRays.Item2.Count > 1)
            {
                Vector3 figure1JointLocus = RayMidpointFinder.FindMinimumMidpoint(sortedRays.Item2);
                figure1Pose.Add(figure1JointLocus - transform.position);
            }
            else
            {
                figure1Pose.Add(null);
            }

            jointCount2++;
        }

        figure0.Set3DPose(figure0Pose);
        figure1.Set3DPose(figure1Pose);
    }

    static Tuple<List<Ray>, List<Ray>> SortRays(List<Ray> rays, Vector3 target)
    {
        List<Ray> leftRays = new List<Ray>();
        List<Ray> rightRays = new List<Ray>();

        foreach (Ray ray in rays)
        {
            Vector3 toRay = ray.origin - target;
            Vector3 cross = Vector3.Cross(Vector3.up, toRay);

            // Determine which side of the target the ray is on
            if (Vector3.Dot(cross, ray.direction) > 0)
            {
                rightRays.Add(ray);
            }
            else
            {
                leftRays.Add(ray);
            }
        }

        return new Tuple<List<Ray>, List<Ray>>(leftRays, rightRays);
    }

    void Update()
    {
        RaycastHit? hit = mover.CastRay();
        if (hit.HasValue && hit.Value.collider != null && hit.Value.collider.GetComponent<Polygon>())
        {
            Polygon hitPolygon = hit.Value.collider.GetComponent<Polygon>();

            if (currentHighlightedMarker != null)
            {
                currentHighlightedMarker.UnHighlight();
            }

            currentHighlightedMarker = hitPolygon;
            currentHighlightedMarker.Highlight();
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
            foreach (Tuple<Figure, Figure> figures in cameras.Select(cameraSetup => cameraSetup.PoseOverlay.GetFigures()))
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

            Debug.Log("Saved to " + sqliteOutput.DbPath);
        }
    }
}