using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices
{
    /// <summary>
    /// Types of errors than an upstream server can return with a SOAP reply.
    /// The enum member names must match exactly the string error codes mentioned in the protocol specification
    /// </summary>
    public enum UpstreamServerErrorCodes
    {
        InvalidAuthorizationCookie,
        IncompatibleProtocolVersion,
        InternalServerError,
        InvalidParameters,
        Unknown
    }

    /// <summary>
    /// An exception raised when an error code is received from an upstream update server.
    /// It contains an inner generic FaultException from which a specific USS error is infered.
    /// </summary>
    public class UpstreamServerException : Exception
    {
        /// <summary>
        /// An infered ErrorCode according to the protocol specification
        /// </summary>
        public readonly UpstreamServerErrorCodes ErrorCode;

        /// <summary>
        /// Create a UpstreamServerException from a FaultException.
        /// </summary>
        /// <param name="soapException">The inner soap exception. Its "Reason" field is matched to a known error code</param>
        public UpstreamServerException(System.ServiceModel.FaultException soapException) : base(soapException.Message, soapException)
        {
            var faultReasonText = soapException.Reason.GetMatchingTranslation().Text;

            ErrorCode = UpstreamServerErrorCodes.Unknown;

            if (!string.IsNullOrEmpty(faultReasonText))
            {
                // Match the reason in the inner exception with a known error code
                foreach (var errorCode in (UpstreamServerErrorCodes[])Enum.GetValues(typeof(UpstreamServerErrorCodes)))
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
