using System;
using System.Globalization;
using System.Net;
using PSProxmoxVE.Core.Exceptions;

namespace PSProxmoxVE.Core.Errors
{
    /// <summary>
    /// The error classifications this module reports. Every member name is also the name of a
    /// <c>System.Management.Automation.ErrorCategory</c> member, so the cmdlet layer can
    /// translate a kind without inventing a second vocabulary.
    /// </summary>
    public enum PveErrorKind
    {
        /// <summary>The failure was not recognised and carries no classification.</summary>
        NotSpecified,

        /// <summary>The caller lacks the privilege or the credential the operation needs.</summary>
        PermissionDenied,

        /// <summary>The credential was rejected because the session is no longer valid.</summary>
        AuthenticationError,

        /// <summary>The addressed object does not exist on the server.</summary>
        ObjectNotFound,

        /// <summary>The server rejected the request payload or a parameter value.</summary>
        InvalidArgument,

        /// <summary>The operation did not complete inside its time budget.</summary>
        OperationTimeout,

        /// <summary>The server could not be reached, or no session is established.</summary>
        ConnectionError,

        /// <summary>The server was reached but cannot service the request.</summary>
        ResourceUnavailable,

        /// <summary>The operation is not valid for the server or object in its current state.</summary>
        InvalidOperation,

        /// <summary>The operation started and then stopped without completing.</summary>
        OperationStopped,
    }

    /// <summary>The classification and error identifier derived from a failure.</summary>
    public sealed class PveErrorDescriptor
    {
        /// <summary>Initializes a new descriptor.</summary>
        /// <param name="kind">The classification of the failure.</param>
        /// <param name="errorId">The stable identifier a script can match on.</param>
        /// <param name="target">The object the failure is about, or null when none is known.</param>
        public PveErrorDescriptor(PveErrorKind kind, string errorId, object? target)
        {
            Kind = kind;
            ErrorId = errorId;
            Target = target;
        }

        /// <summary>The classification of the failure.</summary>
        public PveErrorKind Kind { get; }

        /// <summary>The stable identifier a script can match on, for example <c>PveApi.404.nodes/pve1/qemu/100</c>.</summary>
        public string ErrorId { get; }

        /// <summary>The object the failure is about, derived from the exception when the caller supplied none.</summary>
        public object? Target { get; }
    }

    /// <summary>Maps a module exception to the classification and identifier reported to PowerShell.</summary>
    public static class PveErrorMapper
    {
        private const string ApiErrorIdPrefix = "PveApi";

        /// <summary>
        /// Reports whether <paramref name="exception"/> is one this module classifies. An
        /// exception that is not recognised must reach the PowerShell engine untouched: it may
        /// carry its own error record, or be a flow-control or fatal exception.
        /// </summary>
        /// <param name="exception">The failure to test.</param>
        /// <returns>True when <see cref="Describe"/> classifies the exception.</returns>
        public static bool IsRecognized(Exception exception)
            => exception is PveApiException
                or PveNotConnectedException
                or PveSessionExpiredException
                or PveTaskFailedException
                or PveTaskTimeoutException
                or PveVersionException;

        /// <summary>Classifies <paramref name="exception"/> and derives its error identifier and target.</summary>
        /// <param name="exception">The failure to classify.</param>
        /// <returns>The classification, error identifier and derived target.</returns>
        public static PveErrorDescriptor Describe(Exception exception)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));

            switch (exception)
            {
                case PveApiException api:
                    return new PveErrorDescriptor(KindForStatus(api.StatusCode), ApiErrorId(api), NullIfBlank(api.Resource));

                case PveNotConnectedException:
                    return new PveErrorDescriptor(PveErrorKind.ConnectionError, "PveNotConnected", null);

                case PveSessionExpiredException:
                    return new PveErrorDescriptor(PveErrorKind.AuthenticationError, "PveSessionExpired", null);

                case PveTaskFailedException taskFailed:
                    return new PveErrorDescriptor(PveErrorKind.OperationStopped, "PveTaskFailed", NullIfBlank(taskFailed.Upid));

                case PveTaskTimeoutException taskTimeout:
                    return new PveErrorDescriptor(PveErrorKind.OperationTimeout, "PveTaskTimeout", NullIfBlank(taskTimeout.Upid));

                case PveVersionException:
                    return new PveErrorDescriptor(PveErrorKind.InvalidOperation, "PveVersionTooOld", null);

                default:
                    return new PveErrorDescriptor(PveErrorKind.NotSpecified, exception.GetType().Name, null);
            }
        }

        private static PveErrorKind KindForStatus(HttpStatusCode status)
        {
            switch (status)
            {
                case HttpStatusCode.BadRequest:
                    return PveErrorKind.InvalidArgument;

                // 401 is a rejected or expired ticket, which the module also reports as
                // PveSessionExpiredException; 403 is a privilege the ticket does not carry.
                case HttpStatusCode.Unauthorized:
                    return PveErrorKind.AuthenticationError;

                case HttpStatusCode.Forbidden:
                    return PveErrorKind.PermissionDenied;

                case HttpStatusCode.NotFound:
                    return PveErrorKind.ObjectNotFound;

                case HttpStatusCode.RequestTimeout:
                case HttpStatusCode.GatewayTimeout:
                    return PveErrorKind.OperationTimeout;

                // PveHttpClient reports a failed connection as 503, so 503 is a reachability
                // failure here and not the server's own "try again later".
                case HttpStatusCode.ServiceUnavailable:
                    return PveErrorKind.ConnectionError;
            }

            var code = (int)status;
            if (code >= 500 && code <= 599) return PveErrorKind.ResourceUnavailable;
            return PveErrorKind.InvalidOperation;
        }

        private static string ApiErrorId(PveApiException exception)
        {
            var status = ((int)exception.StatusCode).ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(exception.Resource)
                ? $"{ApiErrorIdPrefix}.{status}"
                : $"{ApiErrorIdPrefix}.{status}.{exception.Resource}";
        }

        private static object? NullIfBlank(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
