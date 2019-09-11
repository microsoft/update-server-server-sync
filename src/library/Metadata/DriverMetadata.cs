// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Extended information about a hardware ID handled by a driver update.
    /// A driver update matches one or more hardware ids; each match has its own metadata that describes it.
    /// </summary>
    public class DriverMetadata : IComparable
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
        /// Gets driver vesion information
        /// </summary>
        /// <value>
        /// Driver version
        /// </value>
        [JsonProperty]
        public DriverVersion Versioning { get; private set; }

        /// <summary>
        /// Gets the driver class type
        /// </summary>
        /// <value>
        /// Driver class (graphics, USB, etc.)
        /// </value>
        [JsonProperty]
        public string Class { get; set; }

        /// <summary>
        /// List of driver feature scores.
        /// <para>
        /// See <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/feature-score--windows-vista-and-later-">feature score documentation</see> for more information.
        /// </para>
        /// </summary>
        /// <value>
        /// List of feature scores
        /// </value>
        [JsonProperty]
        public List<DriverFeatureScore> FeatureScores;

        /// <summary>
        /// List of distribution computer hardware ids. A driver update that contains distribution computer hardware ids is only offered to computers that match the computer hardware id, regardless of device hardware id matching.
        /// <para>
        /// See the <see href="http://download.microsoft.com/download/B/A/8/BA89DCE0-DB25-4425-9EFF-1037E0BA06F9/windows10_driver_publishing_workflow.docx">driver publishing manual</see> for information on how this field is used
        /// for device matching.
        /// </para>
        /// </summary>
        /// <value>
        /// List of distribution computer hardware ids.
        /// </value>
        [JsonProperty]
        public List<Guid> DistributionComputerHardwareId;

        /// <summary>
        /// List of target computer hardware ids. A driver update that contains target computer hardware ids is only offered to computers that match the computer hardware id, regardless of device hardware id matching.
        /// <para>
        /// See the <see href="http://download.microsoft.com/download/B/A/8/BA89DCE0-DB25-4425-9EFF-1037E0BA06F9/windows10_driver_publishing_workflow.docx">driver publishing manual</see> for information on hwo this field is used
        /// for device matching.
        /// </para>
        /// </summary>
        /// <value>
        /// List of target computer hardware ids.
        /// </value>
        [JsonProperty]
        public List<Guid> TargetComputerHardwareId;

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

            FeatureScores = new List<DriverFeatureScore>();

            foreach (var featureScore in xmlMetadata.Descendants(XName.Get("FeatureScore", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver")))
            {
                FeatureScores.Add(new DriverFeatureScore()
                {
                    OperatingSystem = featureScore.Attribute("OperatingSystem").Value,
                    Score = Convert.ToByte(featureScore.Attribute("FeatureScore").Value, 16)
                });
            }

            DistributionComputerHardwareId = new List<Guid>();
            foreach (var distributionComputerHardwareId in xmlMetadata.Descendants(XName.Get("DistributionComputerHardwareId", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver")))
            {
                DistributionComputerHardwareId.Add(Guid.Parse(distributionComputerHardwareId.Value));
            }

            TargetComputerHardwareId = new List<Guid>();
            foreach (var targetComputerHardwareId in xmlMetadata.Descendants(XName.Get("TargetComputerHardwareId", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver")))
            {
                TargetComputerHardwareId.Add(Guid.Parse(targetComputerHardwareId.Value));
            }

            Versioning = new DriverVersion();

            // Go through all property names, extract them from the XML node and set this objects properties
            foreach (var propertyName in propertyNames)
            {
                var propertyAttributes = xmlMetadata.Attributes(propertyName);
                if (propertyAttributes.Count() > 0)
                {
                    if (propertyName == "DriverVerDate")
                    {
                        Versioning.Date = DriverVersion.ParseDateFromString(propertyAttributes.First().Value);
                    }
                    else if (propertyName == "DriverVerVersion")
                    {
                        Versioning.Version = DriverVersion.ParseVersionFromString(propertyAttributes.First().Value);
                    }
                    else if (propertyName == "HardwareID")
                    {
                        HardwareID = propertyAttributes.First().Value.ToLowerInvariant();
                    }
                    else
                    {
                        this.GetType().GetProperty(propertyName).SetValue(this, propertyAttributes.First().Value);
                    }
                }
            }
        }

        static internal List<DriverMetadata> GetDriverMetadataFromXml(XDocument xdoc)
        {
            var returnList = new List<DriverMetadata>();
            // Get the driver metadata nodes
            var metadataElements = xdoc.Descendants(XName.Get("WindowsDriverMetaData", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver"));

            foreach (var metadataElement in metadataElements)
            {
                returnList.Add(new DriverMetadata(metadataElement));
            }

            return returnList;
        }

        /// <summary>
        /// Compare two DriverMetadata by their date and version
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (object.ReferenceEquals(obj, null))
            {
                return 1;
            }

            if (!(obj is DriverMetadata))
            {
                return -1;
            }

            var other = obj as DriverMetadata;
            return this.Versioning.CompareTo(other.Versioning);
        }
    }
}
