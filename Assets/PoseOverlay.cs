using System;
using System.Collections.Generic;
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

    Figure figure0;
    Figure figure1;

    StaticLink figure0Spear;
    StaticLink figure1Spear;

    bool poseMarkerCollidersOn = false;

    void Awake()
    {
        figure0Spear = Instantiate(StaticLink.prototypeStaticLink);
        figure0Spear.gameObject.SetActive(false);
        figure0Spear.transform.SetParent(transform, false);
        figure0Spear.SetColor(Color.red);

        figure1Spear = Instantiate(StaticLink.prototypeStaticLink);
        figure1Spear.gameObject.SetActive(false);
        figure1Spear.transform.SetParent(transform, false);
        figure1Spear.SetColor(Color.red);
    }

    public void InitFigures(
        GameObject photo,
        List<List<List<Vector2>>> posesPerFrame,
        SqliteInput.DbFigure figure0PerFrame,
        SqliteInput.DbFigure figure1PerFrame)
    {
        figuresByFrame = posesPerFrame;

        figure0 = photo.AddComponent<Figure>();
        figure0.SetRole(0);
        figure0.posesByFrame = figure0PerFrame.PosesByFrame;

        figure1 = photo.AddComponent<Figure>();
        figure1.SetRole(1);
        figure1.posesByFrame = figure1PerFrame.PosesByFrame;
    }

    public void LoadPose(int frameNumber, Main.ImgMetadata imgMeta, GameObject photo)
    {
        foreach (Figure figure in unknownFigures)
        {
            figure.SetVisible(false);
        }

        figure0.Set2DPose(frameNumber, imgMeta);
        figure1.Set2DPose(frameNumber, imgMeta);

        if (figure0.HasPoseValueAt(frameNumber) && figure1.HasPoseValueAt(frameNumber)) return;

        figure0.SetVisible(false);
        figure1.SetVisible(false);

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
        Polygon figure0Target = figure0.GetJoint(jointNumber);
        figure0Spear.LinkFromTo(transform, figure0Target.transform);
        figure0Spear.UpdateLink();
        figure0Spear.SetLength(10);
        figure0Spear.gameObject.SetActive(figure0Target.gameObject.activeInHierarchy);

        Polygon figure1Target = figure1.GetJoint(jointNumber);
        figure1Spear.LinkFromTo(transform, figure1Target.transform);
        figure1Spear.UpdateLink();
        figure1Spear.SetLength(10);
        figure1Spear.gameObject.SetActive(figure1Target.gameObject.activeInHierarchy);
    }

    public Tuple<Ray?, Ray?>[] PoseRays()
    {
        Tuple<Ray?, Ray?>[] returnList = new Tuple<Ray?, Ray?>[Enum.GetNames(typeof(Joints)).Length];

        for (int i = 0; i < returnList.Length; i++)
        {
            Ray? ray1 = null;
            Ray? ray2 = null;
            Polygon figure0PoseMarker = figure0.GetJoint(i);
            if (figure0PoseMarker.gameObject.activeInHierarchy)
            {
                ray1 = new Ray(
                    transform.position,
                    Vector3.Normalize(figure0PoseMarker.transform.position - transform.position));
            }

            Polygon figure1PoseMarker = figure1.GetJoint(i);
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
        switch (role)
        {
            case 0:
                figure0.posesByFrame[currentFrameNumber] = targetedFigure.Get2DPose(imgMeta);
                figure0.Set2DPose(currentFrameNumber, imgMeta);
                figure0.SetVisible(true);

                break;
            case 1:
                figure1.posesByFrame[currentFrameNumber] = targetedFigure.Get2DPose(imgMeta);
                figure1.Set2DPose(currentFrameNumber, imgMeta);
                figure1.SetVisible(true);

                break;
            case -1:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }

        targetedFigure.SetVisible(false);
    }

    public Tuple<Figure, Figure> GetFigures()
    {
        return new Tuple<Figure, Figure>(figure0, figure1);
    }
}