// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Represents a detectoid. Detectoids determine applicabilty of updates for a computer and as such are used
    /// as pre-requisites for other updates.
    /// <para>
    /// Example detectoids: x64, x86, arm64, DirectX12 supported, etc.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var categoriesSource = await server.GetCategories();
    ///
    /// // Create a filter for quering drivers
    /// var filter = new QueryFilter(
    ///      categoriesSource.ProductsIndex.Values,
    ///      categoriesSource.ClassificationsIndex.Values.Where(c => c.Title.Equals("Driver")));
    /// 
    /// // Get drivers
    /// var updatesSource = await server.GetUpdates(filter);
    /// var drivers = updatesSource.UpdatesIndex.Values.OfType&lt;DriverUpdate&gt;();
    /// 
    /// updatesSource.Delete();
    /// categoriesSource.Delete();
    /// 
    /// </code>
    /// </example>
    public class Detectoid : Update
    {
        internal Detectoid(Identity id, IMetadataSource source) : base(id, source) { }

        internal Detectoid(Identity id, XDocument xdoc) : base(id, null)
        {
        }
    }
}
