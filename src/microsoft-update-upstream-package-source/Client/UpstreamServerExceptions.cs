// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Source
{
    /// <summary>
    /// The UpstreamServerErrorCode enumeration contains errors than an upstream server can return with a SOAP reply.
    /// </summary>
    public enum UpstreamServerErrorCode
    {
        /// <summary>
        /// The authorization cookie was invalid
        /// </summary>
        InvalidAuthorizationCookie,
        /// <summary>
        /// The protocol version is not compatible with the server
        /// </summary>
        IncompatibleProtocolVersion,
        /// <summary>
        /// Internal server error
        /// </summary>
        InternalServerError,
        /// <summary>
        /// The parametes sent to the server are invalid
        /// </summary>
        InvalidParameters,
        /// <summary>
        /// Unknown other errors
        /// </summary>
        Unknown
    }

    /// <summary>
    /// The exception that is thrown when an error code is received from an upstream update server.
    /// It contains an inner SOAP FaultException.
    /// </summary>
    public class UpstreamServerException : Exception
    {
        /// <summary>
        /// Gets the UpstreamServerErrorCode received over SOAP from the server
        /// </summary>
        /// <value>
        /// Error code reported by the upstream server.
        /// </value>
        public readonly UpstreamServerErrorCode ErrorCode;

        /// <summary>
        /// Initialize a new instance of UpstreamServerException from a SOAP FaultException.
        /// </summary>
        /// <param name="soapException">The inner SOAP exception.</param>
        public UpstreamServerException(System.ServiceModel.FaultException soapException) : base(soapException.Message, soapException)
        {
            var faultReasonText = soapException.Reason.GetMatchingTranslation().Text;

            ErrorCode = UpstreamServerErrorCode.Unknown;

            if (!string.IsNullOrEmpty(faultReasonText))
            {
                // Match the reason in the inner exception with a known error code
                foreach (var errorCode in (UpstreamServerErrorCode[])Enum.GetValues(typeof(UpstreamServerErrorCode)))
                {
                    if (faultReasonText.Contains(errorCode.ToString()))
                    {
                        ErrorCode = errorCode;
                        break;
                    }
                }
            }
        }
    }
}
