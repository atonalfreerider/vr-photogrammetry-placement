using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTKLite.Controllers;

[RequireComponent(typeof(ControllerEvents))]
public class Mover : MonoBehaviour
{
    ControllerEvents controllerEvents;
    GameObject main;

    void Awake()
    {
        controllerEvents = GetComponent<ControllerEvents>();
        controllerEvents.TriggerPressed += Grab;
        controllerEvents.TriggerReleased += Release;
        controllerEvents.PrimaryAxisHeld += OnPrimaryAxisHeld;

        main = GameObject.Find("Main");
    }

    readonly Dictionary<string, BoxCollider> current = new();
    BoxCollider child;

    void OnTriggerEnter(Collider other)
    {
        if (other is BoxCollider boxCollider)
        {
            if (!current.ContainsKey(other.name))
            {
                current.Add(other.name, boxCollider);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other is BoxCollider boxCollider)
        {
            if (current.ContainsKey(boxCollider.name))
            {
                current.Remove(boxCollider.name);
            }
        }
    }

    void Grab()
    {
        if (current.Any())
        {
            child = current.Values.First();
            child.transform.SetParent(transform);
        }
    }

    void Release()
    {
        if (child != null)
        {
            child.transform.SetParent(main.transform);
            child = null;
        }

        current.Clear();
    }


    void OnPrimaryAxisHeld(Vector2 axisValue)
    {
        Vector3 translation = transform.forward * -axisValue.y * (2f * Time.deltaTime);

        main.transform.Translate(translation);
    }
}    
