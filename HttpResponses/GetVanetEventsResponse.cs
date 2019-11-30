using vanet_function_GC.GeoLocation;
using System.Collections.Generic;

namespace vanet_function_GC
{
    public class GetVanetEventsResponse
    {
        public List<GpsPoint> collisionPoints { get; set; }
        public string status { get; set; }

        public GetVanetEventsResponse(List<GpsPoint> collisionList, string status)
        {
            this.collisionPoints = collisionList;
            this.status = status;
        }
        public GetVanetEventsResponse() { }
    }
}
