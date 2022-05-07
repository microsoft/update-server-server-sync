// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Declare an interface for the IReportingWebService (WCF) service that can be used in AspNetCore with slight modifications.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.1-preview-30422-0661")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://www.microsoft.com/SoftwareDistribution", ConfigurationName = "Microsoft.UpdateServices.WebServices.ClientReporting.WebServiceSoap")]
    interface IReportingWebService
    {

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/ReportEventBatch", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> ReportEventBatchAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ClientReporting.ReportingEvent[] eventBatch);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/ReportEventBatch2", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> ReportEventBatch2Async(string computerId, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ClientReporting.ReportingEvent[] eventBatch);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetRequiredInventoryType", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<int> GetRequiredInventoryTypeAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.Guid rulesId, string rulesVersion);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/ReportInventory", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> ReportInventoryAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ClientReporting.ReportingInventory inventory);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetRollupConfiguration", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<Microsoft.UpdateServices.WebServices.ClientReporting.RollupConfiguration> GetRollupConfigurationAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/RollupDownstreamServers", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task RollupDownstreamServersAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ClientReporting.DownstreamServerRollupInfo[] downstreamServers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/RollupComputers", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<Microsoft.UpdateServices.WebServices.ClientReporting.ChangedComputer[]> RollupComputersAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.DateTime clientTime, Microsoft.UpdateServices.WebServices.ClientReporting.ComputerRollupInfo[] computers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetOutOfSyncComputers", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<string[]> GetOutOfSyncComputersAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.Guid parentServerId, Microsoft.UpdateServices.WebServices.ClientReporting.ComputerLastRollupNumber[] lastRollupNumbers);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/RollupComputerStatus", ReplyAction = "*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        System.Threading.Tasks.Task<bool> RollupComputerStatusAsync(Microsoft.UpdateServices.WebServices.ClientReporting.Cookie cookie, System.DateTime clientTime, System.Guid parentServerId, Microsoft.UpdateServices.WebServices.ClientReporting.ComputerStatusRollupInfo[] computers);
    }
}