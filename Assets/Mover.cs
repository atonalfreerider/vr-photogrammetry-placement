#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTKLite.Controllers;

[RequireComponent(typeof(ControllerEvents))]
public class Mover : MonoBehaviour
{
    ControllerEvents controllerEvents;
    Raycast raycast;

    void Awake()
    {
        controllerEvents = GetComponent<ControllerEvents>();
        controllerEvents.TriggerPressed += Grab;
        controllerEvents.TriggerReleased += Release;
        controllerEvents.RightButtonPressed += Main.Instance.Advance;
        controllerEvents.LeftButtonPressed += Main.Instance.Reverse;
        
        controllerEvents.UpButtonPressed += Main.Instance.DrawNextSpear;
        controllerEvents.UpButtonPressed += Main.Instance.MarkPoseAsLead;
        
        controllerEvents.DownButtonPressed += Main.Instance.DrawPreviousSpear;
        controllerEvents.DownButtonPressed += Main.Instance.MarkPoseAsFollow;

        raycast = new GameObject("raycast").AddComponent<Raycast>();
        raycast.transform.SetParent(transform, false);
    }

    readonly Dictionary<string, Collider> current = new();
    Collider? child;

    bool isDraggingFloor = false;
    Vector3 hitPointOrigin = Vector3.zero;
    bool isGrabbing = false;
    CameraSetup? currentPhoto;
    BoxCollider? currentMarker;

    void OnTriggerEnter(Collider other)
    {
        current.TryAdd(other.name, other);
    }

    void OnTriggerExit(Collider other)
    {
        if (current.ContainsKey(other.name))
        {
            current.Remove(other.name);
        }
    }

    void Grab()
    {
        if (current.Any())
        {
            isGrabbing = true;
            child = current.Values.First();
            switch (child)
            {
                case BoxCollider boxCollider:
                    // photo focal pull or marker 2D move
                    if (boxCollider.transform.parent.GetComponent<CameraSetup>())
                    {
                        currentPhoto = boxCollider.transform.parent.GetComponent<CameraSetup>();
                    }
                    else
                    {
                        currentMarker = boxCollider;
                        Dancer myDancer = currentMarker.transform.parent.GetComponent<Dancer>();
                        
                    }

                    break;
                case SphereCollider sphereCollider:
                    // move whole camera
                    sphereCollider.transform.parent.SetParent(transform);
                    break;
            }
        }
        else
        {
            // drag floor
            Vector3? collisionPt = raycast.CastRayToFloor();
            if (collisionPt != null)
            {
                hitPointOrigin = collisionPt.Value - Main.Instance.transform.localPosition;
                isDraggingFloor = true;
            }
        }
    }

    void Update()
    {
        if (isGrabbing)
        {
            if (currentPhoto != null)
            {
                float d = Vector3.Dot(
                    transform.position - currentPhoto.transform.position,
                    Vector3.forward);

                currentPhoto.MovePhotoToDistance(d);
            }

            if (currentMarker != null)
            {
                // TODO move marker in XY plane
            }

            Main.Instance.UpdateCameraLinks();
        }

        if (isDraggingFloor)
        {
            Vector3? floorHit = raycast.CastRayToFloor();
            if (floorHit != null)
            {
                Main.Instance.transform.localPosition = floorHit.Value - hitPointOrigin;
            }
        }
    }

    void Release()
    {
        if (child != null)
        {
            isGrabbing = false;

            switch (child)
            {
                case BoxCollider boxCollider:
                    // photo focul pull
                    currentPhoto = null;
                    currentMarker = null;
                    Main.Instance.currentlyTargetedCameraSetup = null;
                    break;
                case SphereCollider sphereCollider:
                    // move whole camera
                    sphereCollider.transform.parent.SetParent(Main.Instance.transform);
                    break;
            }

            child = null;
        }

        current.Clear();

        if (isDraggingFloor)
        {
            isDraggingFloor = false;
            hitPointOrigin = Vector3.zero;
        }
    }
}