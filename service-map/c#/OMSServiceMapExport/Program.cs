using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMSServiceMapExport.EF;
using OMSServiceMapExport.Model;
using OMSServiceMapExport.Model.ServiceMap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
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

        static void InitDatabase()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ServiceMapDataContext, Migrations.Configuration>());
        }

        static void Main(string[] args)
        {
            InitDatabase();

            string accessToken = Login();

            var machines = GetMachines(accessToken);
            var serviceMaps = GetServiceMaps(accessToken, machines);

            var dbContext = new ServiceMapDataContext();

            foreach (var serviceMap in serviceMaps)
            {
                if (serviceMap.Map == null)
                {
                    continue;
                }

                if (serviceMap.Map.Nodes != null)
                {
                    #region Machines
                    if (serviceMap.Map.Nodes.Machines != null)
                    {
                        Console.WriteLine("Processing & Saving Machines");
                        foreach (var machineDto in serviceMap.Map.Nodes.Machines)
                        {
                            var machine = Machine.CreateInstance(machineDto);

                            if (machine != null)
                            {
                                dbContext.Machines.AddOrUpdate(x => x.ServiceMapReferenceKey, machine);
                            }
                        }

                        dbContext.SaveChanges();
                    }
                    #endregion

                    #region Processes
                    if (serviceMap.Map.Nodes.Processes != null)
                    {
                        Console.WriteLine("Processing & Saving Processing");

                        foreach (var processDto in serviceMap.Map.Nodes.Processes)
                        {
                            var process = MachineProcess.CreateInstance(dbContext, processDto);

                            if (process != null)
                            {
                                dbContext.MachineProcesses.AddOrUpdate(x => x.ServiceMapReferenceKey, process);
                            }
                        }

                        dbContext.SaveChanges();
                    }
                    #endregion

                    #region Ports
                    if (serviceMap.Map.Nodes.Ports != null)
                    {
                        Console.WriteLine("Processing & Saving Ports");

                        foreach (var portDto in serviceMap.Map.Nodes.Ports)
                        {
                            var port = MachinePort.CreateInstance(dbContext, portDto);

                            if (port != null)
                            {
                                dbContext.MachinePorts.AddOrUpdate(x => x.ServiceMapReferenceKey, port);
                            }
                        }

                        dbContext.SaveChanges();
                    }
                    #endregion

                    #region Inbound Traffic
                    if (serviceMap.Map.Edges.Acceptors != null)
                    {
                        Console.WriteLine("Processing & Saving Inbound Traffic");

                        foreach (var inboundDto in serviceMap.Map.Edges.Acceptors)
                        {
                            var inbound = MachineInboundConnection.CreateInstance(dbContext, inboundDto);

                            if (inbound != null)
                            {
                                dbContext.MachineInboundConnections.AddOrUpdate(x => x.ServiceMapReferenceKey, inbound);
                            }
                        }

                        dbContext.SaveChanges();
                    }
                    #endregion

                    #region Outbound Traffic
                    if (serviceMap.Map.Edges.Connections != null)
                    {
                        Console.WriteLine("Processing & Saving Outbound Traffic");

                        foreach (var outboundDto in serviceMap.Map.Edges.Connections)
                        {
                            var outbound = MachineOutboundConnection.CreateInstance(dbContext, outboundDto);

                            if (outbound != null)
                            {
                                dbContext.MachineOutboundConnections.AddOrUpdate(x => x.ServiceMapReferenceKey, outbound);
                            }
                        }

                        dbContext.SaveChanges();
                    }
                    #endregion
                }
            }

            Console.WriteLine("All Done");
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
            Console.WriteLine("Retrieving Machines from Service Map");

            var client = GetServiceMapClient(accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var result = client.GetAsync($"machines?api-version={SERVICE_MAP_API_VERSION}");
            var content = result.Result.Content.ReadAsStringAsync();

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            return JsonConvert.DeserializeObject(content.Result, jsonSerializerSettings) as JObject;
        }

        static List<RootDTO> GetServiceMaps(string accessToken, JObject machines)
        {
            Console.WriteLine("Generating Service Map");

            var client = GetServiceMapClient(accessToken);
            var serviceMaps = new List<RootDTO>();

            foreach (var machine in machines)
            {
                if (machine.Value.First == null)
                {
                    continue;
                }

                Console.WriteLine($"\t{machine.Value.First.SelectToken("properties.computerName").Value<string>()}");

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

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                var response = JsonConvert.DeserializeObject<RootDTO>(content.Result, jsonSerializerSettings);
                serviceMaps.Add(response);
            }

            return serviceMaps;
        }
    }
}
