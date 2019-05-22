using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ExtractorOrchestrator
{
    public static class Orchestrator
    {

        [FunctionName("Orchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
           [OrchestrationClient]DurableOrchestrationClient starter,
           ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Orchestrator", await req.Content.ReadAsStringAsync());

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            var postBody = context.GetInput<string>();

            //This should return url for the api
            var ipAddress = await context.CallActivityAsync<string>("Orchestrator_Create_ACI_Group", ("customer1",context.InstanceId));           
            // This activity function calls into the container. scenarios could be check some status, or do something specifically by calling out api endpoint 
            await context.CallActivityAsync<string>("Orchestrator_Call_Into_Container", (ipAddress,postBody));
            //Return Boolean, this will be invoked from the ACI container once its done with its job
            await context.WaitForExternalEvent("Job_Finished");
            //This activition function delete the ACI group once its done with its job
            await context.CallActivityAsync<string>("Orchestrator_Delete_ACI_Group", "customer1");
            

            // Replace "hello" with the name of your Durable Activity Function.

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Orchestrator_Create_ACI_Group")]
        public static string CreateAciGroup([ActivityTrigger] Tuple<string,string> args, ILogger log)
        {
            var creds = new AzureCredentialsFactory().FromServicePrincipal(Environment.GetEnvironmentVariable("client"), Environment.GetEnvironmentVariable("key"), Environment.GetEnvironmentVariable("tenant"), AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Authenticate(creds).WithSubscription(Environment.GetEnvironmentVariable("subscriptionId"));

            return CreateContainerGroup(azure, "azure-poc-rg", "extractor"+args.Item1, Environment.GetEnvironmentVariable("d"), args.Item2);
        }

        /// <summary>
        /// Creates a container group with a single container.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group in which to create the container group.</param>
        /// <param name="containerGroupName">The name of the container group to create.</param>
        /// <param name="containerImage">The container image name and tag, for example 'microsoft\aci-helloworld:latest'.</param>
        /// <param name="instanceId"></param>
        private static string CreateContainerGroup(IAzure azure,
            string resourceGroupName,
            string containerGroupName,
            string containerImage,
            string instanceId)
        {
            Console.WriteLine($"\nCreating container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;

            // Create the container group
            var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithWindows()
                .WithPrivateImageRegistry(Environment.GetEnvironmentVariable("a"), Environment.GetEnvironmentVariable("b"), Environment.GetEnvironmentVariable("c"))
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName)
                .WithImage(containerImage)
                .WithExternalTcpPort(20008)
                .WithCpuCoreCount(1.0)
                .WithMemorySizeInGB(3)
                .WithEnvironmentVariable("instance", instanceId)
                .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();

            Console.WriteLine($"Once DNS has propagated, container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
            return containerGroup.IPAddress;
        }

        [FunctionName("Orchestrator_Call_Into_Container")]
        public static string StartExtraction([ActivityTrigger] Tuple<string,string> args, ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(args.Item2);

                client.PostAsync(args.Item1, content);
            }
        

            return $"Hello !";
        }

        [FunctionName("Orchestrator_Delete_ACI_Group")]
        public static string SayHello3([ActivityTrigger] string name, ILogger log)
        {
            var creds = new AzureCredentialsFactory().FromServicePrincipal(Environment.GetEnvironmentVariable("client"), Environment.GetEnvironmentVariable("key"), Environment.GetEnvironmentVariable("tenant"), AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Authenticate(creds).WithSubscription(Environment.GetEnvironmentVariable("subscriptionId"));
            DeleteContainerGroup(azure, "azure-poc-rg", "extractor" + name);

            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        /// <summary>
        /// Deletes the specified container group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the container group.</param>
        /// <param name="containerGroupName">The name of the container group to delete.</param>
        private static void DeleteContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName)
        {
            IContainerGroup containerGroup = null;

            while (containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine($"Deleting container group '{containerGroupName}'...");

            azure.ContainerGroups.DeleteById(containerGroup.Id);
        }

       
    }
}