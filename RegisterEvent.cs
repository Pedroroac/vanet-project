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

namespace vanet_function_GC
{
    public static class RegisterEvent
    {
        [FunctionName("RegisterEvent")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Register New Event HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            //Comprobamos que el usuario existe en primer lugar.
            if(DbConnection.QueryDatabase($"SELECT username FROM userprofile WHERE username='{data?.username}'").Count<=0){
                return new BadRequestObjectResult("User must be registered first to use this function.");
            }

            try 
            {
                DbConnection.QueryDatabase($@"insert into userevents(eventTime, locationName, latitude, longitude, userid) 
                values({data?.eventTime},'{data?.locationName}',{data?.latitude},{data?.longitude},(SELECT userid FROM userprofile WHERE username='{data?.username}'))");
                
                return new OkObjectResult(requestBody);
            }
            catch
            {
                return new BadRequestObjectResult("Something went wrong while trying to register the new event.");
            }
        }
    }
}
