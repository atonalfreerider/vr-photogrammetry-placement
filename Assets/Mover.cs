using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTKLite.Controllers;

[RequireComponent(typeof(ControllerEvents))]
public class Mover : MonoBehaviour
{
    ControllerEvents controllerEvents;

    void Awake()
    {
        controllerEvents = GetComponent<ControllerEvents>();
        controllerEvents.TriggerPressed += Grab;
        controllerEvents.TriggerReleased += Release;
        controllerEvents.RightButtonPressed += Main.Instance.Advance;
        controllerEvents.LeftButtonPressed += Main.Instance.Reverse;
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

    void Grab()
    {
        if (current.Any())
        {
            child = current.Values.First();
            child.transform.parent.SetParent(transform);
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
    }
}