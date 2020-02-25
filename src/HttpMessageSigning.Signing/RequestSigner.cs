using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dalion.HttpMessageSigning.Logging;

namespace Dalion.HttpMessageSigning.Signing {
    internal class RequestSigner : IRequestSigner {
        private const string AuthorizationScheme = "Signature";
        
        private readonly ISignatureCreator _signatureCreator;
        private readonly IAuthorizationHeaderParamCreator _authorizationHeaderParamCreator;
        private readonly SigningSettings _signingSettings;
        private readonly ISignatureHeaderEnsurer _dateHeaderEnsurer;
        private readonly ISignatureHeaderEnsurer _digestHeaderEnsurer;
        private readonly ISystemClock _systemClock;
        private readonly IHttpMessageSigningLogger<RequestSigner> _logger;

        public RequestSigner(
            ISignatureCreator signatureCreator,
            IAuthorizationHeaderParamCreator authorizationHeaderParamCreator,
            SigningSettings signingSettings,
            ISignatureHeaderEnsurer dateHeaderEnsurer, 
            ISignatureHeaderEnsurer digestHeaderEnsurer,
            ISystemClock systemClock,
            IHttpMessageSigningLogger<RequestSigner> logger) {
            _signatureCreator = signatureCreator ?? throw new ArgumentNullException(nameof(signatureCreator));
            _authorizationHeaderParamCreator = authorizationHeaderParamCreator ?? throw new ArgumentNullException(nameof(authorizationHeaderParamCreator));
            _signingSettings = signingSettings ?? throw new ArgumentNullException(nameof(signingSettings));
            _dateHeaderEnsurer = dateHeaderEnsurer ?? throw new ArgumentNullException(nameof(dateHeaderEnsurer));
            _digestHeaderEnsurer = digestHeaderEnsurer ?? throw new ArgumentNullException(nameof(digestHeaderEnsurer));
            _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Sign(HttpRequestMessage request) {
            try {
                if (request == null) throw new ArgumentNullException(nameof(request));
                
                _signingSettings.Validate();

                var timeOfSigning = _systemClock.UtcNow;
                await _dateHeaderEnsurer.EnsureHeader(request, _signingSettings, timeOfSigning);
                await _digestHeaderEnsurer.EnsureHeader(request, _signingSettings, timeOfSigning);
                
                var signature = _signatureCreator.CreateSignature(request, _signingSettings, timeOfSigning);
                var authParam = _authorizationHeaderParamCreator.CreateParam(signature);

                _logger.Debug("Setting Authorization scheme to '{0}' and param to '{1}'.", AuthorizationScheme, authParam);

                request.Headers.Authorization = new AuthenticationHeaderValue(AuthorizationScheme, authParam);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Could not sign the specified request. See inner exception.");
                throw;
            }
        }
    }
}