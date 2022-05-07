// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.ClientReporting;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Reporting web service implementation. Placeholder only.
    /// </summary>
    public class ReportingWebService : IReportingWebService
    {
        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="parentServerId"></param>
        /// <param name="lastRollupNumbers"></param>
        /// <returns></returns>
        public Task<string[]> GetOutOfSyncComputersAsync(Cookie cookie, Guid parentServerId, ComputerLastRollupNumber[] lastRollupNumbers)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="rulesId"></param>
        /// <param name="rulesVersion"></param>
        /// <returns></returns>
        public Task<int> GetRequiredInventoryTypeAsync(Cookie cookie, Guid rulesId, string rulesVersion)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public Task<RollupConfiguration> GetRollupConfigurationAsync(Cookie cookie)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="computerId"></param>
        /// <param name="clientTime"></param>
        /// <param name="eventBatch"></param>
        /// <returns></returns>
        public Task<bool> ReportEventBatch2Async(string computerId, DateTime clientTime, ReportingEvent[] eventBatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="clientTime"></param>
        /// <param name="eventBatch"></param>
        /// <returns></returns>
        public Task<bool> ReportEventBatchAsync(Cookie cookie, DateTime clientTime, ReportingEvent[] eventBatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="clientTime"></param>
        /// <param name="inventory"></param>
        /// <returns></returns>
        public Task<bool> ReportInventoryAsync(Cookie cookie, DateTime clientTime, ReportingInventory inventory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="clientTime"></param>
        /// <param name="computers"></param>
        /// <returns></returns>
        public Task<ChangedComputer[]> RollupComputersAsync(Cookie cookie, DateTime clientTime, ComputerRollupInfo[] computers)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="clientTime"></param>
        /// <param name="parentServerId"></param>
        /// <param name="computers"></param>
        /// <returns></returns>
        public Task<bool> RollupComputerStatusAsync(Cookie cookie, DateTime clientTime, Guid parentServerId, ComputerStatusRollupInfo[] computers)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="clientTime"></param>
        /// <param name="downstreamServers"></param>
        /// <returns></returns>
        public Task RollupDownstreamServersAsync(Cookie cookie, DateTime clientTime, DownstreamServerRollupInfo[] downstreamServers)
        {
            throw new NotImplementedException();
        }
    }
}
