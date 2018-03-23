using System;
using System.Diagnostics;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory; // Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Rest;

namespace ServerAuth
{
    class Program
    {

        //TenantId and clientId as seen from Azure AAD portal, 
        //see https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal
        //on how to create secret key for an client application.
        //To authorize an application to perform as a catalog user, glossary admin, or catalog admin,
        //please add the service principal user in the format of {ClientAppId}@{TenantId} to the according list.
        private static string tenantId = "AzureActiveDirectoryTenantId";
        private static string clientId = "ApplicationId";
        private static string secret = "ApplicationKey";

        //Note: This example uses the "DefaultCatalog" keyword to update the user's default catalog.  You may alternately
        //specify the actual catalog name.
        private static string catalogName = "DefaultCatalog";

        private static string authorityUri = string.Format("https://login.windows.net/{0}", tenantId);
        private static string upn = clientId + "@" + tenantId;
        private static string registerUri = string.Format("https://api.azuredatacatalog.com/catalogs/{0}/views/tables?api-version=2016-03-30", catalogName);
        static AuthenticationResult authResult = null;
        static void Main(string[] args)
        {
            //Call the AccessToken Task
            AccessToken().Wait();
            //Create a new token from the authResult task.
            //This token can then be added to authorisation headers
            var tokenCredentials = new TokenCredentials(authResult.AccessToken, "Bearer");

            //To prove that we have authenticated properly with AAD, we can output the accesstoken to a file.
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"C:\Temp\BearerToken.txt"))
            {
                file.WriteLine(authResult.AccessToken);
            }

        }

        static async Task<AuthenticationResult> AccessToken()
        {
            if (authResult == null)
            {
                //Resource Uri for Data Catalog API
                string resourceUri = "https://api.azuredatacatalog.com";

                //A redirect uri gives AAD more details about the specific application that it will authenticate.
                //Since a client app does not have an external service to redirect to, this Uri is the standard placeholder for a client app.
                //string redirectUri = "https://login.live.com/oauth20_desktop.srf";

                //Create an instance of AuthenticationContext to acquire an Azure access token using OAuth2 authority Uri
                AuthenticationContext authContext = new AuthenticationContext(authorityUri);

                //Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
                //AcquireToken takes a Client Id that Azure AD creates when you register your client app.
                //It also uses the secret (key) that you create for the registered client app.
                authResult = await authContext.AcquireTokenAsync(resourceUri, new ClientCredential(clientId, secret));
            }

            return authResult;
        }


    }
}
