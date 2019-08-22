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
    class PrerequisitesAnalyzer
    {
        public static bool IsApplicable(Update update, List<Guid> installedPrerequisites)
        {
            if (update.Prerequisites == null)
            {
                return true;
            }

            foreach(var prereq in update.Prerequisites)
            {
                if (prereq is Simple)
                {
                    if (!installedPrerequisites.Contains((prereq as Simple).UpdateId))
                    {
                        return false;
                    }
                }
                else if (prereq is AtLeastOne)
                {
                    var atLeastOne = false;
                    foreach(var atLeastOnePrereq in (prereq as AtLeastOne).Simple)
                    {
                        if (installedPrerequisites.Contains(atLeastOnePrereq.UpdateId))
                        {
                            atLeastOne = true;
                            break;
                        }
                    }

                    if (!atLeastOne)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
