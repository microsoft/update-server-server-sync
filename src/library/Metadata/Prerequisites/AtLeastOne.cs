using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata.Prerequisites
{
    /// <summary>
    /// A collection of prerequisites, of which at least one must be met for the AtLeastOne prerequisite to be satisfied.
    /// </summary>
    public class AtLeastOne : Prerequisite
    {
        /// <summary>
        /// Get the list of simple prerequisites that are part of the group
        /// </summary>
        /// <value>
        /// List of simple prerequisites
        /// </value>
        public List<Simple> Simple { get; private set; }

        /// <summary>
        /// Check if the AtLestOne prerequisite is a "category" prerequisite. Category prerequisites are not true prerequisites,
        /// just a way to encode a product and classification for an update.
        /// </summary>
        public bool IsCategory { get; private set; }

        /// <summary>
        /// From XML constructor
        /// </summary>
        /// <param name="xmlData">XML containing prerequisite data</param>
        internal AtLeastOne(XElement xmlData)
        {
            var isCategoryAttributes = xmlData.Attributes("IsCategory");
            if (isCategoryAttributes.Count() == 1)
            {
                IsCategory = bool.Parse(isCategoryAttributes.First().Value);
            }

            Simple = new List<Simple>();

            // Grab all sub-prerequisites that are part of this group
            var subPrerequisites = xmlData.Elements();
            foreach (var subprereq in subPrerequisites)
            {
                if (subprereq.Name.LocalName.Equals("UpdateIdentity"))
                {
                    Simple.Add(new Simple(subprereq));
                }
                else
                {
                    throw new Exception($"Unknown prerequisite type: {subprereq.Name.LocalName}");
                }
            }
        }
    }
}
