using System;
using System.Net;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the Proxmox VE API returns an error response</summary>
    public class PveApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string Resource { get; }
        public string HttpMethod { get; }

        public PveApiException(HttpStatusCode statusCode, string message, string resource, string httpMethod)
            : base($"PVE API error ({(int)statusCode} {statusCode}) on {httpMethod} {resource}: {message}")
        {
            StatusCode = statusCode;
            Resource = resource;
            HttpMethod = httpMethod;
        }

        public PveApiException(HttpStatusCode statusCode, string message, string resource, string httpMethod, Exception innerException)
            : base($"PVE API error ({(int)statusCode} {statusCode}) on {httpMethod} {resource}: {message}", innerException)
        {
            StatusCode = statusCode;
            Resource = resource;
            HttpMethod = httpMethod;
        }
    }
}
