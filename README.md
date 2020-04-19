# Serverless batch processing using Azure Durable Functions and Container Instances

This samples shows how you can orchestrate the life cycle of Azure Container Instances using Azure Durbale funcionts a in true serverless fashion. Azure Durable Functions are used to do the orchestration of the container deployment, monitoring and cleanup of Azure Container Instances. Azure Container registry stores the container images. Used Azure Active Directory and Managed Service Identity within Functions to manage the Container Instances.
