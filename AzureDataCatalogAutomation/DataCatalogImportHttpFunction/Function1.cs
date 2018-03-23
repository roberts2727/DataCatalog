
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json; //Install-Package Newtonsoft.Json
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory; //Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory
using Microsoft.Rest; //Install-Package Microsoft.Rest

namespace DataCatalogImportHttpFunction
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }

    public class AzureDataCatalog
    {
        private readonly string ClientId;
        //private readonly Uri RedirectUri;
        private readonly string CatalogName;

        private static AuthenticationResult authResult;
        private static readonly AuthenticationContext AuthContext = new AuthenticationContext("https://login.windows.net/common/oauth2/authorize");

        private static readonly string Username = "ujo@adatis.co.uk";
        private static readonly string Password = "Fevrier2018";
        //private static readonly string AuthorityUrl = "";
        private static readonly string ResourceUrl = "https://api.azuredatacatalog.com";

        public AzureDataCatalog()
        {
            //NOTE: You must fill in the App.Config with the following three settings. The first two are values that you received registered your application with AAD. The 3rd setting is always the same value.:
            //< ADCImportExport.Properties.Settings >
            //    <setting name = "ClientId" serializeAs = "String">
            //           <value></value>
            //       </setting>
            //       <setting name = "RedirectURI" serializeAs = "String">
            //              <value> https://login.live.com/oauth20_desktop.srf</value>
            //    </setting>
            //    <setting name = "ResourceId" serializeAs = "String">
            //           <value> https://datacatalog.azure.com</value>
            //    </setting>
            //</ADCImportExport.Properties.Settings>

            var credential = new UserPasswordCredential(Username, Password);


            ClientId = "7b55229b-785b-4bc8-a3e3-6dad9728bd94";
            //RedirectUri = new Uri(ADCImportExport.Properties.Settings.Default.RedirectURI);

            CatalogName = "DefaultCatalog";

            var authContext = new AuthenticationContext("https://login.windows.net/common/oauth2/authorize");
            authResult = authContext.AcquireTokenAsync(ResourceUrl, ClientId, credential).Result;

            var tokenCredentials = new TokenCredentials(authResult.AccessToken, "Bearer");

        }

        public string Get(string uri)
        {

            var fullUri = string.Format("{0}?api-version=2016-03-30", uri);

            string requestId = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUri);
            request.Method = "GET";

            try
            {
                string s = GetPayload(request, out requestId);
                return s;
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Status);
                Console.WriteLine("Request Id: " + requestId);

                if (ex.Response != null)
                {
                    // can use ex.Response.Status, .StatusDescription
                    if (ex.Response.ContentLength != 0)
                    {
                        using (var stream = ex.Response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                Console.WriteLine("Failed Get of asset: " + uri);
                                Console.WriteLine(reader.ReadToEnd());
                            }
                        }
                    }
                }
                return null;
            }
        }

        public string Search(string searchTerm, int startPage, int count)
        {
            var fullUri = string.Format("https://api.azuredatacatalog.com/catalogs/{0}/search/search?searchTerms={1}&count={2}&startPage={3},&api-version=2016-03-30", CatalogName, searchTerm, count, startPage);

            string requestId = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUri);
            request.Method = "GET";

            try
            {
                string s = GetPayload(request, out requestId);
                return s;
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Status);
                Console.WriteLine("Request Id: " + requestId);
                if (ex.Response != null)
                {
                    // can use ex.Response.Status, .StatusDescription
                    if (ex.Response.ContentLength != 0)
                    {
                        using (var stream = ex.Response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                Console.WriteLine(reader.ReadToEnd());
                            }
                        }
                    }
                }
                return null;
            }
        }

        public string Update(string postPayload, string viewType, out string id)
        {

            var fullUri = string.Format("https://api.azuredatacatalog.com/catalogs/{0}/views/{1}?api-version=2016-03-30", CatalogName, viewType);

            string requestId = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUri);
            request.Method = "POST";
            try
            {
                var response = SetRequestAndGetResponse(request, out requestId, postPayload);
                var responseStream = response.GetResponseStream();

                id = response.Headers["location"];

                StreamReader reader = new StreamReader(responseStream);
                return reader.ReadToEnd();
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Status);
                Console.WriteLine("Request Id: " + requestId);
                if (ex.Response != null)
                {
                    // can use ex.Response.Status, .StatusDescription
                    if (ex.Response.ContentLength != 0)
                    {
                        using (var stream = ex.Response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                Console.WriteLine("Failed Update of asset: " + postPayload.Substring(0, 50));
                                Console.WriteLine(reader.ReadToEnd());
                            }
                        }
                    }
                }
                id = null;
                return null;
            }
        }

        private static HttpWebResponse SetRequestAndGetResponse(HttpWebRequest request, out string requestId, string payload = null)
        {
            while (true)
            {
                //Add a guid to help with diagnostics
                requestId = Guid.NewGuid().ToString();
                request.Headers.Add("x-ms-client-request-id", requestId);
                //To authorize the operation call, you need an access token which is part of the Authorization header
                request.Headers.Add("Authorization", authResult.CreateAuthorizationHeader());
                //Set to false to be able to intercept redirects
                request.AllowAutoRedirect = false;

                if (!string.IsNullOrEmpty(payload))
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(payload);
                    request.ContentLength = byteArray.Length;
                    request.ContentType = "application/json";
                    //Write JSON byte[] into a Stream
                    request.GetRequestStream().Write(byteArray, 0, byteArray.Length);
                }
                else
                {
                    request.ContentLength = 0;
                }

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                // Requests to **Azure Data Catalog (ADC)** may return an HTTP 302 response to indicate
                // redirection to a different endpoint. In response to a 302, the caller must re-issue
                // the request to the URL specified by the Location response header. 
                if (response.StatusCode == HttpStatusCode.Redirect)
                {
                    string redirectedUrl = response.Headers["Location"];
                    HttpWebRequest nextRequest = WebRequest.Create(redirectedUrl) as HttpWebRequest;
                    nextRequest.Method = request.Method;
                    request = nextRequest;
                }
                else
                {
                    return response;
                }
            }
        }

        private static string GetPayload(HttpWebRequest request, out string requestId)
        {
            string result = String.Empty;
            var response = SetRequestAndGetResponse(request, out requestId);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException("Request wrong");
            var stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            result = reader.ReadToEnd();
            return result;
        }
    }

}

