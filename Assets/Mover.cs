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

        raycast = new GameObject("raycast").AddComponent<Raycast>();
        raycast.transform.SetParent(transform, false);
    }

    readonly Dictionary<string, Collider> current = new();
    Collider child;

    void OnTriggerEnter(Collider other)
    {
        if (!current.ContainsKey(other.name))
        {
            current.Add(other.name, other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (current.ContainsKey(other.name))
        {
            current.Remove(other.name);
        }
    }

    bool isDragging = false;
    Vector3 hitPointOrigin = Vector3.zero;

    void Grab()
    {
        if (current.Any())
        {
            child = current.Values.First();
            child.transform.parent.SetParent(transform);
        }
        else
        {
            Vector3? collisionPt = raycast.CastRayToFloor();
            if (collisionPt != null)
            {
                isDragging = true;
            }
        }
    }

    void Update()
    {
        if (isDragging)
        {
            
             Vector3? floorHit = raycast.CastRayToFloor();
             if (floorHit != null)
             {
                 Main.Instance.transform.localPosition = floorHit.Value;
             }
        }
    }

    void Release()
    {
        if (child != null)
        {
            child.transform.parent.SetParent(Main.Instance.transform);
            child = null;
        }

        current.Clear();

        if (isDragging)
        {
            isDragging = false;
        }
    }
}