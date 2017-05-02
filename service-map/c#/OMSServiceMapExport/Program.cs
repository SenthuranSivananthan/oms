using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace OMSServiceMapExport
{
    class Program
    {
        static string SUBSCRIPTION_ID = "SUBSCRIPTION-GUID";

        static string TENANT_ID = "AZURE-AD-GUID";
        static string CLIENT_ID = "APPLICATION-ID-GUID";
        static string CLIENT_SECRET = "APPLICATION-SECRET";

        static string MANAGEMENT_URL_REST = "https://management.azure.com";
        static string SERVICE_MAP_API_VERSION = "2015-11-01-preview";

        static string OMS_WORKSPACE_RESOURCE_GROUP = "";
        static string OMS_WORKSPACE_NAME = "";

        static void Main(string[] args)
        {
            string accessToken = Login();
            JObject machines = GetMachines(accessToken);
            List<JObject> serviceMaps = GetServiceMaps(accessToken, machines);

            Console.ReadLine();
        }

        static string Login()
        {
            var authenticationContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", TENANT_ID));
            var credential = new ClientCredential(clientId: CLIENT_ID, clientSecret: CLIENT_SECRET);

            var result = authenticationContext.AcquireTokenAsync(resource: "https://management.core.windows.net/", clientCredential: credential);
            return result.Result.AccessToken;
        }

        static HttpClient GetServiceMapClient(string accessToken)
        {
            string url = string.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.OperationalInsights/workspaces/{3}/features/serviceMap/",
                                MANAGEMENT_URL_REST, SUBSCRIPTION_ID, OMS_WORKSPACE_RESOURCE_GROUP, OMS_WORKSPACE_NAME);

            var client = new HttpClient()
            {
                BaseAddress = new Uri(url)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));
            return client;
        }

        static JObject GetMachines(string accessToken)
        {
            var client = GetServiceMapClient(accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var result = client.GetAsync(string.Format("machines?api-version={0}", SERVICE_MAP_API_VERSION));
            var content = result.Result.Content.ReadAsStringAsync();
            var machines = JsonConvert.DeserializeObject(content.Result) as JObject;
            
            return machines;
        }

        static List<JObject> GetServiceMaps(string accessToken, JObject machines)
        {
            var client = GetServiceMapClient(accessToken);
            var serviceMaps = new List<JObject>();
            
            foreach (var machine in machines)
            {
                var machineId = machine.Value.First.SelectToken("id").Value<string>();

                var postContent = new StringContent(
                        JsonConvert.SerializeObject(new
                            {
                                kind = "map:single-machine-dependency",
                                machineId = machineId
                            }),
                        Encoding.UTF8,
                        "application/json");

                var result = client.PostAsync(string.Format("generateMap?api-version={0}", SERVICE_MAP_API_VERSION), postContent);
                var content = result.Result.Content.ReadAsStringAsync();

                serviceMaps.Add(JsonConvert.DeserializeObject(content.Result) as JObject);
            }

            return serviceMaps;
        }
    }
}
