# Server Application to Web API Authentication
This solution can be used to provide an example for Server Application to Web API Azure Active Directory authentication.

We can then use this as a basis for automating the interaction to Azure Data Catalog, so that users do not have to interact for either
data discovery or data asset registration. 
The code has been created to be referenced for the following blog post:
http://blogs.adatis.co.uk/ustoldfield/post/Azure-Active-Directory-Authentication-and-Azure-Data-Catalog

## Prerequisites
In order to use the code, you'll need your Azure tenant id; a registered application and its application client id; and application key.

To get register an application in Azure Active Directory, follow the steps here: https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-integrating-applications

To get your Azure tenant id, open up PowerShell and run **Login-AzureRmAccount**. Login to Azure using an account that is associated with the domain tenancy. The return from the Login process includes the tenant id. 
