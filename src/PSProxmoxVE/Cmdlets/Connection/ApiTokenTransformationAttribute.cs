using System;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Security;

namespace PSProxmoxVE.Cmdlets.Connection
{
    /// <summary>
    /// Converts a plain <see cref="string"/> API token argument to a <see cref="SecureString"/> so
    /// that scripts written against the pre-SecureString parameter keep binding for one minor
    /// release. A <see cref="SecureString"/> argument is returned unchanged.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class ApiTokenTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// The SecureStrings this attribute built from a plain string, held weakly so an argument
        /// bound by an invocation that never reached its cmdlet is not kept alive by this table.
        /// </summary>
        private static readonly ConditionalWeakTable<SecureString, object> ConvertedFromString =
            new ConditionalWeakTable<SecureString, object>();

        private static readonly object Marker = new object();

        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            var value = inputData is PSObject wrapper ? wrapper.BaseObject : inputData;

            if (value is SecureString secure)
                return secure;

            if (value is string plain)
            {
                var converted = new SecureString();
                foreach (var c in plain)
                    converted.AppendChar(c);
                converted.MakeReadOnly();
                ConvertedFromString.Add(converted, Marker);
                return converted;
            }

            return inputData;
        }

        /// <summary>
        /// Reports whether <paramref name="value"/> is a SecureString this attribute built from a
        /// plain string argument, and forgets it either way. A true answer also means the module
        /// owns the instance and may dispose it.
        /// </summary>
        internal static bool WasConvertedFromString(SecureString? value)
        {
            if (value == null || !ConvertedFromString.TryGetValue(value, out _))
                return false;

            ConvertedFromString.Remove(value);
            return true;
        }
    }
}
