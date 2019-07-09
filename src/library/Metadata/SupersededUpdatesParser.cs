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
    /// Parses superseded updates information from update XML blob
    /// </summary>
    abstract class SupersededUpdatesParser
    {
        public static List<Identity> Parse(XDocument xdoc)
        {
            var supersededIds = new List<Identity>();

            var supersededUpdatesElements = xdoc.Descendants(XName.Get("SupersededUpdates", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (supersededUpdatesElements.Count() > 1)
            {
                throw new Exception("Multiple superseded elements nodes found");
            } else if (supersededUpdatesElements.Count() == 1)
            {
                var superseedeIdentityNodes = supersededUpdatesElements
                    .First()
                    .Descendants(XName.Get("UpdateIdentity", "http://schemas.microsoft.com/msus/2002/12/Update"));

                foreach(var supersededIdentityNode in superseedeIdentityNodes)
                {
                    supersededIds
                        .Add(
                        new Identity(
                            new UpdateIdentity()
                            {
                                UpdateID = Guid.Parse(supersededIdentityNode.Attribute("UpdateID").Value)
                            }));
                }
            }

            return supersededIds;
        }
    }
}
