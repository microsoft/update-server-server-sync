using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata.Prerequisites
{
    /// <summary>
    /// Base, abstract class for update prerequisites.
    /// <para>See <see cref="Simple"/> and <see cref="AtLeastOne"/> for possible prerequisite classes.</para>
    /// </summary>
    public abstract class Prerequisite
    {
        internal static List<Prerequisites.Prerequisite> FromXml(XDocument xdoc)
        {
            var parsedPrerequisites = new List<Prerequisites.Prerequisite>();
            // Get the prerequisites node
            var prerequisitesElements = xdoc.Descendants(XName.Get("Prerequisites", "http://schemas.microsoft.com/msus/2002/12/Update"));

            if (prerequisitesElements.Count() > 1)
            {
                throw new Exception("More than 1 prerequite nodes found");
            }

            if (prerequisitesElements.Count() > 0)
            {
                var allPrerequisites = prerequisitesElements.First().Elements();
                foreach (var prerequisite in allPrerequisites)
                {
                    if (prerequisite.Name.LocalName.Equals("UpdateIdentity"))
                    {
                        parsedPrerequisites.Add(new Prerequisites.Simple(prerequisite));
                    }
                    else if (prerequisite.Name.LocalName.Equals("AtLeastOne"))
                    {
                        parsedPrerequisites.Add(new Prerequisites.AtLeastOne(prerequisite));
                    }
                    else
                    {
                        throw new Exception($"Unknown prerequisite type: {prerequisite.Name.LocalName}");
                    }
                }
            }

            return parsedPrerequisites;
        }
    }
}
