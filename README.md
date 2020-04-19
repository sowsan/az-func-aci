# Serverless batch processing using Azure Durable Functions and Container Instances

This samples shows how you can orchestrate the life cycle of Azure Container Instances using Azure Durbale funcionts a in true serverless fashion. Azure Durable Functions are used to do the orchestration of the container deployment, monitoring and cleanup of Azure Container Instances. Azure Container registry stores the container images. Used Azure Active Directory and Managed Service Identity within Functions to manage the Container Instances.

![Architecture](https://github.com/sowsan/az-func-aci/blob/master/durable_func_aci.png)
1. A HTTP Trigger will invoke the Azure Durable Function to orchestrate the container deployment.
1. First activity function will create the ACI group and with the container image stored in the Azure Container Registry. Used [Azure Fluent API](https://github.com/Azure/azure-libraries-for-net) and [ACI Management libraries](https://docs.microsoft.com/dotnet/api/overview/azure/containerinstance?view=azure-dotnet) for accomplishing this task.
1. A URL for the API from the container will be received from the above activity function and that is used to call and start the job. The same API can be used for monitoring the job progress.
1. Once the job completed successfully, the program within the container will invoke the Azure Durable Function by [raising an external event](https://docs.microsoft.com/azure/azure-functions/durable/durable-functions-external-events?tabs=csharp) and provides the status of the Job stating whether its completed or failed.
1. Container Group will be deleted or stopped depending on the status from the step #4. Please refer the options [here](https://docs.microsoft.com/azure/container-instances/container-instances-stop-start). You can also control the container instance using [restart policies](https://docs.microsoft.com/azure/container-instances/container-instances-restart-policy)
