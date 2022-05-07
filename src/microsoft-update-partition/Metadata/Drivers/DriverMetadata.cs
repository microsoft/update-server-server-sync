// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers
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

        internal DriverMetadata(XPathNavigator driverMetadataNavigator, XmlNamespaceManager namespaceManager)
        {
            string[] attributeQueries = new string[] {
                "HardwareID", "WhqlDriverID", "Manufacturer", "Company", "Provider", "DriverVerDate", "DriverVerVersion", "Class" };

            FeatureScores = GetFeatureScoresList(driverMetadataNavigator, namespaceManager);
            DistributionComputerHardwareId = GetHardwareIdListFromXml("DistributionComputerHardwareId", driverMetadataNavigator, namespaceManager);
            TargetComputerHardwareId = GetHardwareIdListFromXml("TargetComputerHardwareId", driverMetadataNavigator, namespaceManager);

            Versioning = new DriverVersion();

            // Go through all property names, extract them from the XML node and set this objects properties
            foreach (var propertyName in attributeQueries)
            {
                XPathExpression propertyQuery = driverMetadataNavigator.Compile($"@{propertyName}");
                propertyQuery.SetContext(namespaceManager);

                var propertyQueryResult = driverMetadataNavigator.Evaluate(propertyQuery) as XPathNodeIterator;

                if (propertyQueryResult.Count > 0)
                {
                    propertyQueryResult.MoveNext();
                    var propertyString = propertyQueryResult.Current.Value;

                    if (propertyName == "DriverVerDate")
                    {
                        Versioning.Date = DriverVersion.ParseDateFromString(propertyString);
                    }
                    else if (propertyName == "DriverVerVersion")
                    {
                        Versioning.Version = DriverVersion.ParseVersionFromString(propertyString);
                    }
                    else if (propertyName == "HardwareID")
                    {
                        HardwareID = propertyString.ToLowerInvariant();
                    }
                    else
                    {
                        this.GetType().GetProperty(propertyName).SetValue(this, propertyString);
                    }
                }
            }
        }

        private static List<DriverFeatureScore> GetFeatureScoresList(XPathNavigator driverMetadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression featureScoreQuery = driverMetadataNavigator.Compile("drv:FeatureScore");
            featureScoreQuery.SetContext(namespaceManager);
            var featureScoreResult = driverMetadataNavigator.Evaluate(featureScoreQuery) as XPathNodeIterator;
            var returnList = new List<DriverFeatureScore>();
            if (featureScoreResult.Count > 0)
            {
                while (featureScoreResult.MoveNext())
                {
                    returnList.Add(new DriverFeatureScore()
                    {
                        OperatingSystem = featureScoreResult.Current.GetAttribute("OperatingSystem",""),
                        Score = Convert.ToByte(featureScoreResult.Current.GetAttribute("FeatureScore",""), 16)
                    });
                }
            }

            return returnList;
        }

        private static List<Guid> GetHardwareIdListFromXml(string listName, XPathNavigator driverMetadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression hwIdQuery = driverMetadataNavigator.Compile($"drv:{listName}");
            hwIdQuery.SetContext(namespaceManager);
            var hwIdResult = driverMetadataNavigator.Evaluate(hwIdQuery) as XPathNodeIterator;
            var returnList = new List<Guid>();
            if (hwIdResult.Count > 0)
            {
                while (hwIdResult.MoveNext())
                {
                    returnList.Add(Guid.Parse(hwIdResult.Current.Value));
                }
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
            if (obj is null)
            {
                return 1;
            }

            if (obj is not DriverMetadata)
            {
                return -1;
            }

            var other = obj as DriverMetadata;
            return this.Versioning.CompareTo(other.Versioning);
        }
    }
}
