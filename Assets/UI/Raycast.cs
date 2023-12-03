#nullable enable
using Shapes.Lines;
using UnityEngine;

namespace UI
{
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
    
        public RaycastHit? CastRay()
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit))
            {
                return hit;
            }

            return null;
        }

        static Vector3? RayPlaneIntersection(Ray ray, Plane plane)
        {
            if (!plane.Raycast(ray, out float distanceAlongRay)) return null;

            Vector3 collisionPoint = ray.GetPoint(distanceAlongRay);
        
            return collisionPoint;
        }
    
        public static Vector3? PlaneIntersection(Plane plane, Ray ray)
        {
            if (!plane.Raycast(ray, out float distanceAlongRay)) return null;

            Vector3 collisionPoint = ray.GetPoint(distanceAlongRay);
        
            return collisionPoint;
        }
    }
}