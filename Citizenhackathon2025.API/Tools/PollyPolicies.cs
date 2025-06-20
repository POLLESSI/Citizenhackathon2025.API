﻿using Polly;
using Polly.Retry;
using Polly.Wrap;
using System;
using System.Net.Http;

namespace CitizenHackathon2025.API.Tools
{
    public static class PollyPolicies
    {
        public static AsyncPolicyWrap<HttpResponseMessage> GetResiliencePolicy()
        {
            // Retry 3 fois avec un délai exponentiel
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            // Circuit breaker : ouvre le circuit après 5 échecs
            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.