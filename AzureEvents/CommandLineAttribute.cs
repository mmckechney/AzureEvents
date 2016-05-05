using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureEvents
{
    [AttributeUsage(AttributeTargets.Property)]
    class CommandLineAttribute : System.Attribute
    {
        private string name = string.Empty;
        private bool required = false;
        private string helpMessage = string.Empty;
        private string errorMessage = string.Empty;
        private int priority = int.MaxValue;

        public bool Required
        {
            get
            {
                return required;
            }

            set
            {
                required = value;
            }
        }

        public string HelpMessage
        {
            get
            {
                return helpMessage;
            }

            set
            {
                helpMessage = value;
            }
        }

        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }

            set
            {
                errorMessage = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public int Priority
        {
            get
            {
                return priority;
            }

            set
            {
                priority = value;
            }
        }
    }
}
