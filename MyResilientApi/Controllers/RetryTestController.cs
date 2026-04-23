using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.CircuitBreaker; //opg3
using Polly.Retry; //opg1
using Polly.Timeout; //opg2

namespace MyResilientApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RetryTestController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

        public RetryTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();

            // Konfigurerer ResiliencePipeline med Retry
            _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                //Opg 2 - Timeout på 5 sekunder
                .AddTimeout(TimeSpan.FromSeconds(5))
                //Opg 1 - retry
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    // Definerer hvornår der skal prøves igen (ved fejl-statuskoder eller netværksfejl)
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2), //pause på 2 sek
                    BackoffType = DelayBackoffType.Constant //holder pausen fast
                })
                //Opg 3 - Circuit breaker
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                    FailureRatio = 0.5, // Åbner kredsløbet hvis 50% af de sidste kald fejler
                    BreakDuration = TimeSpan.FromSeconds(5), // Hvor længe kredsløbet forbliver åbent før det prøver igen
                    MinimumThroughput = 2 //Evaluering efter min. 2 kald
                })
                .Build();
        }

        //opg 1 - Test endpoint der demonstrerer retry mekanismen
        [HttpGet("test-retry")]
        public async Task<IActionResult> TestRetry(CancellationToken cancellationToken)
        {
            // Udfører HTTP-kaldet igennem pipelinen
            var response = await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                return await _httpClient.GetAsync("https://localhost:7181/api/fault", ct);
            }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Ok($"Kaldet lykkedes: {content}");
            }
            return StatusCode((int)response.StatusCode, "Fejl efter alle forsøg var brugt.");
        }

        //opg 2 - Test endpoint der demonstrerer timeout mekanismen
        [HttpGet("test-timeout")]
        public async Task<IActionResult> TestTimeout(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _resiliencePipeline.ExecuteAsync(async ct =>
                {
                    // Kalder endpointet der bevidst hænger (aldrig svarer)
                    return await _httpClient.GetAsync("https://localhost:7181/api/fault/timeout", ct);
                }, cancellationToken);
                return Ok("Kaldet lykkedes.");
            }
            catch (TimeoutRejectedException) //Fanger exeptions, når tid overskrides
            {
                return StatusCode(504, "Tjenesten var for længe om at svare (Timeout på 5 sekunder). Prøv igen senere");
            }
        }

        [HttpGet("test-circuit-breaker")]
        public async Task<IActionResult> TestCircuitBreaker(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _resiliencePipeline.ExecuteAsync(async ct =>
                {
                    //kalder det ustabile endpoint, der fejler 50% af tiden
                    return await _httpClient.GetAsync("https://localhost:7181/api/fault/unstable", ct);
                }, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return Ok(await response.Content.ReadAsStringAsync());
                }
                return StatusCode((int)response.StatusCode, "Kaldet fejlede, men kredsløb er endnu ikke brudt.");
            }
            catch (BrokenCircuitException) //Fanger exeptions, når circuit breaker afbryder kald proaktivt
            {
                return StatusCode(503, "Kredsløb er åbent. Afventer recovery før nye forsøg tillades.");
            }
        }
    }
}
