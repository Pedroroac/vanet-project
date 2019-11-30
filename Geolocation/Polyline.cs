using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace vanet_function_GC.GeoLocation
{
    public static class Polyline
    {
        public static List<GpsPoint> DecodePolylinePoints(string encodedPoints) 
            {
                if (encodedPoints == null || encodedPoints == "") return null;
                List<GpsPoint> poly = new List<GpsPoint>();
                char[] polylinechars = encodedPoints.ToCharArray();
                int index = 0;
                int currentLat = 0;
                int currentLng = 0;
                int next5bits;
                int sum;
                int shifter;
               
                try
                {
                    while (index < polylinechars.Length)
                    {
                        // calculate next latitude
                        sum = 0;
                        shifter = 0;
                        do
                        {
                            next5bits = (int)polylinechars[index++] - 63;
                            sum |= (next5bits & 31) << shifter;
                            shifter += 5;
                        } while (next5bits >= 32 && index < polylinechars.Length);

                        if (index >= polylinechars.Length)
                            break;

                        currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                        //calculate next longitude
                        sum = 0;
                        shifter = 0;
                        do
                        {
                            next5bits = (int)polylinechars[index++] - 63;
                            sum |= (next5bits & 31) << shifter;
                            shifter += 5;
                        } while (next5bits >= 32 && index < polylinechars.Length);

                        if (index >= polylinechars.Length && next5bits >= 32)
                            break;

                        currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                        GpsPoint p = new GpsPoint();
                        p.Latitude = Convert.ToDouble(currentLat) / 100000.0;
                        p.Longitude = Convert.ToDouble(currentLng) / 100000.0;
                        poly.Add(p);
                    } 
                }
                catch (Exception ex)
                {
                    var test = ex;
                }
                return poly;
           }
        public static bool isLocationOnEdge(List<GpsPoint> path, GpsPoint point, int tolerance = 2)
        {
            var C = new GpsPoint(point.Latitude, point.Longitude);
            for (int i = 0; i < path.Count - 1; i++)
            {
                var A = new GpsPoint(path[i].Latitude, path[i].Longitude);
                var B = new GpsPoint(path[i + 1].Latitude, path[i + 1].Longitude);
                if (Math.Round(A.GetDistanceTo(C) + B.GetDistanceTo(C), tolerance) == Math.Round(A.GetDistanceTo(B), tolerance))
                {
                    return true;
                }
            }
            return false;
        }
        public static GpsPoint getCollisionPoint(dynamic data,dynamic externalRoute)
        {
            for (int i = 0; i < externalRoute?.routes[0].legs[0].steps.Count; i++)
            {
                for (int t = 0; t < data?.route.routes[0].legs[0].steps.Count; t++)
                {                            
                    List<GpsPoint> polylineExtList = Polyline.DecodePolylinePoints(externalRoute?.routes[0].legs[0].steps[i].polyline.points.ToString());
                    List<GpsPoint> polylineUserList = Polyline.DecodePolylinePoints(data?.route.routes[0].legs[0].steps[t].polyline.points.ToString());
                    for (int n = 0; n < polylineUserList.Count - 1; n++)
                    {
                        if(Polyline.isLocationOnEdge(polylineExtList,polylineUserList[n])){
                            return polylineUserList[n];
                        }
                    }       
                }
            }
            return null;           
        }
    }
}
