// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata.Prerequisites
{
    /// <summary>
    /// Interface implemented by updates that have prerequisites
    /// </summary>
    public interface IUpdateWithPrerequisites
    {
        List<Prerequisite> Prerequisites { get; }
    }

    /// <summary>
    /// Interface implemented by updates that have a classification
    /// </summary>
    public interface IUpdateWithClassification
    {
        List<Guid> ClassificationIds { get; }

        void ResolveClassification(List<Classification> allClassifications);
    }

    /// <summary>
    /// Resolves "IsCategory" prerequisites to a category.
    /// This is needed because the category and classification for an update is encoded as a prerequisite
    /// </summary>
    public abstract class CategoryResolver
    {
        /// <summary>
        /// Resolve product from prerequisites and list of all known products
        /// </summary>
        /// <param name="prerequisites">Update prerequisites</param>
        /// <param name="allProducts">All known products</param>
        /// <returns>All products that were found in the prerequisites list</returns>
        public static List<Guid> ResolveProductFromPrerequisites(List<Prerequisite> prerequisites, List<MicrosoftProduct> allProducts)
        {
            var returnList = new List<Guid>();
            // Find all "AtLeastOne" prerequisites
            var categoryPrereqs = prerequisites.OfType<AtLeastOne>().ToList();

            foreach (var category in categoryPrereqs)
            {
                foreach(var subCategory in category.Simple)
                {
                    var matchingProduct = allProducts.Find(p => p.Identity.Raw.UpdateID == subCategory.UpdateId);
                    if (matchingProduct != null)
                    {
                        returnList.Add(matchingProduct.Identity.Raw.UpdateID);
                    }
                }
            }

            return returnList;
        }

        /// <summary>
        /// Resolve classification from prerequisites and list of all known classifications
        /// </summary>
        /// <param name="prerequisites">Update prerequisites</param>
        /// <param name="allProducts">All known classifications</param>
        /// <returns>On success, the GUID of the classification, empty guid otherwise</returns>
        public static List<Guid> ResolveClassificationFromPrerequisites(List<Prerequisite> prerequisites, List<Classification> allClassifications)
        {
            var returnList = new List<Guid>();
            // Find all "AtLeastOne" prerequisites
            var categoryPrereqs = prerequisites.OfType<AtLeastOne>().ToList();

            foreach (var category in categoryPrereqs)
            {
                foreach (var subCategory in category.Simple)
                {
                    var matchingProduct = allClassifications.Find(p => p.Identity.Raw.UpdateID == subCategory.UpdateId);
                    if (matchingProduct != null)
                    {
                        returnList.Add(matchingProduct.Identity.Raw.UpdateID);
                    }
                }

            }

            return returnList;
        }
    }

    /// <summary>
    /// Types of prerequisites for an update
    /// </summary>
    public enum UpdatePrerequisiteType
    {
        UpdateIdentity,
        AtLeastOne
    }

    /// <summary>
    /// Base class for prerequisites
    /// </summary>
    public abstract class Prerequisite
    {
        /// <summary>
        /// The type of this prerequisite. Used when deserializing from JSON
        /// </summary>
        public UpdatePrerequisiteType PrerequisiteType { get; set; }

        [JsonConstructor]
        protected Prerequisite()
        {

        }
    }

    /// <summary>
    /// Simple prerequisite: a single update ID
    /// </summary>
    public class SimplePrerequisite : Prerequisite
    {
        /// <summary>
        /// The update ID that is required for another update
        /// </summary>
        public Guid UpdateId { get; set; }

        /// <summary>
        /// Initialize an object during deserialization
        /// </summary>
        [JsonConstructor]
        private SimplePrerequisite()
        {
            
        }

        /// <summary>
        /// Initialize a prerequisite from XML data
        /// </summary>
        /// <param name="xmlData">The XML that contains the data for the prerequisite</param>
        public SimplePrerequisite(XElement xmlData)
        {
            PrerequisiteType = UpdatePrerequisiteType.UpdateIdentity;
            // Parse the guid from the XML data
            UpdateId = new Guid(xmlData.Attributes("UpdateID").First().Value);
        }
    }

    /// <summary>
    /// A group of prerequisites, and at least one must be met
    /// </summary>
    public class AtLeastOne : Prerequisite
    {
        /// <summary>
        /// The prerequisites that are part of this group
        /// </summary>
        public List<SimplePrerequisite> Simple { get; set; }

        /// <summary>
        /// From XML
        /// </summary>
        public bool IsCategory { get; set; }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        [JsonConstructor]
        private AtLeastOne() { }

        /// <summary>
        /// From XML constructor
        /// </summary>
        /// <param name="xmlData">XML containing prerequisite data</param>
        public AtLeastOne(XElement xmlData)
        {
            PrerequisiteType = UpdatePrerequisiteType.AtLeastOne;

            var isCategoryAttributes = xmlData.Attributes("IsCategory");
            if (isCategoryAttributes.Count() == 1)
            {
                IsCategory = bool.Parse(isCategoryAttributes.First().Value);
            }
            
            Simple = new List<SimplePrerequisite>();

            // Grab all sub-prerequisites that are part of this group
            var subPrerequisites = xmlData.Elements();
            foreach(var subprereq in subPrerequisites)
            {
                if (subprereq.Name.LocalName.Equals("UpdateIdentity"))
                {
                    Simple.Add(new SimplePrerequisite(subprereq));
                }
                else
                {
                    throw new Exception($"Unknown prerequisite type: {subprereq.Name.LocalName}");
                }
            }
        }
    }

    /// <summary>
    /// Converts between various polimorphic prerequisites during deserialization
    /// The deserializer does not know which derived class to use when deserializing the abstract Prerequisite class 
    /// </summary>
    public class PrerequisiteConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Prerequisite).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);

            // Peek at the PrerequisiteType in the JSON to determine which class we are actually deserializing now
            var prerequisiteType = (UpdatePrerequisiteType)item["PrerequisiteType"].Value<int>();
            if (prerequisiteType == UpdatePrerequisiteType.UpdateIdentity)
            {
                return item.ToObject<SimplePrerequisite>();
            }
            else if (prerequisiteType == UpdatePrerequisiteType.AtLeastOne)
            {
                return item.ToObject<AtLeastOne>();
            }
            else
            {
                throw new Exception("Unexpected prerequisite type");
            }
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
