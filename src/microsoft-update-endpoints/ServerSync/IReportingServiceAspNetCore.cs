// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.ServerReporting;
using System.ServiceModel;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync
{
    /// <summary>
    /// Declare an interface for the reporting web service (WCF) that can be used in AspNetCore with slight modifications.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.1-preview-30422-0661")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://www.microsoft.com/SoftwareDistribution", ConfigurationName = "Microsoft.UpdateServices.WebServices.ServerReporting.WebServiceSoap")]
    interface IReportingServiceAspNetCore
    {

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/ReportEventBatch", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> ReportEventBatchAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ServerReporting.ReportingEvent[] eventBatch);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/ReportEventBatch2", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> ReportEventBatch2Async(string computerId, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ServerReporting.ReportingEvent[] eventBatch);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetRequiredInventoryType", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<int> GetRequiredInventoryTypeAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.Guid rulesId, string rulesVersion);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/ReportInventory", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> ReportInventoryAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ServerReporting.ReportingInventory inventory);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetRollupConfiguration", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<Microsoft.UpdateServices.WebServices.ServerReporting.RollupConfiguration> GetRollupConfigurationAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/RollupDownstreamServers", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task RollupDownstreamServersAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ServerReporting.DownstreamServerRollupInfo[] downstreamServers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/RollupComputers", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<Microsoft.UpdateServices.WebServices.ServerReporting.ChangedComputer[]> RollupComputersAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ServerReporting.ComputerRollupInfo[] computers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetOutOfSyncComputers", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<string[]> GetOutOfSyncComputersAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.Guid parentServerId, Microsoft.UpdateServices.WebServices.ServerReporting.ComputerLastRollupNumber[] lastRollupNumbers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/RollupComputerStatus", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> RollupComputerStatusAsync(Microsoft.UpdateServices.WebServices.ServerReporting.Cookie cookie, System.DateTime clientTime, System.Guid parentServerId, Microsoft.UpdateServices.WebServices.ServerReporting.ComputerStatusRollupInfo[] computers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/IMonitorable/Ping", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<Microsoft.UpdateServices.WebServices.ServerReporting.PingResponse> PingAsync(Microsoft.UpdateServices.WebServices.ServerReporting.PingRequest request);
    }
}