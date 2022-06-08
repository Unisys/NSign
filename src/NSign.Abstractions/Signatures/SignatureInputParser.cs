using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Ref struct that helps parse signature input specs and values of the '@signature-params' component.
    /// </summary>
    internal ref partial struct SignatureInputParser
    {
        /// <summary>
        /// The basic set of component parameters supported by all components that support parameters. This includes
        /// only the <c>req</c> parameter used for request-binding of a component.
        /// </summary>
        private static readonly ImmutableHashSet<string> RequestTargetSupportedParameters = ImmutableHashSet<string>.Empty
            .WithComparer(StringComparer.Ordinal)
            .Add(Constants.ComponentParameters.Request);

        /// <summary>
        /// The set of component parameters supported by structured HTTP header field components. This includes the <c>sf</c>
        /// and the <c>req</c> parameters.
        /// </summary>
        private static readonly ImmutableHashSet<string> StucturedFieldSupportedParameters = RequestTargetSupportedParameters
            .Add(Constants.ComponentParameters.StructuredField);

        /// <summary>
        /// The set of component parameters supported by HTTP header components. This includes the <c>key</c> and <c>sf</c>
        /// parameters for structured header values, in addition to the <c>req</c> parameter.
        /// </summary>
        private static readonly ImmutableHashSet<string> HttpHeaderSupportedParameters = StucturedFieldSupportedParameters
            .Add(Constants.ComponentParameters.Key);

        /// <summary>
        /// The set of component parameters supported by the <c>@query-param</c> derived component. Aside from the <c>req</c>
        /// parameter this also includes the <c>name</c> parameter.
        /// </summary>
        private static readonly ImmutableHashSet<string> QueryParamSupportedParameters = RequestTargetSupportedParameters
            .Add(Constants.ComponentParameters.Name);

        /// <summary>
        /// The SignatureParamsComponent object to update with the results from the parser.
        /// </summary>
        private readonly SignatureParamsComponent signatureParams;

        /// <summary>
        /// Initializes a new instance of SignatureInputParser.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object to update with the results from the parser.
        /// </param>
        private SignatureInputParser(SignatureParamsComponent signatureParams)
        {
            Debug.Assert(null != signatureParams, "The signature params must not be null.");

            this.signatureParams = signatureParams;
        }

        /// <summary>
        /// Parses the given input string and updates the specified SignatureParamsComponent object with the results.
        /// </summary>
        /// <param name="input">
        /// The input string to parse.
        /// </param>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object to update with the results from the parser.
        /// </param>
        public static void ParseAndUpdate(string? input, SignatureParamsComponent? signatureParams)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (null == signatureParams)
            {
                throw new ArgumentNullException(nameof(signatureParams));
            }

            Tokenizer tokenizer = new Tokenizer(input);
            new SignatureInputParser(signatureParams).ParseAndUpdate(ref tokenizer);
        }

        #region Parsing

        /// <summary>
        /// Starts parsing the input and updating the signatureParams object.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdate(ref Tokenizer tokenizer)
        {
            // We expect the component list first ...
            ParseAndUpdateComponentList(ref tokenizer);
            Debug.Assert(TokenType.CloseParenthesis == tokenizer.Token.Type, "The current token must be a closing parenthesis.");

            // ... followed by the parameters.
            ParseAndUpdateParameters(ref tokenizer);
        }

        /// <summary>
        /// Parses the component list from the input and updates the signatureParams object.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateComponentList(ref Tokenizer tokenizer)
        {
            // The component list must start with an open parenthesis.
            EnsureNextToken(ref tokenizer, TokenType.OpenParenthesis);

            // The next token must either be the close parenthesis token yielding an empty component list, or a quoted string.
            EnsureNextToken(ref tokenizer, TokenType.CloseParenthesis | TokenType.QuotedString);

            while (TokenType.CloseParenthesis != tokenizer.Token.Type)
            {
                ParseAndUpdateComponent(ref tokenizer);
            }
        }

        /// <summary>
        /// Parses and updates a single component in the component list from the input and updates the signatureParams
        /// object accordingly.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateComponent(ref Tokenizer tokenizer)
        {
            if (signatureParams.Components.Count > 0)
            {
                // We've already verified that we have a whitespace at the end of the previous component parsing.
                // After the whitespace we must get another quoted string for the component name.
                EnsureNextToken(ref tokenizer, TokenType.QuotedString);
            }
            else
            {
                tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
            }

            int origStringStart = tokenizer.LastPosition;

            ReadOnlySpan<char> componentNameSpan = tokenizer.Token.Value;
            if (componentNameSpan.Length <= 0)
            {
                throw new SignatureInputException("Component names must not be empty.");
            }

            // We now need either a close parenthesis token (to indicate the end of the component list), a semicolon
            // (to indicate a parameter to a component) or a whitespace (to indicate more components to follow).
            EnsureNextToken(ref tokenizer, TokenType.CloseParenthesis | TokenType.Semicolon | TokenType.Whitespace);

            string componentName = new String(componentNameSpan);
            IReadOnlyList<KeyValuePair<string, string?>> componentParams = ParseOptionalComponentParameters(ref tokenizer);
            int origStringEnd = tokenizer.LastPosition;
            ReadOnlySpan<char> originalIdentifier = tokenizer.Input[origStringStart..origStringEnd];

            if (componentNameSpan[0] == '@')
            {
                AddDerivedComponent(componentName, originalIdentifier, componentParams);
            }
            else
            {
                AddHttpHeaderComponent(componentName, originalIdentifier, componentParams);
            }
        }

        /// <summary>
        /// Parses the optional parameter list for a component in the component list.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        /// <returns>
        /// An instance of <see cref="IReadOnlyList{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/>
        /// and nullable <see cref="string"/> tracking all the parameters and their optional values that were found for
        /// the component. If the component does not have any parameters, the list will be empty.
        /// </returns>
        private IReadOnlyList<KeyValuePair<string, string?>> ParseOptionalComponentParameters(ref Tokenizer tokenizer)
        {
            ImmutableList<KeyValuePair<string, string?>> parameters = ImmutableList<KeyValuePair<string, string?>>.Empty;

            while (TokenType.Semicolon == tokenizer.Token.Type)
            {
                KeyValuePair<string, string?> parameter = ParseComponentParameter(ref tokenizer);
                parameters = parameters.Add(parameter);
            }

            return parameters;
        }

        /// <summary>
        /// Parses a single parameter to a component in the component list.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        /// <returns>
        /// A <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/> and nullable <see cref="string"/>
        /// representing the parsed parameter name and value.
        /// </returns>
        private KeyValuePair<string, string?> ParseComponentParameter(ref Tokenizer tokenizer)
        {
            tokenizer.EnsureTokenOneOfOrThrow(TokenType.Semicolon);

            // The semicolon must be followed by an identifier representing the name of the parameter.
            EnsureNextToken(ref tokenizer, TokenType.Identifier);
            string name = new String(tokenizer.Token.Value);
            string? value = null;

            // The parameter name must be followed either by the equal sign (in case there is a value), a semicolon
            // (in case there's another parameter), a closing parenthesis (in case we're at the end of the component
            // list) or a whitespace (in case there are more components in the list).
            EnsureNextToken(ref tokenizer,
                            TokenType.Equal | TokenType.Semicolon | TokenType.CloseParenthesis | TokenType.Whitespace);

            if (tokenizer.Token.Type == TokenType.Equal)
            {
                // We got a parameter with a value, so parse the value. At this point, the only supported values are
                // quoted strings though.
                EnsureNextToken(ref tokenizer, TokenType.QuotedString);
                value = new String(tokenizer.Token.Value);

                // We need to move to the next token because that's expected by the caller and it's necessary to then
                // allow parsing the next parameter (if any), the next component in the list (if any) or the end of the
                // list.
                EnsureNextToken(ref tokenizer, TokenType.Semicolon | TokenType.CloseParenthesis | TokenType.Whitespace);
            }

            return new KeyValuePair<string, string?>(name, value);
        }

        /// <summary>
        /// Adds the given derived component to the component list of the signatureParams.
        /// </summary>
        /// <param name="componentName">
        /// The name of the component to add.
        /// </param>
        /// <param name="originalIdentifier">
        /// A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> that represents the original identifier used for this
        /// derived component.
        /// </param>
        /// <param name="componentParams">
        /// The optional parameter of the component to add.
        /// </param>
        private void AddDerivedComponent(
            string componentName,
            ReadOnlySpan<char> originalIdentifier,
            IReadOnlyList<KeyValuePair<string, string?>> componentParams)
        {
            TryGetParameterValue(componentParams, Constants.ComponentParameters.Request, out bool bindRequest);

            switch (componentName)
            {
                case Constants.DerivedComponents.QueryParam:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, QueryParamSupportedParameters);

                    if (!TryGetParameterValue(componentParams, Constants.ComponentParameters.Name, out string name))
                    {
                        throw new SignatureInputException("The @query-param component requires the 'name' parameter.");
                    }
                    signatureParams.AddComponent(new QueryParamComponent(name, bindRequest));
                    break;

                // Handle known cases without parameters too, so we can reuse existing components rather than creating more.
                case Constants.DerivedComponents.Authority:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundAuthority : SignatureComponent.Authority);
                    break;

                case Constants.DerivedComponents.Method:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundMethod : SignatureComponent.Method);
                    break;
                case Constants.DerivedComponents.Path:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundPath : SignatureComponent.Path);
                    break;
                case Constants.DerivedComponents.Query:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundQuery : SignatureComponent.Query);
                    break;
                case Constants.DerivedComponents.RequestTarget:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundRequestTarget : SignatureComponent.RequestTarget);
                    break;
                case Constants.DerivedComponents.Scheme:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundScheme : SignatureComponent.Scheme);
                    break;
                case Constants.DerivedComponents.TargetUri:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, RequestTargetSupportedParameters);
                    signatureParams.AddComponent(
                        bindRequest ? SignatureComponent.RequestBoundRequestTargetUri : SignatureComponent.RequestTargetUri);
                    break;

                case Constants.DerivedComponents.Status:
                    ThrowForUnsupportedParameters(originalIdentifier, componentParams, ImmutableHashSet<string>.Empty);
                    signatureParams.AddComponent(SignatureComponent.Status);
                    break;

                case Constants.DerivedComponents.SignatureParams:
                    throw new SignatureInputException("The @signature-params component is not allowed.");

                default:
                    signatureParams.AddComponent(new DerivedComponent(componentName, bindRequest));
                    break;
            }
        }

        /// <summary>
        /// Adds the given HTTP message header component to the component list of the signatureParams.
        /// </summary>
        /// <param name="componentName">
        /// The name of the HTTP message header component to add.
        /// </param>
        /// <param name="originalIdentifier">
        /// A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> that represents the original identifier used for this
        /// HTTP header component.
        /// </param>
        /// <param name="componentParams">
        /// The optional parameter of the component to add.
        /// </param>
        private void AddHttpHeaderComponent(
            string componentName,
            ReadOnlySpan<char> originalIdentifier,
            IReadOnlyList<KeyValuePair<string, string?>> componentParams)
        {
            ThrowForUnsupportedParameters(originalIdentifier, componentParams, HttpHeaderSupportedParameters);
            TryGetParameterValue(componentParams, Constants.ComponentParameters.Request, out bool bindRequest);
            TryGetParameterValue(componentParams, Constants.ComponentParameters.StructuredField, out bool structuredField);

            string original = new String(originalIdentifier);

            if (structuredField)
            {
                // If the 'sf' (structured field) parameter is present, the 'key' parameter must not be present.
                ThrowForUnsupportedParameters(originalIdentifier, componentParams, StucturedFieldSupportedParameters);
                signatureParams.AddComponent(new HttpHeaderStructuredFieldComponent(componentName, bindRequest)
                {
                    OriginalIdentifier = original,
                });
            }
            else if (TryGetParameterValue(componentParams, Constants.ComponentParameters.Key, out string key))
            {
                signatureParams.AddComponent(new HttpHeaderDictionaryStructuredComponent(componentName, key, bindRequest)
                {
                    OriginalIdentifier = original,
                });
            }
            else
            {
                signatureParams.AddComponent(new HttpHeaderComponent(componentName, bindRequest)
                {
                    OriginalIdentifier = original,
                });
            }
        }

        /// <summary>
        /// Parses and updates the signature input's parameters and updates the signatureParams accordingly.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateParameters(ref Tokenizer tokenizer)
        {
            while (tokenizer.Next())
            {
                tokenizer.EnsureTokenOneOfOrThrow(TokenType.Semicolon);

                ParseAndUpdateParameter(ref tokenizer);
            }
        }

        /// <summary>
        /// Parses and updates a single signature input parameter and udpates the signatureParams accordingly.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateParameter(ref Tokenizer tokenizer)
        {
            // The parameter name must be an identifier ...
            EnsureNextToken(ref tokenizer, TokenType.Identifier);
            string paramName = new String(tokenizer.Token.Value);

            // ... and must be followed by the equal sign and a quoted string or integer token.
            EnsureNextToken(ref tokenizer, TokenType.Equal);
            EnsureNextToken(ref tokenizer, TokenType.QuotedString | TokenType.Integer);

            switch (paramName)
            {
                case Constants.SignatureParams.Created:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.Integer);
                    signatureParams.Created = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(tokenizer.Token.Value));
                    break;

                case Constants.SignatureParams.Expires:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.Integer);
                    signatureParams.Expires = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(tokenizer.Token.Value));
                    break;

                case Constants.SignatureParams.Nonce:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
                    signatureParams.Nonce = new String(tokenizer.Token.Value);
                    break;

                case Constants.SignatureParams.Alg:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
                    signatureParams.Algorithm = new String(tokenizer.Token.Value);
                    break;

                case Constants.SignatureParams.KeyId:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
                    signatureParams.KeyId = new String(tokenizer.Token.Value);
                    break;

                default:
                    throw new SignatureInputException(
                        $"Unsupported signature input parameter: {paramName} with value '{new String(tokenizer.Token.Value)}'.");
            }
        }

        #endregion

        /// <summary>
        /// Ensures that there is a next token and that it is one of the expectedTypes.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        /// <param name="expectedTypes">
        /// A TokenType value that defines the expected / allowed token types.
        /// </param>
        /// <exception cref="SignatureInputParserException">
        /// A SignatureInputParserException is thrown if the tokenizer cannot present any tokens anymore or if it presents
        /// an unexpected token.
        /// </exception>
        private static void EnsureNextToken(ref Tokenizer tokenizer, TokenType expectedTypes)
        {
            if (!tokenizer.Next() || !tokenizer.Token.IsTypeOneOf(expectedTypes))
            {
                throw new SignatureInputParserException(expectedTypes, tokenizer);
            }
        }

        /// <summary>
        /// Tries to get a string parameter value for a named parameter in a component's parameter list.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="IReadOnlyList{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/> and
        /// nullable <see cref="string"/> tracking all the parameters and their optional values that were found for the
        /// component.
        /// </param>
        /// <param name="name">
        /// The name of the parameter to look for.
        /// </param>
        /// <param name="value">
        /// If the parameter was found, will hold the parameter's string value.
        /// </param>
        /// <returns>
        /// True if the parameter was found, or false otherwise.
        /// </returns>
        private static bool TryGetParameterValue(IReadOnlyList<KeyValuePair<string, string?>> parameters, string name, out string value)
        {
            foreach (KeyValuePair<string, string?> parameter in parameters)
            {
                if (StringComparer.Ordinal.Equals(name, parameter.Key) && null != parameter.Value)
                {
                    value = parameter.Value;
                    return true;
                }
            }

            value = String.Empty;
            return false;
        }

        /// <summary>
        /// Tries to get a boolean parameter value for a named parameter in a component's parameter list.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="IReadOnlyList{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/> and
        /// nullable <see cref="string"/> tracking all the parameters and their optional values that were found for the
        /// component.
        /// </param>
        /// <param name="name">
        /// The name of the parameter to look for.
        /// </param>
        /// <param name="value">
        /// True if the parameter was present and didn't have any value. False in every other case.
        /// </param>
        /// <returns>
        /// True if the parameter was found, or false otherwise.
        /// </returns>
        private static bool TryGetParameterValue(IReadOnlyList<KeyValuePair<string, string?>> parameters, string name, out bool value)
        {
            foreach (KeyValuePair<string, string?> parameter in parameters)
            {
                if (StringComparer.Ordinal.Equals(name, parameter.Key) && null == parameter.Value)
                {
                    // Value-less parameters in structured fields do not have a value (doh); their presence implies 'true'.
                    value = true;
                    return true;
                }
            }

            value = false;
            return false;
        }

        /// <summary>
        /// Throw an <see cref="SignatureInputException"/> if any parameter was present that is not supported by the
        /// target component.
        /// </summary>
        /// <param name="componentIdentifier">
        /// A <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> that represents the full component identifier including
        /// parameters.
        /// </param>
        /// <param name="parameters">
        /// The list of parameters parsed for the component.
        /// </param>
        /// <param name="supportedParameterNames">
        /// A set of parameter names that are supported for the component.
        /// </param>
        /// <exception cref="SignatureInputException">
        /// Thrown for the first unsupported parameter that was found.
        /// </exception>
        private static void ThrowForUnsupportedParameters(
            ReadOnlySpan<char> componentIdentifier,
            IReadOnlyList<KeyValuePair<string, string?>> parameters,
            IImmutableSet<string> supportedParameterNames
            )
        {
            foreach (KeyValuePair<string, string?> parameter in parameters)
            {
                if (!supportedParameterNames.Contains(parameter.Key))
                {
                    throw new SignatureInputException(
                        $"The component '{new String(componentIdentifier)}' has unsupported parameter '{parameter.Key}'.");
                }
            }
        }
    }
}
