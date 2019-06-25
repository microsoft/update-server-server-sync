// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata.Prerequisites
{
    /// <summary>
    /// Parses prerequisites from an update XML
    /// </summary>
    abstract class PrerequisitesParser
    {
        public static List<Prerequisites.Prerequisite> Parse(XDocument xdoc)
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
                        parsedPrerequisites.Add(new Prerequisites.SimplePrerequisite(prerequisite));
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
