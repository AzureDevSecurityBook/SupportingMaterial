# Designing and Developing Secure Microsoft Azure Solutions

## Chapter 6 - Monitoring and Auditing

The current folder contains two demos for Chapter 6. This document describes how you can use them.

There are two different projects:

- YAFancyWebApp, an intentionally vulnerable application.
- YAFancyWebAppFixed, the same application, with a fix and most importantly with the code to raise custom events to be stored in Azure Log Analytics.

### Preparation

The same preparation procedure works for both projects.

1. Ensure you do have an Azure Subscription where you have enough rights to deploy services.
2. Create a Resource Group. For our purposes, we call it **AzureSecurityBook**.
3. Create a Log Analytics Workspace in AzureSecurityBook. From now on we'll call it **MonitoringTest**. 
4. Copy the Workspace ID and the Primary key from the Agents Management settings page of MonitoringTest.
5. Create an App Service in AzureSecurityBook. We'll name it **ASBWebApp** for our convenience from now on. Consider that the solution is based on .NET 6. It also requires a System-assigned Managed Identity. Everything else is by default.
6. Create a Key Vault in AzureSecurityBook. You can pick any name, but for our purposes it will be **ASBMonitoringKV** from now on. Configure it to use Azure RBAC instead of Access Policies.
7. Create in ASBMonitoringKV two secrets: WorkspaceId containing the Workspace ID of MonitoringTest, and WorkspaceKey containing its Primary key. Consider that this step may require you to assign yourself the role of Key Vault Administrator or, better, Key Vault Secrets Officer.
8. Assign Key Vault Secrets User role to the Managed Account assigned to the Web Application over ASBMonitoringKV.
9. In the Configuration settings of ASBWebApp, create a new application setting named KeyVaultUri, with the URL of ASBMonitoringKV, which can be retrieved from its Overview page.

### Execution of the Web Applications

The execution requires the deployment of the selected web application to the ASBWebApp App Service. You can use the same App Service for both. 