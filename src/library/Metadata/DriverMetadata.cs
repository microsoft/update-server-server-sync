using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Extended metadata for a DriverUpdate
    /// </summary>
    public class DriverMetadata
    {
        /// <summary>
        /// Gets the hardware IDs this driver update is applicable to
        /// </summary>
        /// <value>
        /// Hardware ID string
        /// </value>
        [JsonProperty]
        public string HardwareID { get; private set; }

        /// <summary>
        /// Gets the Windows Hardware Quality Lab driver ID
        /// </summary>
        /// <value>
        /// WHQL driver ID string
        /// </value>
        [JsonProperty]
        public string WhqlDriverID { get; private set; }

        /// <summary>
        /// Gets the driver manufacturer
        /// </summary>
        /// <value>
        /// Manufacturer name
        /// </value>
        [JsonProperty]
        public string Manufacturer { get; private set; }

        /// <summary>
        /// Gets the Company that created the driver
        /// </summary>
        /// <value>
        /// Company name
        /// </value>
        [JsonProperty]
        public string Company { get; private set; }

        /// <summary>
        /// Gets the entity that provided the driver.
        /// </summary>
        /// <value>
        /// Driver provider name
        /// </value>
        [JsonProperty]
        public string Provider { get; private set; }

        /// <summary>
        /// Gets the driver timestamp
        /// </summary>
        /// <value>
        /// Driver timestamp
        /// </value>
        [JsonProperty]
        public string DriverVerDate { get; private set; }

        /// <summary>
        /// Gets the driver version
        /// </summary>
        /// <value>
        /// Driver version
        /// </value>
        [JsonProperty]
        public string DriverVerVersion { get; private set; }

        /// <summary>
        /// Gets the driver class type
        /// </summary>
        /// <value>
        /// Driver class (graphics, USB, etc.)
        /// </value>
        [JsonProperty]
        public string Class { get; set; }

        [JsonConstructor]
        private DriverMetadata() { }

        /// <summary>
        /// Constructor; parses the passed WindowsDriverMetaData XML element
        /// </summary>
        /// <param name="xmlMetadata">The WindowsDriverMetaData element to parse data from</param>
        internal DriverMetadata(XElement xmlMetadata)
        {
            string[] propertyNames = new string[] {
                "HardwareID", "WhqlDriverID", "Manufacturer", "Company", "Provider", "DriverVerDate", "DriverVerVersion", "Class" };

            // Go through all property names, extract them from the XML node and set this objects properties
            foreach (var propertyName in propertyNames)
            {
                var propertyAttributes = xmlMetadata.Attributes(propertyName);
                if (propertyAttributes.Count() > 0)
                {
                    this.GetType().GetProperty(propertyName).SetValue(this, propertyAttributes.First().Value);
                }
            }
        }
    }
}
