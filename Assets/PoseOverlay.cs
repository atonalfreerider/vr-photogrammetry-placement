using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pose;
using Shapes;
using Shapes.Lines;
using UnityEngine;

public class PoseOverlay : MonoBehaviour
{
    List<Figure> figures2D = new();

    readonly List<StaticLink> figureSpears = new();

    bool poseMarkerCollidersOn = false;

    public void InitFigures(string dirPath)
    {
        foreach (string poseJsonPath in Directory.EnumerateFiles(dirPath, "*.json"))
        {
            if (poseJsonPath.Contains("grounding")) continue;

            Figure newFigure = Figure.ReadAll2DPosesFrom(poseJsonPath);
            newFigure.gameObject.transform.SetParent(transform, false);
            figures2D.Add(newFigure);
        }
        DrawFigureSpears();
    }
    
    void DrawFigureSpears()
    {
        for (int i = 0; i < figures2D.Count; i++)
        {
            StaticLink figureSpear = Instantiate(StaticLink.prototypeStaticLink);
            figureSpear.gameObject.SetActive(false);
            figureSpear.transform.SetParent(transform, false);
            figureSpear.SetColor(Color.red);
            figureSpears.Add(figureSpear);
        }
    }

    public void LoadPose(int frameNumber, GameObject photo)
    {
        foreach (Figure figure2D in figures2D)
        {
            figure2D.Set2DPoseToCurrentMarkerPositionsAt(frameNumber);
        }
    }

    public void DrawSpear(int jointNumber)
    {
        int count = 0;
        foreach (Figure definedFigure in figures2D)
        {
            Polygon figure0Target = definedFigure.GetCurrentPoseJoint(jointNumber);
            StaticLink figure0Spear = figureSpears[count];
            figure0Spear.LinkFromTo(transform, figure0Target.transform);
            figure0Spear.UpdateLink();
            figure0Spear.SetLength(10);
            figure0Spear.gameObject.SetActive(figure0Target.gameObject.activeInHierarchy);
            count++;
        }
    }

    public Ray?[] PoseRays(int figureCount)
    {
        Ray?[] returnList = new Ray?[Enum.GetNames(typeof(CocoJoint)).Length];

        for (int i = 0; i < returnList.Length; i++)
        {
            Ray? rayToJoint = null;
            Polygon figureJoint = figures2D[figureCount].GetCurrentPoseJoint(i);
            if (figureJoint.gameObject.activeInHierarchy)
            {
                rayToJoint = new Ray(
                    transform.position,
                    Vector3.Normalize(figureJoint.transform.position - transform.position));
            }

            returnList[i] = rayToJoint;
        }

        return returnList;
    }
    
    public Ray RayFromJointFromFigureAtFrame(int jointNumber, int figureCount, int frameNumber)
    {
        Vector3 figureJoint = figures2D[figureCount].GetJointAtFrame(jointNumber, frameNumber);
        return new Ray(
            transform.position,
            Vector3.Normalize(figureJoint - transform.position));
    }



    public void CopyPoseAtFrameTo(Figure targetedFigure, int role, int currentFrameNumber)
    {
        if (role < 0) return;
        
        Figure figure0 = figures2D[role];
        figure0.posesByFrame2D[currentFrameNumber] = targetedFigure.Get2DPoseFromCurrentMarkers();
        figure0.SetMarkersToPoseAt(currentFrameNumber);
        figure0.SetVisible(true);

        targetedFigure.SetVisible(false);
    }

    public Tuple<Figure, Figure> GetFigures()
    {
        return new Tuple<Figure, Figure>(figures2D[0], figures2D[1]);
    }
}