using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static class RayMidpointFinder
{

    const float Tolerance = 0.0001f;
    const int MaxIterations = 1000;

    public static Vector3 FindMinimumMidpoint(List<Ray> rays)
    {
        Vector3 startPoint = AverageOrigins(rays);
        return Optimize(startPoint, rays);
    }

    static Vector3 Optimize(Vector3 startPoint, List<Ray> rays)
    {
        Vector3 currentPoint = startPoint;
        const float stepSize = 0.01f;

        for (int i = 0; i < MaxIterations; i++)
        {
            Vector3 gradient = ComputeGradient(currentPoint, rays);
            Vector3 nextPoint = currentPoint - stepSize * gradient;

            // Check if the objective function value has converged
            if (Math.Abs(ObjectiveFunction(nextPoint, rays) - ObjectiveFunction(currentPoint, rays)) < Tolerance)
                break;

            currentPoint = nextPoint;
        }

        return currentPoint;
    }

    static Vector3 ComputeGradient(Vector3 point, List<Ray> rays)
    {
        Vector3 gradient = Vector3.zero;

        foreach (Ray ray in rays)
        {
            Vector3 w = point - ray.origin;
            float dotProduct = Vector3.Dot(w, ray.direction);
            Vector3 projection = dotProduct * ray.direction;
            Vector3 diff = w - projection;

            // Calculate the gradient of the squared distance
            Vector3 grad = 2 * (diff - Vector3.Dot(diff, ray.direction) * ray.direction);

            // Add the gradient for this ray to the total gradient
            gradient += grad;
        }

        if (float.IsInfinity(gradient.x) || float.IsInfinity(gradient.y) || float.IsInfinity(gradient.z))
        {
            // blow up
            gradient = Vector3.zero;
        }

        return gradient;
    }

    static float ObjectiveFunction(Vector3 point, IEnumerable<Ray> rays)
    {
        return rays.Select(ray => DistanceFromPointToRay(point, ray))
            .Select(distance => distance * distance)
            .Sum();
    }

    public static float DistanceFromPointToRay(Vector3 point, Ray ray)
    {
        // Calculate the shortest distance from 'point' to 'ray'
        Vector3 w0 = point - ray.origin;
        float c1 = Vector3.Dot(w0, ray.direction);
        float c2 = Vector3.Dot(ray.direction, ray.direction);
        float b = c1 / c2;

        Vector3 pb = ray.origin + b * ray.direction;
        return Vector3.Distance(point, pb);
    }

    static Vector3 AverageOrigins(IReadOnlyCollection<Ray> rays)
    {
        Vector3 sum = new Vector3(0, 0, 0);
        sum = rays.Aggregate(sum, (current, ray) => current + ray.origin);

        return sum / rays.Count;
    }
}