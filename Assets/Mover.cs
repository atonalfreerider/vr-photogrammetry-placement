#nullable enable
using System.Collections.Generic;
using System.Linq;
using Shapes;
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

    readonly Dictionary<int, Collider> current = new();
    Collider? child;

    bool isDraggingFloor = false;
    Vector3 hitPointOrigin = Vector3.zero;
    bool isGrabbing = false;
    CameraSetup? currentPhoto;
    Polygon? currentMarker;
    
    public RaycastHit? CastRay() => raycast.CastRay();

    void OnTriggerEnter(Collider other)
    {
        current.TryAdd(other.GetInstanceID(), other);
    }

    void OnTriggerExit(Collider other)
    {
        if (current.ContainsKey(other.GetInstanceID()))
        {
            current.Remove(other.GetInstanceID());
        }
    }

    void Grab()
    {
        RaycastHit? hit = CastRay();
        if (current.Any())
        {
            isGrabbing = true;
            child = current.Values.First();
            switch (child)
            {
                case BoxCollider boxCollider:
                    // photo focal pull or marker 2D move
                    if (boxCollider.name.StartsWith("PHOTO:"))
                    {
                        currentPhoto = boxCollider.transform.parent.GetComponent<CameraSetup>();
                    }
                    break;
                case SphereCollider sphereCollider:
                    // move whole camera
                    sphereCollider.transform.parent.SetParent(transform);
                    break;
            }
        }
        else if (hit.HasValue)
        {
            isGrabbing = true;
            child = hit.Value.collider;
            if (child is BoxCollider boxCollider && boxCollider.GetComponent<Polygon>())
            {
                currentMarker = boxCollider.GetComponent<Polygon>();
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
                Dancer myDancer = currentMarker.MyDancer;
                CameraSetup myCameraSetup = myDancer.transform.parent.GetComponent<CameraSetup>();
                Vector3? rayPlaneIntersection = raycast.PlaneIntersection(myCameraSetup.CurrentPlane);
                if (rayPlaneIntersection.HasValue)
                {
                    Vector3 scale = myCameraSetup.PhotoScale;
                    Vector3 intersection = myDancer.transform.InverseTransformPoint(rayPlaneIntersection.Value);
                    currentMarker.transform.localPosition =
                        new Vector3(intersection.x / scale.x, 0, intersection.z / scale.z);
                }
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