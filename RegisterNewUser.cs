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
using System.Data.SqlClient;

namespace vanet_function_GC
{
    public static class RegisterNewUser
    {
        [FunctionName("RegisterNewUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("RegisterNewUser HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if(DbConnection.QueryDatabase($"SELECT username FROM userprofile WHERE username=@userName",new SqlParameter("userName", data?.username.ToString())).Count>0){
                return new ConflictResult();
            }

            try 
            {
                DbConnection.QueryDatabase("insert into userprofile(fname, lname, username, sex, bdate) values(@fname,@lname,@userName,@sex,@bdate)",
                    new SqlParameter("userName", data?.username.ToString()),
                    new SqlParameter("fname", data?.fname.ToString()),
                    new SqlParameter("lname", data?.lname.ToString()),
                    new SqlParameter("sex", data?.sex.ToString()),
                    new SqlParameter("bdate", data?.bdate.ToString()));  

                DbConnection.QueryDatabase("insert into usercar (marca, modelo, userid) values (@marca,@modelo,(SELECT userid FROM userprofile WHERE username=@userName))",
                new SqlParameter("userName", data?.username.ToString()),
                new SqlParameter("marca", data?.marca.ToString()),
                new SqlParameter("modelo", data?.modelo.ToString()));

                return new OkObjectResult(requestBody);
            }
            catch
            {
                return new BadRequestObjectResult("Something went wrong while trying to register the new user.");
            }
        }
    }
}
