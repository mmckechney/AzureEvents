## Subscribe to Azure health event notifications
This application takes advantage of the Azure REST interface and Azure SDK to simplify subscription to resource (or resource group) specific Azure health events. 
In order to take advantage of this tool, you will need to have access to an Azure Service Principal account. If you do not have access to one, please see the section below on creating one.



###Usage
```
AzureEvents.exe --subscriptionId <Subscription Guid> --resourceName <Azure Resource Name> --jsonConfig <path to Json config file>
```

###Arguments

`--subscriptionId`: The Guid value of the subscription that contains the targeted resource

`--resourceName`:  The name given the target resource

`--password`: Password for the Azure Service Principal account

`--jsonConfig`: Path to a JSON configuration file containing default settings. This is a convenience argument that can be used when creating alerts across multiple resources. See section below for format of the file

`--event`: The Azure health event to subscribe to. The accepted values (via comma-delimited list) are: New,Update,Resolved. This value can be defaulted via the Json config

`--email`: The email addressed to sent the health alerts to. Separate multiple values with commas. This value can be defaulted via the Json config

`--directory`: Name of the AAD directory to use. This is the part before the 'onmicrosoft.com'. This value can be defaulted via the Json config

`--resourceLocation`: The Azure region the resource is located in. This value can be defaulted via the Json config

 `--applicationId`: The Guid value for the service account used (see "Create an Azure Service Principal" below). This value can be defaulted via the Json config

## Create JSON configuration file

This step is optional, but will make running the application easier as it will default several command line arguments to standard values and eliminate the necessity of repeating them. 

An example JSON file is below. Copy the template and replace the values with your own. Then just save the file and use the path to this new file as your `--jsonConfig` argument value. If there are any arguments that are specified in both the JSON file and at the command line, the command line arguments will take precedence. 
```
{
  "applicationId": "<guid for service principal account>",
  "directoryName": "<AAD directory name>",
  "defaultResourceLocation": "<Azure resource location>",
  "emails": [
    { "address": "<email 1>" },
    { "address": "<email 2>" }
  ],
  "defaultEvents": [
    { "event": "New" },
    { "event": "Update" },
    { "event": "Resolved" }
  ]
}
```

## Create an Azure Service Principal account

In order to run the AzureEvents.exe application, an Azure Service   rincipal account must be created. If you do not have access to an existing account that has priviliges to manage alerts, use the following PowerShell commands to create one. 


1. Open a PowerShell window and login to Azure resource manager with: `Login-AzureRmAccount`.
2. In the output window, if you have more than one TenantId value, you will need to set the context to the proper directory using the command `Set-AzureRmContext`, specifying the correct tenant via the `-TenantId`, `-SubscriptionId` or `-SubscriptionName` values.
3. Now that your PowerShell context is set, you can  use the following sequence of commands to create the Service Principal and give it permissions to manage alerts. 
The 4 variables that need to be set are:
* `$password` - This is the password value that will be used when running the AzureEvents.exe console application
* `$appaccountname` - The name that you will give to the application. Although, when running the console app, you will need the “applicationid” Guid value, which will be  retrieved later
* `$homepage` - You can make up whatever address you want; it just needs to be a properly formatted URL. It does not need to be an actual site. 
* `$uri` - Also a properly formatted URL that can be made up.

 
4. Run the following script, replacing your values for the four variables
```
$password=""
$appaccountname="AzureAlertServiceApp"
$homepage=""
$uri=""
$azureAdApplication = New-AzureRmADApplication -DisplayName $appaccountname -HomePage $homepage  -IdentifierUris $uri -Password $password
Write-Output $azureAdApplication
New-AzureRmADServicePrincipal -ApplicationId $azureAdApplication.ApplicationId 
Start-Sleep-Seconds 10  #needed to make sure the principal is properly created. If you get an error on the next command, just run this last line again
New-AzureRmRoleAssignment -RoleDefinitionName "Application Insights Component Contributor" -ServicePrincipalName $azureAdApplication.ApplicationId 
```

In the console output will be a Guid value for applicationId. You will need to record this value and use it as the `--applicationId` command line value or add it to the `applicationId` JSON value.

 

###Acknowledgments
Special thanks to Matt Loflin for the great code and descriptions in his articles:

[*How to Setup Email Alerts for Azure Service Health Events*](https://code.msdn.microsoft.com/How-To-Setup-Email-Alerts-c26cdc55)

[*How to Retrieve Azure Service Health Event Logs*](https://code.msdn.microsoft.com/How-To-Programmatically-49df487d)