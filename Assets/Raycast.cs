#nullable enable
using Shapes.Lines;
using UnityEngine;

public class Raycast : MonoBehaviour
{
    void Start()
    {
        StaticLink pointer = Instantiate(StaticLink.prototypeStaticLink);
        pointer.gameObject.SetActive(true);
        pointer.transform.SetParent(transform, false);
        pointer.DrawFromTo(transform.position, transform.forward * 10);
    }

    readonly Plane floorPlane = new(Vector3.up, Vector3.zero);

    public Vector3? CastRayToFloor() =>
        // Don't raycast to the floor if we don't allow the user full use of raycasting.
        RayPlaneIntersection(new Ray(transform.position, transform.forward), floorPlane);
    

    static Vector3? RayPlaneIntersection(Ray ray, Plane plane)
    {
        if (!plane.Raycast(ray, out float distanceAlongRay)) return null;

        Vector3 collisionPoint = ray.GetPoint(distanceAlongRay);
        
        return collisionPoint;
    }
}