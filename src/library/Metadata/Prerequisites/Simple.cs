using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata.Prerequisites
{
    /// <summary>
    /// Simple prerequisite: a single update ID.
    /// <para>The update ID contained in a simple prerequisite must be installed before the update that has this prerequisite can be installed.</para>
    /// <para>The detectoid ID contained in a simple prerequisite must evaluate to true before the update that has this prerequisite can be installed. See <see cref="Detectoid"/> for more details.</para>
    /// </summary>
    public class Simple : Prerequisite
    {
        /// <summary>
        /// The update ID or detectoid ID that is required before an update can be installed.
        /// </summary>
        public Guid UpdateId { get; private set; }

        /// <summary>
        /// Initialize a prerequisite from XML data
        /// </summary>
        /// <param name="xmlData">The XML that contains the data for the prerequisite</param>
        internal Simple(XElement xmlData)
        {
            // Parse the guid from the XML data
            UpdateId = new Guid(xmlData.Attributes("UpdateID").First().Value);
        }
    }
}
