using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ExtractorOrchestrator
{
    public static class Orchestrator
    {
        [FunctionName("Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            var postBody = context.GetInput<string>();

            await context.CallActivityAsync<string>("Orchestrator_Create_ACI_Group", context.InstanceId);
            await context.CallActivityAsync<string>("Orchestrator_Call_Into_Container", postBody);
            await context.WaitForExternalEvent("Extractor_Finished");
            await context.CallActivityAsync<string>("Orchestrator_Delete_ACI_Group", "");
            

            // Replace "hello" with the name of your Durable Activity Function.

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Orchestrator_Create_ACI_Group")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Orchestrator_Call_Into_Container")]
        public static string SayHello2([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Orchestrator_Delete_ACI_Group")]
        public static string SayHello3([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

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
    }
}