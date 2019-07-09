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
        /// <summary>
        /// Gets the list of prerequisites for an update
        /// </summary>
        /// <value>
        /// List of prerequisites (<see cref="Simple"/>, <see cref="AtLeastOne"/> etc.)
        /// </value>
        List<Prerequisite> Prerequisites { get; }
    }

    interface IUpdateWithClassificationInternal
    {
        void ResolveClassification(List<Classification> allClassifications);
    }
}
