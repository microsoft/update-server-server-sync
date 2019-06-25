// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Parses superseeded updates information from update XML blob
    /// </summary>
    abstract class SuperseededUpdatesParser
    {
        public static List<MicrosoftUpdateIdentity> Parse(XDocument xdoc)
        {
            var superseededIds = new List<MicrosoftUpdateIdentity>();

            var superseededUpdatesElements = xdoc.Descendants(XName.Get("SupersededUpdates", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (superseededUpdatesElements.Count() > 1)
            {
                throw new Exception("Multiple superseeded elements nodes found");
            } else if (superseededUpdatesElements.Count() == 1)
            {
                var superseedeIdentityNodes = superseededUpdatesElements
                    .First()
                    .Descendants(XName.Get("UpdateIdentity", "http://schemas.microsoft.com/msus/2002/12/Update"));

                foreach(var superseededIdentityNode in superseedeIdentityNodes)
                {
                    superseededIds
                        .Add(
                        new MicrosoftUpdateIdentity(
                            new UpdateIdentity()
                            {
                                UpdateID = Guid.Parse(superseededIdentityNode.Attribute("UpdateID").Value)
                            }));
                }
            }

            return superseededIds;
        }
    }
}
