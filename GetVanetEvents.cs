using System;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using vanet_function_GC.GeoLocation;
using vanet_function_GC.Utilities;
using System.Net.Http;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace vanet_function_GC
{
    public static class GetVanetEvents
    {
        [FunctionName("GetVanetEvents")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            //Declaring variables.
            DataRowCollection currentRoutes;
            HttpClient client = new HttpClient();
            List<GpsPoint> collisionList = new List<GpsPoint>();
            GpsPoint currentUserSP;

            //Environment Variables
            int sysMinDistance = Convert.ToInt32(Environment.GetEnvironmentVariable("minDistanceDetection"));
            int secondsToDestination = Convert.ToInt32(Environment.GetEnvironmentVariable("secondsToDestination"));
            string googleApiKey = Environment.GetEnvironmentVariable("googleApiKey");

            log.LogInformation("GetVanetEvents HTTP trigger function processed a request.");

            //Reading the HTTP Request Body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            currentUserSP = new GpsPoint(Convert.ToDouble(data?.route.routes[0].legs[0].start_location.lat), Convert.ToDouble(data?.route.routes[0].legs[0].start_location.lng));

            try
            {
                //Comprobamos que el usuario existe en primer lugar.
                if (DbConnection.QueryDatabase($"SELECT username FROM userprofile WHERE username=@userName", new SqlParameter("userName", data?.username.ToString())).Count <= 0)
                {
                    return new BadRequestObjectResult("User must be registered first to use this function.");
                }
                //Eliminamos la información de ruta anterior, solo se almacena la mas reciente.
                DbConnection.QueryDatabase($"DELETE FROM userroutes where userid=(SELECT userid FROM userprofile WHERE username=@userName)", new SqlParameter("userName", data?.username.ToString()));

                //Almacenamos la data de la ruta del usuario.
                DbConnection.QueryDatabase($@"insert into userroutes (currentroute,speed,eventTime,userid) values (@route, @speed, @eventTime, (SELECT userid FROM userprofile WHERE username=@userName))",
                    new SqlParameter("route", data?.route.ToString()),
                    new SqlParameter("speed", data?.speed.ToString()),
                    new SqlParameter("eventTime", data?.eventime.ToString()),
                    new SqlParameter("userName", data?.username.ToString()));

                //Obtener las rutas de los demas usuarios del sistema.
                currentRoutes = DbConnection.QueryDatabase($"SELECT currentroute, speed FROM userroutes WHERE userid!=(SELECT userid FROM userprofile WHERE username=@userName);",
                new SqlParameter("userName", data?.username.ToString()));

                //Almacenamos información de coducta de el usuario.
                DbConnection.QueryDatabase($@"insert into drivebehavior (latitude,longitude,speed,eventTime,userid) 
                values (@latitude,@longitude,@speed,@eventTime,(SELECT userid FROM userprofile WHERE username=@userName))",
                    new SqlParameter("latitude", currentUserSP.Latitude.ToString()),
                    new SqlParameter("longitude", currentUserSP.Longitude.ToString()),
                    new SqlParameter("speed", data?.speed.ToString()),
                    new SqlParameter("eventTime", data?.eventime.ToString()),
                    new SqlParameter("userName", data?.username.ToString()));
            }
            catch
            {
                return new BadRequestObjectResult("Something went wrong while trying to access the database.");
            }

            // Algoritmo de colisiones
            currentUserSP = new GpsPoint(Convert.ToDouble(data?.route.routes[0].legs[0].start_location.lat), Convert.ToDouble(data?.route.routes[0].legs[0].start_location.lng));

            foreach (DataRow row in currentRoutes)
            {
                dynamic externalRoute = JsonConvert.DeserializeObject(row["currentroute"].ToString());
                GpsPoint externaltUserSP = new GpsPoint(Convert.ToDouble(externalRoute?.routes[0].legs[0].start_location.lat),
                                                    Convert.ToDouble(externalRoute?.routes[0].legs[0].start_location.lng));
                double externaltUserSpeed = Convert.ToDouble(row["speed"]);
                double distanceUsers = externaltUserSP.GetDistanceTo(currentUserSP);

                if (distanceUsers <= sysMinDistance)
                {
                    GpsPoint collisionPoint = Polyline.getCollisionPoint(data, externalRoute);
                    if (collisionPoint != null)
                    {
                        var response = await client.PostAsync($@"https://maps.googleapis.com/maps/api/distancematrix/json?origins={currentUserSP.Latitude},{currentUserSP.Longitude}&destinations={collisionPoint.Latitude},{collisionPoint.Longitude}&key={googleApiKey}", null);
                        dynamic apiResponseCU = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                        response = await client.PostAsync($@"https://maps.googleapis.com/maps/api/distancematrix/json?origins={externaltUserSP.Latitude},{externaltUserSP.Longitude}&destinations={collisionPoint.Latitude},{collisionPoint.Longitude}&key={googleApiKey}", null);
                        dynamic apiResponseEU = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                        int timeCU = apiResponseCU?.rows[0].elements[0].duration.value;
                        int timeEU = apiResponseEU?.rows[0].elements[0].duration.value;
                        if (Math.Abs(timeCU - timeEU) <= secondsToDestination)
                        {
                            collisionList.Add(collisionPoint);
                        }
                    }

                }
            }
            if (collisionList.Count > 0)
            {
                return new OkObjectResult(JsonConvert.SerializeObject(new GetVanetEventsResponse(collisionList, "Possible collissions detected.")));
            }
            else
            {
                return new OkObjectResult(JsonConvert.SerializeObject(new GetVanetEventsResponse(collisionList, "No collissions detected.")));
            }

        }
    }
}
