using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.SunMath
{
    public static class RadianceSolver
    {
        private static Dictionary<CelestialBody, double> receivedRadiance = new Dictionary<CelestialBody, double>();
        public static double GetReceivedRadiance(CelestialBody targetBody)
        {
            if (receivedRadiance.TryGetValue(targetBody, out double radiance))
            {
                return radiance;
            }
            else
            {
                radiance = ReceivedRadiancePerBody(targetBody);
                receivedRadiance[targetBody] = radiance;
                return radiance;
            }
        }

        public static double ReceivedRadiancePerBody(CelestialBody targetBody)
        {
            // Calculate the average distance from the target body to the sun
            // Solve for parent body distance to sun and add half of the target body's semi-major axis
            double distanceToSun = 0;
            CelestialBody currentBody = targetBody;
            while (!currentBody.referenceBody.referenceBody == currentBody.referenceBody)
            {
                distanceToSun += currentBody.orbit.semiMajorAxis / 2.0;
                currentBody = currentBody.referenceBody;
            }
            distanceToSun += currentBody.orbit.semiMajorAxis; // Add the parent body's contribution

            double homeDistance = FlightGlobals.GetHomeBody()?.orbit?.semiMajorAxis ?? distanceToSun;
            double ratio = homeDistance / distanceToSun;
            return ratio * ratio;
        }

        public static double ReceivedRadianceAngle(CelestialBody targetBody, double lat, double lon, double panelAngle, double panelheading)
        {
            CelestialBody sun = Planetarium.fetch.Sun;
            double time = Planetarium.GetUniversalTime();

            // Surface normal at the requested lat/lon (world-space, unit-like direction)
            Vector3d up = targetBody.GetSurfaceNVector(lat, lon).normalized;

            // World positions
            Vector3d bodyPosition = targetBody.getTruePositionAtUT(time);
            Vector3d sunPosition = sun.getTruePositionAtUT(time);

            // Approximate surface point to account for local horizon/occlusion by the body
            Vector3d surfacePosition = bodyPosition + (up * targetBody.Radius);

            // Direction from surface point to sun
            Vector3d sunDirection = (sunPosition - surfacePosition).normalized;

            // Angle relative to sun at this location (cosine form for efficiency)
            double surfaceSunCos = Vector3d.Dot(up, sunDirection);

            // Sun is below horizon for this point
            if (surfaceSunCos <= 0.0)
                return 0.0;

            // Build local tangent frame (north/east) for panel heading
            Vector3d axis = targetBody.transform.up.normalized; // body north axis
            Vector3d east = Vector3d.Cross(axis, up);

            // Fallback near poles where axis and up become parallel
            if (east.sqrMagnitude < 1e-12)
            {
                Vector3d fallback = Math.Abs(up.y) < 0.999 ? Vector3d.up : Vector3d.right;
                east = Vector3d.Cross(fallback, up);
            }

            east = east.normalized;
            Vector3d north = Vector3d.Cross(up, east).normalized;

            // Heading: 0 = north, 90 = east
            double headingRad = panelheading * (Math.PI / 180.0);
            Vector3d headingDir = (north * Math.Cos(headingRad) + east * Math.Sin(headingRad)).normalized;

            // Panel angle: 0 = straight up (normal aligned with surface normal), 90 = vertical toward horizon
            double panelAngleRad = panelAngle * (Math.PI / 180.0);
            Vector3d panelNormal = (up * Math.Cos(panelAngleRad) + headingDir * Math.Sin(panelAngleRad)).normalized;

            // Directness score (0..1)
            double panelDirectness = Math.Max(0.0, Vector3d.Dot(panelNormal, sunDirection));

            // Scale by received radiance at this body
            return GetReceivedRadiance(targetBody) * panelDirectness;
        }

        public static double ReceivedRadianceAngle(CelestialBody targetBody, double lat, double lon)
        {
            CelestialBody sun = Planetarium.fetch.Sun;
            double time = Planetarium.GetUniversalTime();

            // Surface normal at the requested lat/lon
            Vector3d up = targetBody.GetSurfaceNVector(lat, lon).normalized;

            // World positions
            Vector3d bodyPosition = targetBody.getTruePositionAtUT(time);
            Vector3d sunPosition = sun.getTruePositionAtUT(time);

            // Surface point (for local horizon/occlusion test)
            Vector3d surfacePosition = bodyPosition + (up * targetBody.Radius);

            // Direction from surface point to sun
            Vector3d sunDirection = (sunPosition - surfacePosition).normalized;

            // Terminator cutoff: sun below local horizon
            if (Vector3d.Dot(up, sunDirection) <= 0.0)
                return 0.0;

            // Dual-axis tracking: panel normal is perfectly aligned with sun direction (dot = 1)
            return GetReceivedRadiance(targetBody);
        }
    }
}
