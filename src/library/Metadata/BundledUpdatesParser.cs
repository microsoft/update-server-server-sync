// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Parses bundled updates information from update XML blob
    /// </summary>
    abstract class BundlesUpdatesParser
    {
        public static List<Identity> Parse(XDocument xdoc)
        {
            var bundledUpdates = new List<Identity>();

            var bundledUpdatesElements = xdoc.Descendants(XName.Get("BundledUpdates", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (bundledUpdatesElements.Count() > 1)
            {
                throw new Exception("Multiple BundledUpdates elements nodes found");
            }
            else if (bundledUpdatesElements.Count() == 1)
            {
                var bundledIdentityNodes = bundledUpdatesElements
                    .First()
                    .Descendants(XName.Get("UpdateIdentity", "http://schemas.microsoft.com/msus/2002/12/Update"));

                foreach(var bundledIdentityNode in bundledIdentityNodes)
                {
                    bundledUpdates.Add(
                        new Identity(
                            new UpdateIdentity()
                            {
                                UpdateID = Guid.Parse(bundledIdentityNode.Attribute("UpdateID").Value),
                                RevisionNumber = int.Parse(bundledIdentityNode.Attribute("RevisionNumber").Value)
                            }));
                }
            }

            return bundledUpdates;
        }
    }
}
