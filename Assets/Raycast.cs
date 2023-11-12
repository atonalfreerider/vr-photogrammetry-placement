#nullable enable
using UnityEngine;

public class Raycast : MonoBehaviour
{

    Plane FloorPlane = new(Vector3.up, Vector3.zero);

    public Vector3? CastRayToFloor() =>
        // Don't raycast to the floor if we don't allow the user full use of raycasting.
        RayPlaneIntersection(new Ray(transform.position, transform.forward), FloorPlane);
    

    static Vector3? RayPlaneIntersection(Ray ray, Plane plane)
    {
        if (!plane.Raycast(ray, out float distanceAlongRay)) return null;

        Vector3 collisionPoint = ray.GetPoint(distanceAlongRay);
        
        return collisionPoint;
    }
}