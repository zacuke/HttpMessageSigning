using System;

namespace Dalion.HttpMessageSigning.SigningString {
    internal class RequestTargetHeaderAppender : IHeaderAppender {
        private readonly HttpRequestForSigning _request;
        
        public RequestTargetHeaderAppender(HttpRequestForSigning request) {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public string BuildStringToAppend(HeaderName header) {
            if (!_request.RequestUri.IsAbsoluteUri) throw new ValidationException("Cannot sign a request that uses a relative uri.");
            
            return "\n" + new Header(
                       HeaderName.PredefinedHeaderNames.RequestTarget,
                       $"{_request.Method.Method.ToLowerInvariant()} {_request.RequestUri.LocalPath}");
        }
    }
}