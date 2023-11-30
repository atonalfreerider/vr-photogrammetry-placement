﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pose;
using Shapes;
using Shapes.Lines;
using UnityEngine;

public class PoseOverlay : MonoBehaviour
{
    List<List<List<Vector2>>> figuresByFrame = new();

    /// <summary>
    /// N number of figures to be drawn per frame. They have no persistence.
    /// </summary>
    readonly List<Figure> unknownFigures = new();
    readonly List<Figure> definedFigures = new();

    readonly List<StaticLink> figureSpears = new();

    bool poseMarkerCollidersOn = false;

    public void InitFigures(
        GameObject photo,
        List<List<List<Vector2>>> posesPerFrame,
        List<SqliteInput.DbFigure> figuresPerFrame)
    {
        figuresByFrame = posesPerFrame;

        int figureCount = 0;
        foreach (SqliteInput.DbFigure dbFigure in figuresPerFrame)
        {
            Figure newFigure = photo.AddComponent<Figure>();
            newFigure.SetRole(figureCount);
            newFigure.posesByFrame = dbFigure.PosesByFrame;
            definedFigures.Add(newFigure);
            newFigure.DrawNames();
            figureCount++;
        }
        
        DrawFigureSpears();
    }
    
    void DrawFigureSpears()
    {
        for (int i = 0; i < definedFigures.Count; i++)
        {
            StaticLink figureSpear = Instantiate(StaticLink.prototypeStaticLink);
            figureSpear.gameObject.SetActive(false);
            figureSpear.transform.SetParent(transform, false);
            figureSpear.SetColor(Color.red);
            figureSpears.Add(figureSpear);
        }
    }

    public void LoadPose(int frameNumber, Main.ImgMetadata imgMeta, GameObject photo)
    {
        foreach (Figure figure in unknownFigures)
        {
            figure.SetVisible(false);
        }

        foreach (Figure figure in definedFigures)
        {
            figure.Set2DPose(frameNumber, imgMeta);
        }

        if (definedFigures.All(x => x.HasPoseValueAt(frameNumber))) return;

        foreach (Figure figure in definedFigures)
        {
            figure.SetVisible(false);
        }

        List<List<Vector2>> frame = figuresByFrame[frameNumber];
        for (int i = 0; i < frame.Count; i++)
        {
            if (unknownFigures.Count <= i)
            {
                Figure figure = photo.AddComponent<Figure>();
                figure.SetRole(-1);
                unknownFigures.Add(figure);
            }

            Figure figureAtI = unknownFigures[i];
            figureAtI.SetVisible(true);
            figureAtI.Set2DPose(frame[i], imgMeta);
            figureAtI.SetPoseMarkerColliders(poseMarkerCollidersOn);
        }
    }

    public void DrawSpear(int jointNumber)
    {
        int count = 0;
        foreach (Figure definedFigure in definedFigures)
        {
            Polygon figure0Target = definedFigure.GetJoint(jointNumber);
            StaticLink figure0Spear = figureSpears[count];
            figure0Spear.LinkFromTo(transform, figure0Target.transform);
            figure0Spear.UpdateLink();
            figure0Spear.SetLength(10);
            figure0Spear.gameObject.SetActive(figure0Target.gameObject.activeInHierarchy);
            count++;
        }
    }

    public Tuple<Ray?, Ray?>[] PoseRays()
    {
        Tuple<Ray?, Ray?>[] returnList = new Tuple<Ray?, Ray?>[Enum.GetNames(typeof(Joints)).Length];

        for (int i = 0; i < returnList.Length; i++)
        {
            Ray? ray1 = null;
            Ray? ray2 = null;
            Polygon figure0PoseMarker = definedFigures[0].GetJoint(i);
            if (figure0PoseMarker.gameObject.activeInHierarchy)
            {
                ray1 = new Ray(
                    transform.position,
                    Vector3.Normalize(figure0PoseMarker.transform.position - transform.position));
            }

            Polygon figure1PoseMarker = definedFigures[1].GetJoint(i);
            if (figure1PoseMarker.gameObject.activeInHierarchy)
            {
                ray2 = new Ray(
                    transform.position,
                    Vector3.Normalize(figure1PoseMarker.transform.position - transform.position));
            }

            returnList[i] = new Tuple<Ray?, Ray?>(ray1, ray2);
        }

        return returnList;
    }

    public void SetMarkers(bool isOn)
    {
        poseMarkerCollidersOn = isOn;
        foreach (Figure unknownFigure in unknownFigures)
        {
            unknownFigure.SetPoseMarkerColliders(poseMarkerCollidersOn);
        }
    }

    public void CopyPoseAtFrameTo(Figure targetedFigure, int role, int currentFrameNumber, Main.ImgMetadata imgMeta)
    {
        if (role < 0) return;
        
        Figure figure0 = definedFigures[role];
        figure0.posesByFrame[currentFrameNumber] = targetedFigure.Get2DPose(imgMeta);
        figure0.Set2DPose(currentFrameNumber, imgMeta);
        figure0.SetVisible(true);

        targetedFigure.SetVisible(false);
    }

    public Tuple<Figure, Figure> GetFigures()
    {
        return new Tuple<Figure, Figure>(definedFigures[0], definedFigures[1]);
    }
}