using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureEvents
{
    class Arguments
    {
        JsonConfig cfg = new JsonConfig();
        private static char[] splitter = new char[] { ',', ';', ':', '|' };
        private List<string> errors = new List<string>();

        //Make sure this is the first as it will grab the default values first
        [CommandLine(Required = false, Priority = 1, HelpMessage = "Path to a JSON file containing the default configuration values (directory name, emails, events, etc)")]
        public string JsonConfig
        {
            set
            {
                string file = string.Empty;
                string currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (File.Exists(value))
                {
                    file = value;
                }else if(File.Exists(Path.Combine(currentDir,value)))
                {
                    file = Path.Combine(currentDir, value);
                }
                if(file.Length > 0)
                {
                     cfg = JsonSerializer.Deserialize<JsonConfig>(File.ReadAllText(value));

                    if (cfg != null && cfg is JsonConfig)
                    {
                        if (cfg.applicationId != null) this.ApplicationId = cfg.applicationId;
                        if (cfg.directoryName != null) this.Directory = cfg.directoryName;
                        if (cfg.defaultResourceLocation != null) this.ResourceLocation = cfg.defaultResourceLocation;

                        if (cfg.defaultEvents != null)
                        {
                            var events = cfg.defaultEvents.Select(d => d.@event).Distinct();
                            this.EventStatus = events.Select(x => (EventStatusType)Enum.Parse(typeof(EventStatusType), x)).ToList();
                        }

                        if(cfg.emails != null)
                        {
                            this.Emails = cfg.emails.Select(e => e.address).Distinct().ToList();
                        }
                       
                    }
                }
            }
        }

        [CommandLine(Required = true, HelpMessage = "Subscription ID for the subscription containing the resource you want to alert on. This is the GUID value, not the name", ErrorMessage = "-subscriptionId is a required argument")]
        public string SubscriptionId { get; set; }

        [CommandLine(Required = true, HelpMessage = "Password for the service identity that will set the alerts", ErrorMessage = "is a required argument")]
        public string Password { get; set; }

        [CommandLine(Required = true, HelpMessage = "Name of the Azure resource to add the alert to", ErrorMessage = "is a required argument")]
        public string ResourceName { get; set; }        

        [CommandLine(Required = true, HelpMessage = "Name of the AAD directory to use. This is the part before the 'onmicrosoft.com'. [Optional if JsonConfig is specified]", ErrorMessage = "is a required argument when not included in the JsonConfig file")]
        public string Directory { get; set; }
       
        [CommandLine(Required = true, HelpMessage = "The Azure region the resource is located in. [Optional if JsonConfig is specified]", ErrorMessage = "is a required argument when not included in the JsonConfig file")]
        public string ResourceLocation { get; set; }

        [CommandLine(Required = true, HelpMessage = "The Guid value for the service account used to authenticate to Azure. [Optional if JsonConfig is specified]", ErrorMessage = "is a required argument when not included in the JsonConfig file")]
        public string ApplicationId { get; set; }

        [CommandLine(Required = true, HelpMessage = "The type of event notification(s) to subscribe to: New,Update,Resolved. Separate with a comma (,) to list more than one. [Optional if JsonConfig is specified]", ErrorMessage = "is a required argument when not included in the JsonConfig file")]
        public List<EventStatusType> EventStatus { get;set; }

        [CommandLine(Required = true, HelpMessage = "Email addresse(s) to send notifcation to. Separate with a comma (,) to list more than one. [Optional if JsonConfig is specified]", ErrorMessage = "is a required argument when not included in the JsonConfig file")]
        public List<string> Emails { get; set; }
       
        public List<string> Errors
        {
            get
            {
                return this.errors;
            }
        }

        public static Arguments ParseAndValidateArguments(string[] args)
        {
            Arguments tmp = new Arguments();
            var cmdArgs = ParseArguments(args);


             
            //Get properties with the CommandLineAttribute in the order of priorty
            var props = typeof(Arguments).GetProperties().Where(p => p.GetCustomAttributes(typeof(CommandLineAttribute),true).Any());

            //If help is requested, then print out help screen and exit
            if (cmdArgs.Count == 0 ||cmdArgs.Where(c => c.Key == "?" || c.Key == "help" || c.Key == "h").Any())
            {
                var argNamePad= props.Select(p => p.Name.Length).Max() + 5;
                var helpTableFormat = "    {0,-" + argNamePad + "}{1,-10}{2}\r\n";
                Console.WriteLine("AzureEvents.exe  -  a simplified method of subscribing to Azure health events related to your Azure resources\r\n");
                Console.WriteLine("Usage:");
                Console.WriteLine("    AzureEvents.exe -JsonConfig <file name> -SubscriptionId <Guid> -ResourceName <Resource Group Name> -Password <password>");
                Console.WriteLine("    AzureEvents.exe <required arguments>");
                Console.WriteLine("    AzureEvents.exe -help\r\n");
                Console.WriteLine("Options:");
                props.ToList().ForEach(p =>
                {
                    var attrib = p.GetCustomAttributes(typeof(CommandLineAttribute), true).First();
                    var msg = ((CommandLineAttribute)attrib).HelpMessage;
                    var required = ((CommandLineAttribute)attrib).Required ? "Required" : "Optional";
                    Console.WriteLine(string.Format(helpTableFormat,"-" + p.Name,required,msg));
                });

                return null;
            }


            //Assign values based on the command line arguments
            props.ToList().OrderBy(s => ((CommandLineAttribute)s.GetCustomAttributes(typeof(CommandLineAttribute),true).First()).Priority).ToList().ForEach(p =>
            {
                if (cmdArgs.ContainsKey(p.Name.ToLowerInvariant()))
                {
                    try
                    {
                        if (p.PropertyType.IsGenericType && ((System.Reflection.TypeInfo)p.PropertyType).ImplementedInterfaces.Select(i => i.Name == "IList").Any())
                        {
                            //Create the IList generic type
                            var inner = p.PropertyType.GetGenericArguments()[0];
                            var listdef = typeof(List<>).MakeGenericType(new[] { inner });
                            IList list = (IList)Activator.CreateInstance(listdef);

                            //Get the values from the command line
                            var v = cmdArgs[p.Name.ToLowerInvariant()].Split(Arguments.splitter);

                            if (inner.IsEnum)
                            {
                                foreach (var item in v)
                                {
                                    list.Add(Enum.Parse(inner, item,true)); 
                                }

                            }
                            
                            else
                            {
                                foreach (var item in v)
                                    list.Add(Convert.ChangeType(item, inner));
                            }

                            //Set the value to the property
                            p.SetValue(tmp, list);
                        }
                        else
                        {
                            p.SetValue(tmp, Convert.ChangeType(cmdArgs[p.Name.ToLowerInvariant()], p.PropertyType));
                        }
                    }
                    catch(Exception exe)
                    { }
                }
            });

            //Now check that the required elements have been populated..
            string errorFormat = "-{0} {1}";
            props.ToList().ForEach(p =>
            {
            var attrib = p.GetCustomAttributes(typeof(CommandLineAttribute), true).First();
                if (((CommandLineAttribute)attrib).Required && p.GetValue(tmp) == null)
                {
                    tmp.errors.Add(string.Format(errorFormat, p.Name, ((CommandLineAttribute)attrib).ErrorMessage));
                }
            });

           return tmp;

        }
        public static Dictionary<string,string> ParseArguments(string[] Args)
        {
            Dictionary<string, string> Parameters = new Dictionary<string, string>();
            Regex Spliter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 '--=nice=--'
            foreach (string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a possible enclosed value (=,:)
                Parts = Spliter.Split(Txt, 3);
                switch (Parts.Length)
                {
                    // Found a value (for the last parameter found (space separator))
                    case 1:
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter))
                            {
                                Parts[0] = Remover.Replace(Parts[0], "$1");
                                Parameters.Add(Parameter, Parts[0]);
                            }
                            Parameter = null;
                        }
                        // else Error: no parameter waiting for a value (skipped)
                        break;
                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (Parameter != null)
                            if (!Parameters.ContainsKey(Parameter))
                                Parameters.Add(Parameter, "true");

                        Parameter = Parts[1].ToLowerInvariant();
                        break;
                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter))
                                Parameters.Add(Parameter, "true");
                        }
                        Parameter = Parts[1].ToLowerInvariant();
                        // Remove possible enclosing characters (",')
                        if (!Parameters.ContainsKey(Parameter))
                        {
                            Parts[2] = Remover.Replace(Parts[2], "$1");
                            Parameters.Add(Parameter, Parts[2]);
                        }
                        Parameter = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (Parameter != null)
            {
                if (!Parameters.ContainsKey(Parameter)) Parameters.Add(Parameter, "true");
            }

            return Parameters;
        }


    }


    enum EventStatusType
    {
        New,
        Update,
        Resolved
    }


}
