using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Linq;

namespace SecurityQuiz
{
    public class Results
    {
        public List<Result> results { get; set; }
    }

    public class Result
    {
        public string priority { get; set; }
        public int reported_by { get; set; }
        public double timestamp { get; set; }
        public int employee_id { get; set; }
        public string source_ip { get; set; }
        public string machine_ip { get; set; }
        public string internal_ip { get; set; }
        public string ip { get; set; }
    }

    public class Incident
    {
        public string type { get; set; }
        public string priority { get; set; }
        public string machine_ip { get; set; }
        public double timestamp { get; set; }
    }

    public class Incidents
    {
        public int count { get; set; }

        public List<Incident> incidents { get; set; }
    }

    public class Employee
    {
        public Dictionary<string, Incidents> Data { get; set; }
    }

    public class APIIncidents
    {
        public Dictionary<int, Employee> Employees { get; set; }
    }

    public class IpAddressToIdPair
    {
        public string IPAddress { get; set; }
        public int ID { get; set; }
    }

    class Program
    {
        public static HttpListener listener;
        public static string url = "http://localhost:9000/incidents/";
        public static int requestCount = 0;

        private static string _baseUri = "https://incident-api.use1stag.elevatesecurity.io/incidents/";
        private static string _identitiesUri = "https://incident-api.use1stag.elevatesecurity.io/identities/";
        private static string _credential = "elevateinterviews:ElevateSecurityInterviews2021";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();

            //RunAsync().Wait();
        }

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                // Write the response info
                string disableSubmit = !runServer ? "disabled" : "";

                var theData = await RunAsync();

                // byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));
                byte[] data = Encoding.UTF8.GetBytes(theData);
                resp.ContentType = "application/json";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        static void parseResults(Results theResult, Dictionary<string, int> pairs, APIIncidents apiIncidents, string priorityType)
        {
            foreach (var item in theResult.results)
            {
                int employeeId = item.employee_id;
                var ip_address = String.Empty;
                if (employeeId == 0)
                {
                    if (!String.IsNullOrWhiteSpace(item.internal_ip))
                    {
                        ip_address = item.internal_ip;
                    }
                    else if (!String.IsNullOrWhiteSpace(item.machine_ip))
                    {
                        ip_address = item.machine_ip;
                    }
                    else if (!String.IsNullOrWhiteSpace(item.ip))
                    {
                        ip_address = item.ip;
                    }
                    else if (!String.IsNullOrWhiteSpace(item.source_ip))
                    {
                        ip_address = item.source_ip;
                    }
                    if (pairs.TryGetValue(ip_address, out int foundId))
                    {
                        employeeId = foundId;
                    }
                }
                if (employeeId != 0)
                {
                    var incident = new Incident()
                    {
                        machine_ip = ip_address,
                        priority = item.priority,
                        timestamp = item.timestamp,
                        type = priorityType
                    };

                    Incidents iList = null;
                    if (apiIncidents.Employees.TryGetValue(employeeId, out Employee employee))
                    {
                        if (employee.Data.TryGetValue(item.priority, out Incidents incidents))
                        {
                            iList = incidents;
                        }
                        else
                        {
                            Console.WriteLine("iList should never be null!");
                        }
                    }
                    else
                    {
                        var employee2 = new Employee()
                        {
                            Data = new Dictionary<string, Incidents>()
                        };
                        employee2.Data.Add("low", new Incidents() { incidents = new List<Incident>() });
                        employee2.Data.Add("medium", new Incidents() { incidents = new List<Incident>() });
                        employee2.Data.Add("high", new Incidents() { incidents = new List<Incident>() });
                        employee2.Data.Add("critical", new Incidents() { incidents = new List<Incident>() });
                        apiIncidents.Employees.Add(employeeId, employee2);
                        if (employee2.Data.TryGetValue(item.priority, out Incidents incidents))
                        {
                            iList = incidents;
                        }
                    }
                    if (iList != null)
                    {
                        iList.incidents.Add(incident);
                        iList.count = iList.incidents.Count;
                        if (iList.count > 1)
                        {
                            var sortedList = iList.incidents.OrderBy(x => x.timestamp).ToList();
                            iList.incidents = sortedList;
                            Console.WriteLine("multiple count sort is OK");
                        }
                    }
                    else
                    {
                        Console.WriteLine("iList should never be null!");
                    }
                }
                else
                {
                    Console.WriteLine($"employeeId not found ip={ip_address}");
                }
            }
        }

        static async Task CallAPI(string incidentType, Dictionary<string, int> pairs, APIIncidents apiIncidents)
        {
            try
            {
                var response = await MakeRequestAsync(incidentType);
                var contentString = await response.Content.ReadAsStringAsync();
                var theResult = JsonConvert.DeserializeObject<Results>(contentString);
                parseResults(theResult, pairs, apiIncidents, incidentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API processing exception={ex.Message}");
            }
        }

        static async Task<string> RunAsync()
        {
            try
            {
                HttpResponseMessage identityMapResonse = await IdentityMapAsync();
                var identityMapJson = await identityMapResonse.Content.ReadAsStringAsync();
                Dictionary<string, int> idPairs = JsonConvert.DeserializeObject<Dictionary<string, int>>(identityMapJson);

                APIIncidents apiIncidents = new APIIncidents()
                {
                    Employees = new Dictionary<int, Employee>()
                };

                await CallAPI("denial", idPairs, apiIncidents);
                await CallAPI("intrusion", idPairs, apiIncidents);
                await CallAPI("executable", idPairs, apiIncidents);
                await CallAPI("misuse", idPairs, apiIncidents);
                await CallAPI("unauthorized", idPairs, apiIncidents);
                await CallAPI("probing", idPairs, apiIncidents);
                await CallAPI("other", idPairs, apiIncidents);

                string output = JsonConvert.SerializeObject(apiIncidents.Employees);
                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //Console.WriteLine("\nPress ENTER to exit...");
            //Console.ReadLine();
            return String.Empty;
        }

        static async Task<HttpResponseMessage> MakeRequestAsync(string queryString)
        {
            var client = new HttpClient();

            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(_credential));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            var result = (await client.GetAsync(_baseUri + queryString + "/"));
            return result;
        }

        static async Task<HttpResponseMessage> IdentityMapAsync()
        {
            var client = new HttpClient();

            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(_credential));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            var result = (await client.GetAsync(_identitiesUri));
            return result;
        }

    }
}
