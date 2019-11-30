using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using vanet_function_GC.Utilities;
using System.Collections.Generic;
using System.Data;
using vanet_function_GC.GeoLocation;

namespace vanet_function_GC
{
    public static class GetCollisionHotSpots
    {
        [FunctionName("GetCollisionHotSpots")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            DataRowCollection collisionPoints;
            List<GpsPoint> collisionHotsSpots = new List<GpsPoint>();

            log.LogInformation("Get Collision HotSpots HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            //Comprobamos que el usuario existe en primer lugar.
            if(DbConnection.QueryDatabase($"SELECT username FROM userprofile WHERE username='{data?.username}'").Count<=0){
                return new BadRequestObjectResult("User must be registered first to use this function.");
            }

            try 
            {
                collisionPoints = DbConnection.QueryDatabase($@"SELECT latitude,longitude from userevents GROUP BY latitude,longitude HAVING COUNT(*) >= {Environment.GetEnvironmentVariable("collisionCount")};");
            }
            catch
            {
                return new BadRequestObjectResult("Something went wrong while trying to get collision hotspots.");
            }

            foreach (DataRow row in collisionPoints)
            {
                collisionHotsSpots.Add(new GpsPoint(Convert.ToDouble(row["latitude"]),Convert.ToDouble(row["longitude"])));
            }
            
            if(collisionHotsSpots.Count>0)
            {
                return new OkObjectResult(JsonConvert.SerializeObject(new GetVanetEventsResponse(collisionHotsSpots,"Collission hotspots detected.")));
            }
            else
            {
                return new OkObjectResult(JsonConvert.SerializeObject(new GetVanetEventsResponse(collisionHotsSpots,"No collission hotspots detected.")));
            }
        }
    }
}
