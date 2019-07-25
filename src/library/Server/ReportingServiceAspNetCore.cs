// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.ServerReporting;

namespace Microsoft.UpdateServices.Server
{
    /// <summary>
    /// Reporting service implementation.
    /// </summary>
    class ReportingWebService : IReportingServiceAspNetCore
    {
        public Task<string[]> GetOutOfSyncComputersAsync(Cookie cookie, Guid parentServerId, ComputerLastRollupNumber[] lastRollupNumbers)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetRequiredInventoryTypeAsync(Cookie cookie, Guid rulesId, string rulesVersion)
        {
            throw new NotImplementedException();
        }

        public Task<RollupConfiguration> GetRollupConfigurationAsync(Cookie cookie)
        {
            throw new NotImplementedException();
        }

        public Task<PingResponse> PingAsync(PingRequest request)
        {
            return Task.FromResult(new PingResponse());
        }

        public Task<bool> ReportEventBatch2Async(string computerId, DateTime clientTime, ReportingEvent[] eventBatch)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReportEventBatchAsync(Cookie cookie, DateTime clientTime, ReportingEvent[] eventBatch)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReportInventoryAsync(Cookie cookie, DateTime clientTime, ReportingInventory inventory)
        {
            throw new NotImplementedException();
        }

        public Task<ChangedComputer[]> RollupComputersAsync(Cookie cookie, DateTime clientTime, ComputerRollupInfo[] computers)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RollupComputerStatusAsync(Cookie cookie, DateTime clientTime, Guid parentServerId, ComputerStatusRollupInfo[] computers)
        {
            throw new NotImplementedException();
        }

        public Task RollupDownstreamServersAsync(Cookie cookie, DateTime clientTime, DownstreamServerRollupInfo[] downstreamServers)
        {
            throw new NotImplementedException();
        }
    }
}
