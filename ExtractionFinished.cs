using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace ExtractorOrchestrator
{
    public static class ExtractionFinished
    {
        [FunctionName("ExtractionFinishedTrigger")]
        public static async Task Run(
            [QueueTrigger("extractionfinished")] string instanceId,
            [OrchestrationClient] DurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(instanceId, "Extractor_Finished", true);
        }
    }
}