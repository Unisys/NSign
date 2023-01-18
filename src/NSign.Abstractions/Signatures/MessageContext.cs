using Microsoft.Extensions.Logging;
using NSign.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NSign.Signatures
{
    /// <summary>
    /// Defines the contract for a context of a HTTP message for signing and verification.
    /// </summary>
    public abstract partial class MessageContext
    {
        #region Fields

        /// <summary>
        /// A <see cref="Signatures"/> object that tracks signatures (and signature input) from request and response
        /// messages (if available).
        /// </summary>
        private readonly Signatures signatures;

        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="MessageContext"/>.
        /// </summary>
        /// <param name="logger">
        /// Gets the <see cref="ILogger"/> to use.
        /// </param>
        /// <param name="httpFieldOptions">
        /// The <see cref="HttpFieldOptions"/> that should be used for both signing messages and verifying message
        /// signatures.
        /// </param>
        public MessageContext(ILogger logger, HttpFieldOptions httpFieldOptions)
        {
            signatures = new Signatures(this);
            Logger = logger;
            HttpFieldOptions = httpFieldOptions;
        }

        #region Public Interface

        /// <summary>
        /// The <see cref="HttpFieldOptions"/> that should be used for both signing messages and verifying message
        /// signatures.
        /// </summary>
        public HttpFieldOptions HttpFieldOptions { get; }

        /// <summary>
        /// The <see cref="MessageSigningOptions"/> that should be used for signing messages. The default implementation
        /// always returns null, so it is up to derived classes to override if signing is needed.
        /// </summary>
        /// <remarks>
        /// Can be null only if signing is not needed for the message in this context.
        /// </remarks>
        public virtual MessageSigningOptions? SigningOptions => null;

        /// <summary>
        /// The <see cref="SignatureVerificationOptions"/> that should be used for verifying message signatures. The
        /// default implementation always returns null, so it is up to derived classes to override if signature
        /// verification is needed.
        /// </summary>
        /// <remarks>
        /// Can be null only if signature verification is not needed for the message in this context.
        /// </remarks>
        public virtual SignatureVerificationOptions? VerificationOptions => null;

        /// <summary>
        /// Gets a flag which indicates whether or not this context has a response message too.
        /// </summary>
        public abstract bool HasResponse { get; }

        /// <summary>
        /// Gets a CancellationToken value that tracks when/if the message pipeline is aborted.
        /// </summary>
        public abstract CancellationToken Aborted { get; }

        /// <summary>
        /// Checks if the given <paramref name="component"/> is available on the message.
        /// </summary>
        /// <param name="component">
        /// A <see cref="SignatureComponent"/> object that describes the component to check.
        /// </param>
        /// <returns>
        /// True if the component is available (and can be used in signatures), false otherwise.
        /// </returns>
        public bool HasSignatureComponent(SignatureComponent component)
        {
            EnsureComponentIsAllowed(component);

            InputCheckingVisitor visitor = new InputCheckingVisitor(this);

            component.Accept(visitor);

            return visitor.Found;
        }

        /// <summary>
        /// Gets <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> representing the signature input (the signature
        /// base) for signing and verification.
        /// </summary>
        /// <param name="signatureParams">
        /// The <see cref="SignatureParamsComponent"/> value that specifies the different parts to include in the input.
        /// </param>
        /// <param name="signatureParamsValue">
        /// A string that receives the representation of the '@signature-params' component for the given
        /// <paramref name="signatureParams"/> when the method returns.
        /// </param>
        /// <returns>
        /// A <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> representing the signature input.
        /// </returns>
        public ReadOnlyMemory<byte> GetSignatureInput(
            SignatureParamsComponent signatureParams,
            out string signatureParamsValue)
        {
            InputBuildingVisitor visitor = new InputBuildingVisitor(this);

            signatureParams.Accept(visitor);
            signatureParamsValue = visitor.SignatureParamsValue!;

            return Encoding.ASCII.GetBytes(visitor.SignatureInput);
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> of <see cref="SignatureContext"/> values that represent signatures that
        /// can be verified.
        /// </summary>
        public IEnumerable<SignatureContext> SignaturesForVerification
        {
            get
            {
                if (HasResponse)
                {
                    return signatures.ResponseSignatures;
                }
                else
                {
                    return signatures.RequestSignatures;
                }
            }
        }

        /// <summary>
        /// Gets the signature of the given <paramref name="key"/> from the request message.
        /// </summary>
        /// <param name="key">
        /// The name of the signature to get.
        /// </param>
        /// <returns>
        /// A nullable <see cref="SignatureContext"/> that holds the data for the given signature or null if a signature
        /// by that key was not found.
        /// </returns>
        public SignatureContext? GetRequestSignature(string key)
        {
            if (signatures.TryGetRequestSignature(key, out SignatureContext sigContext))
            {
                return sigContext;
            }

            return null;
        }

        /// <summary>
        /// Gets the signature of the given <paramref name="key"/> from the response message.
        /// </summary>
        /// <param name="key">
        /// The name of the signature to get.
        /// </param>
        /// <returns>
        /// A nullable <see cref="SignatureContext"/> that holds the data for the given signature or null if a signature
        /// by that key was not found.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when a response message is not available.
        /// </exception>
        public SignatureContext? GetResponseSignature(string key)
        {
            if (!HasResponse)
            {
                throw new NotSupportedException("Cannot get response signatures if no response is available.");
            }

            if (signatures.TryGetResponseSignature(key, out SignatureContext sigContext))
            {
                return sigContext;
            }

            return null;
        }

        /// <summary>
        /// Gets the HTTP header values for the header with the given <paramref name="headerName"/> from the message.
        /// </summary>
        /// <param name="headerName">
        /// The name of the header to retrieve.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="String"/> that can be used to enumerate the header values.
        /// If the header does not exist, the enumerable is empty.
        /// </returns>
        public abstract IEnumerable<string> GetHeaderValues(string headerName);

        /// <summary>
        /// Gets the HTTP header values for the header with the given <paramref name="headerName"/> from the request
        /// message.
        /// </summary>
        /// <param name="headerName">
        /// The name of the header to retrieve.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="String"/> that can be used to enumerate the request header
        /// values. If the header does not exist, the enumerable is empty.
        /// </returns>
        public abstract IEnumerable<string> GetRequestHeaderValues(string headerName);

        /// <summary>
        /// Gets the HTTP trailer values for the field with the given <paramref name="fieldName"/> from the message.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the field to retrieve.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="String"/> that can be used to enumerate the field values.
        /// If the field does not exist in the trailer, the enumerable is empty.
        /// </returns>
        public abstract IEnumerable<string> GetTrailerValues(string fieldName);

        /// <summary>
        /// Gets the HTTP trailer values for the field with the given <paramref name="fieldName"/> from the request
        /// message.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the field to retrieve.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="String"/> that can be used to enumerate the request field
        /// values. If the field does not exist, the enumerable is empty.
        /// </returns>
        public abstract IEnumerable<string> GetRequestTrailerValues(string fieldName);

        /// <summary>
        /// Gets the value of a simple derived components. Structured dictionary value components are not supported.
        /// </summary>
        /// <param name="component">
        /// The <see cref="DerivedComponent"/> for which to get the value.
        /// </param>
        /// <returns>
        /// A string that represents the value or null if the component does not exist.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown if the <paramref name="component"/> is not supported.
        /// </exception>
        /// <remarks>
        /// Implementations must always return at least the '?' for the '@query' component.
        /// </remarks>
        public abstract string? GetDerivedComponentValue(DerivedComponent component);

        /// <summary>
        /// Gets the values of the query parameter with the given <paramref name="paramName"/>.
        /// </summary>
        /// <param name="paramName">
        /// The name of the query parameter for which to get the values.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="String"/> that represents the different values of the given
        /// <paramref name="paramName"/>. Empty values are represented as empty strings, and the absence of any values
        /// for the parameter is represented by an empty enumerable.
        /// </returns>
        public abstract IEnumerable<string> GetQueryParamValues(string paramName);

        /// <summary>
        /// Checks if a header with the given <paramref name="headerName"/> is available in the message.
        /// </summary>
        /// <param name="bindRequest">
        /// Whether or not the header's existence should checked on the request message (as opposed to the message from
        /// the context).
        /// </param>
        /// <param name="headerName">
        /// The name of the header to check.
        /// </param>
        /// <returns>
        /// True if the header exists, or false otherwise.
        /// </returns>
        public virtual bool HasHeader(bool bindRequest, string headerName)
        {
            if (bindRequest)
            {
                return GetRequestHeaderValues(headerName).Any();
            }
            else
            {
                return GetHeaderValues(headerName).Any();
            }
        }

        /// <summary>
        /// Checks if a trailer with the given <paramref name="fieldName"/> is available in the message.
        /// </summary>
        /// <param name="bindRequest">
        /// Whether or not the field's existence should checked on the request message (as opposed to the message from
        /// the context).
        /// </param>
        /// <param name="fieldName">
        /// The name of the field to check in the trailers.
        /// </param>
        /// <returns>
        /// True if the trailer exists, or false otherwise.
        /// </returns>
        public virtual bool HasTrailer(bool bindRequest, string fieldName)
        {
            if (bindRequest)
            {
                return GetRequestTrailerValues(fieldName).Any();
            }
            else
            {
                return GetTrailerValues(fieldName).Any();
            }
        }

        /// <summary>
        /// Checks if the given <paramref name="component"/> exists in the message context.
        /// </summary>
        /// <param name="component">
        /// A <see cref="DerivedComponent"/> object that describes the derived component to check.
        /// </param>
        /// <returns>
        /// True if the derived component is available (and can be used in signatures), false otherwise.
        /// </returns>
        public virtual bool HasDerivedComponent(DerivedComponent component)
        {
            if (component is QueryParamComponent queryParam)
            {
                return HasQueryParam(queryParam.Name);
            }

            if (HasResponse)
            {
                return true;
            }
            else
            {
                switch (component.ComponentName)
                {
                    case Constants.DerivedComponents.Status:
                        return false;

                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Checks if a query parameter with the given <paramref name="paramName"/> is available on the request URL.
        /// </summary>
        /// <param name="paramName">
        /// The name of the query parameter to check.
        /// </param>
        /// <returns>
        /// True if the query parameter exists, false otherwise.
        /// </returns>
        public virtual bool HasQueryParam(string paramName)
        {
            return GetQueryParamValues(paramName).Any();
        }

        /// <summary>
        /// Gets a flag which indicates whether or not there are any signatures that can be verified on the message.
        /// </summary>
        public bool HasSignaturesForVerification
        {
            get
            {
                if (HasResponse)
                {
                    return signatures.HasResponseSignatures;
                }
                else
                {
                    return signatures.HasRequestSignatures;
                }
            }
        }

        /// <summary>
        /// Adds a new header to the message.
        /// </summary>
        /// <param name="headerName">
        /// The name of the header to add.
        /// </param>
        /// <param name="value">
        /// The value of the header to add.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// Thrown when adding headers to messages is not supported by the context.
        /// </exception>
        public abstract void AddHeader(string headerName, string value);

        #endregion

        #region Internal Interface

        /// <summary>
        /// Ensures that the given component is allowed on the HTTP message described by this context. In particular,
        /// verifies that the <see cref="SignatureComponent.BindRequest"/> is not set when the message is a request
        /// message. If the component is not allowed, an exception is thrown.
        /// </summary>
        /// <param name="component">
        /// The <see cref="SignatureComponent"/> that should be checked.
        /// </param>
        /// <exception cref="Exception"></exception>
        internal void EnsureComponentIsAllowed(SignatureComponent component)
        {
            if (component.BindRequest)
            {
                if (!HasResponse)
                {
                    // This context is for a request message, but request-response binding is not allowed for request
                    // messages.
                    throw new SignatureComponentNotAllowedException(
                        $"Cannot use {nameof(component.BindRequest)} for request message signature components.", component);
                }
                else if (StringComparer.Ordinal.Equals(Constants.DerivedComponents.Status, component.ComponentName))
                {
                    // The '@status' derived component can never be bound to the request because the request doesn't
                    // have a status code.
                    throw new SignatureComponentNotAllowedException(
                        $"Cannot use '@status' with request-response binding.", component);
                }
            }
        }

        #endregion

        #region Protected Interface

        /// <summary>
        /// Gets the <see cref="ILogger"/> to use.
        /// </summary>
        internal protected ILogger Logger { get; }

        #endregion
    }
}
