using Microsoft.Azure;
using Microsoft.Azure.Management.Insights;
using Microsoft.Azure.Management.Insights.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureEvents
{
    /// <summary>
    /// Derived from original work of Matt Loflin (https://code.msdn.microsoft.com/How-To-Setup-Email-Alerts-c26cdc55)
    /// </summary>
    class AzureHealthAlerts
    {
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            var cmdLine = Arguments.ParseAndValidateArguments(args);
            if(cmdLine == null)
            {
                Environment.Exit(0);
            }
           
            if(cmdLine.Errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                cmdLine.Errors.ForEach(e => Console.WriteLine(e));
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }

            try
            {
                //Get the TenantID based off of the AD directory name

                string tenantId = Utility.GetDirectoryTenantId(cmdLine.Directory);
                if(tenantId.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Unable to get Tenant ID from provided directory name. Please double check that your directory name is correct and try again");
                    Console.ForegroundColor = ConsoleColor.White;
                    Environment.Exit(2);
                }
                //Get Credentials        
                string token = await Utility.GetAuthorizationToken(tenantId, cmdLine.ApplicationId, cmdLine.Password);

                TokenCloudCredentials credentials = new TokenCloudCredentials(cmdLine.SubscriptionId, token);

                //Create an instance of the InsightsManagementClient from Microsoft.Azure.Insights  
                InsightsManagementClient managementClient = new InsightsManagementClient(credentials);


                //Get the rules based on the commandline selections
                List<Rule> resourceAlertRules = new List<Rule>();
                foreach (var type in cmdLine.EventStatus)
                {
                    resourceAlertRules.Add(GetAlert(type));
                }


                //Define the RuleAction   
                RuleAction resourceAlertEmailRuleAction = new RuleEmailAction
                {
                    SendToServiceOwners = false,
                    CustomEmails = cmdLine.Emails
                };

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Create alert rules and notifications");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ConfigurePaddingAndHeader(cmdLine, resourceAlertRules));
                foreach (var alertRule in resourceAlertRules)
                {
                    //Associate the RuleAction with the Rule  
                    alertRule.Actions.Add(resourceAlertEmailRuleAction);
                    alertRule.LastUpdatedTime = DateTime.UtcNow;

                    //Commit the Alert configuration   
                    AzureOperationResponse alertResponse = managementClient.AlertOperations.CreateOrUpdateRule(cmdLine.ResourceName,
                        new RuleCreateOrUpdateParameters()
                        {
                            Location = cmdLine.ResourceLocation,
                            Properties = alertRule
                        });


                    //Output the results
                    Console.WriteLine(ConfigureWriteOutput(alertRule, alertResponse, cmdLine.ResourceName));
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Processing complete!");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch(Exception exe)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Whoops! Something went wrong...");
                Console.WriteLine(exe.Message);
                Console.ForegroundColor = ConsoleColor.White;
                return -1;
            }
            return 0;
        }
       

        private static Rule GetAlert(EventStatusType type)
        {
            switch(type)
            {
                case EventStatusType.Update:
                    Rule resourceAlertRule = new Rule
                    {
                        Name = "Update_Incident_Alert",
                        Description = "This alert will be triggered when an Azure Incident is updated",
                        IsEnabled = true,
                        Condition = new ManagementEventRuleCondition
                        {
                            Aggregation = null,
                            DataSource = new RuleManagementEventDataSource
                            {
                                ResourceProviderName = "Azure.Health",
                                Status = "InProgress"
                            }
                        }
                    };
                    return resourceAlertRule;
                case EventStatusType.Resolved:
                    Rule resourceResolvedRule = new Rule
                    {
                        Name = "Resolved_Incident_Alert",
                        Description = "This alert will be triggered when an Azure Incident is resolved",
                        IsEnabled = true,
                        Condition = new ManagementEventRuleCondition
                        {
                            Aggregation = null,
                            DataSource = new RuleManagementEventDataSource
                            {
                                ResourceProviderName = "Azure.Health",
                                Status = "Resolved"
                            }
                        }
                    };
                    return resourceResolvedRule;
                case EventStatusType.New:
                default:
                    Rule resourceNewRule = new Rule
                    {
                        Name = "Active_Incident_Alert",
                        Description = "This alert will be triggered when an Azure Incident is published",
                        IsEnabled = true,
                        Condition = new ManagementEventRuleCondition
                        {
                            Aggregation = null,
                            DataSource = new RuleManagementEventDataSource
                            {
                                ResourceProviderName = "Azure.Health",
                                Status = "Active"
                            }
                        }
                    };

                    return resourceNewRule;

            }
           
        }

        static int eventTypePad = 4;
        static int resourceNamePad = 2;
        static int ruleNamePad = 2;
        static int emailPad = 2;
        static string consoleTableFormat = string.Empty;
        private static string ConfigurePaddingAndHeader(Arguments args, List<Rule> rules)
        {
            emailPad +=  args.Emails.Select(x => x.Length).Max();
            eventTypePad += args.EventStatus.Select(x => x.ToString().Length).Max();
            resourceNamePad += args.ResourceName.Length;
            ruleNamePad += rules.Select(x => x.Name.Length).Max();
            consoleTableFormat = "{0,-" + ruleNamePad + "}{1,-" + resourceNamePad + "}{2,-" + eventTypePad + "}{3,-" + emailPad + "}{4,-8}";

            string header = string.Format(consoleTableFormat, "Rule Name", "Resource", "Event Type", "Email", "Response") + "\r\n" +
                new string('-', ruleNamePad - 2) + "  " +
                new string('-', resourceNamePad - 2) + "  " +
                new string('-', eventTypePad - 2) + "  " +
                new string('-', emailPad - 2) + "  " +
                new string('-', 8);

            return header;
        }
        private static string ConfigureWriteOutput(Rule rule, AzureOperationResponse alertResponse, string resourceName)
        {
            string output = string.Empty;
            var eventType = ((RuleManagementEventDataSource)((ManagementEventRuleCondition)rule.Condition).DataSource).Status;
            if (eventType.ToLower() == "inprogress") eventType = "Update";
            var emails = rule.Actions.Select(x => ((RuleEmailAction)x).CustomEmails).FirstOrDefault();

            foreach(var email in emails)
            {
                output += string.Format(consoleTableFormat, rule.Name, resourceName, eventType, email, alertResponse.StatusCode) + "\r\n";
            }

            return output.Substring(0, output.Length - 2);
        }


    }

 
}

