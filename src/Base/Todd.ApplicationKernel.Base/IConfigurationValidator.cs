using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.ApplicationKernel.Base
{
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Validates system configuration and throws an exception if configuration is not valid.
        /// </summary>
        void ValidateConfiguration();
    }
}
