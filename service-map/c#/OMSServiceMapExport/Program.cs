using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace OMSServiceMapExport
{
    class Program
    {
        static string SUBSCRIPTION_ID = ConfigurationManager.AppSettings["SubscriptionId"];

        static string TENANT_ID = ConfigurationManager.AppSettings["AADTenantId"];
        static string CLIENT_ID = ConfigurationManager.AppSettings["AADClientId"];
        static string CLIENT_SECRET = ConfigurationManager.AppSettings["AADClientSecret"];

        static string MANAGEMENT_URL_REST = "https://management.azure.com";
        static string SERVICE_MAP_API_VERSION = "2015-11-01-preview";

        static string OMS_WORKSPACE_RESOURCE_GROUP = ConfigurationManager.AppSettings["OMSWorkspaceResourceGroup"];
        static string OMS_WORKSPACE_NAME = ConfigurationManager.AppSettings["OMSWorkspaceName"];

        static void Main(string[] args)
        {
            string accessToken = Login();
            JObject machines = GetMachines(accessToken);

            GetProcesses(accessToken, machines);
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
            string url = $"{MANAGEMENT_URL_REST}/subscriptions/{SUBSCRIPTION_ID}/resourceGroups/{OMS_WORKSPACE_RESOURCE_GROUP}/providers/Microsoft.OperationalInsights/workspaces/{OMS_WORKSPACE_NAME}/features/serviceMap/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri(url)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            return client;
        }

        static JObject GetMachines(string accessToken)
        {
            var client = GetServiceMapClient(accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var result = client.GetAsync($"machines?api-version={SERVICE_MAP_API_VERSION}");
            var content = result.Result.Content.ReadAsStringAsync();
            var machines = JsonConvert.DeserializeObject(content.Result) as JObject;
            
            return machines;
        }

        static void GetProcesses(string accessToken, JObject machines)
        {
            var client = GetServiceMapClient(accessToken);

            foreach (var machine in machines)
            {
                if (machine.Value.First == null)
                {
                    continue;
                }

                var machineId = machine.Value.First.SelectToken("id").Value<string>().Split('/').Last();
                var result = client.GetAsync($"machines/{machineId}/processes?api-version={SERVICE_MAP_API_VERSION}");
                var content = result.Result.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }
        }

        static List<JObject> GetServiceMaps(string accessToken, JObject machines)
        {
            var client = GetServiceMapClient(accessToken);
            var serviceMaps = new List<JObject>();

            foreach (var machine in machines)
            {
                if (machine.Value.First == null)
                {
                    continue;
                }

                var machineId = machine.Value.First.SelectToken("id").Value<string>();

                var postContent = new StringContent(
                        JsonConvert.SerializeObject(new
                            {
                                kind = "map:single-machine-dependency",
                                machineId = machineId
                            }),
                        Encoding.UTF8,
                        "application/json");

                var result = client.PostAsync($"generateMap?api-version={SERVICE_MAP_API_VERSION}", postContent);
                var content = result.Result.Content.ReadAsStringAsync();

                serviceMaps.Add(JsonConvert.DeserializeObject(content.Result) as JObject);
            }

            return serviceMaps;
        }
    }
}
