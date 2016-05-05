using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using RestSharp;
using System;

namespace AzureEvents
{
    internal class Utility
    {
        public static string GetDirectoryTenantId(string directoryName)
        {
            
            RestClient client = new RestClient("https://login.windows.net");

            string resourceFormat = "{0}.onmicrosoft.com/.well-known/openid-configuration";
            var request = new RestRequest(string.Format(resourceFormat, directoryName.Trim()), Method.GET);
            var response = client.Execute(request);
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic dyn = JsonConvert.DeserializeObject(response.Content);
                string x = dyn.token_endpoint;
                string tenantId = x.Split('/')[3];
                return tenantId;
            }
            return string.Empty;
            
        }

        public static string GetAuthorizationToken(string tenantId, string clientId, string password)
        {
            try
            {
                //https://azure.microsoft.com/en-us/documentation/articles/resource-group-authenticate-service-principal/#provide-credentials-through-code-in-an-application
                //Establish context & Acquire token 
                var authenticationContext = new AuthenticationContext("https://login.windows.net/" + tenantId);
                var credential = new ClientCredential(clientId, password);
                var result = authenticationContext.AcquireToken("https://management.core.windows.net/", credential);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the token.");
                }

                return result.AccessToken;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.InnerException);
                throw;
            }
        }
       

        public static void ListSubscriptions(string accessToken)
        {
            RestClient client = new RestClient("https://management.core.windows.net");
             
            var request = new RestRequest("subscriptions", Method.GET);
            request.AddHeader("Authorization" , "Bearer " + accessToken);
            request.AddHeader("x-ms-version", "2013-08-01");
            var response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic dyn = JsonConvert.DeserializeObject(response.Content);
                string x = dyn.token_endpoint;
                string tenantId = x.Split('/')[3];
                //return tenantId;
            }
           // return string.Empty;
        }

        public static void T(SubscriptionCloudCredentials creds)
        {
           var x  = new Microsoft.WindowsAzure.Management.ManagementClient(creds);
            
            var subs = x.Subscriptions;

        }

    }
}
