using System;
using System.Net;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the Proxmox VE API returns an error response.</summary>
    public class PveApiException : Exception
    {
        /// <summary>The HTTP status code returned by the PVE API.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>The API resource path that was requested.</summary>
        public string Resource { get; }

        /// <summary>The HTTP method used for the request (GET, POST, PUT, DELETE).</summary>
        public string HttpMethod { get; }

        /// <summary>Initializes a new instance for a failed PVE API request.</summary>
        /// <param name="statusCode">The HTTP status code returned.</param>
        /// <param name="message">The error message from the API.</param>
        /// <param name="resource">The API resource path that was requested.</param>
        /// <param name="httpMethod">The HTTP method used.</param>
        public PveApiException(HttpStatusCode statusCode, string message, string resource, string httpMethod)
            : base($"PVE API error ({(int)statusCode} {statusCode}) on {httpMethod} {resource}: {message}")
        {
            StatusCode = statusCode;
            Resource = resource;
            HttpMethod = httpMethod;
        }

        /// <summary>Initializes a new instance for a failed PVE API request, with an inner exception.</summary>
        /// <param name="statusCode">The HTTP status code returned.</param>
        /// <param name="message">The error message from the API.</param>
        /// <param name="resource">The API resource path that was requested.</param>
        /// <param name="httpMethod">The HTTP method used.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveApiException(HttpStatusCode statusCode, string message, string resource, string httpMethod, Exception innerException)
            : base($"PVE API error ({(int)statusCode} {statusCode}) on {httpMethod} {resource}: {message}", innerException)
        {
            StatusCode = statusCode;
            Resource = resource;
            HttpMethod = httpMethod;
        }
    }
}
