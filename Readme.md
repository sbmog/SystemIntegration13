# Øvelse: Implementering af Resiliens-mønstre med Aspire

## Introduktion
I denne øvelse skal du lære at arbejde med resiliens i distribuerede systemer. Vi fokuserer på tre centrale mønstre: **Retry**, **Timeout** og **Circuit Breaker**.

Vi har ét eksisterende projekt:
- **ResilienceTestApi**: Kører på `https://localhost:7181`.
  - `api/fault`: Returnerer bevidst fejl (HTTP 500) de første to gange, det kaldes.
  - `api/fault/timeout`: Dette endpoint svarer aldrig (simulerer et system, der er gået i stå).
  - `api/fault/unstable`: Dette endpoint skifter mellem succes og fejl (bruges til at teste Circuit Breaker).

---

## Opgave 1: Implementering af Retry

Målet er at få din egen tjeneste til at håndtere midlertidige fejl ved at prøve igen automatisk.

### Trin 1: Projektopsætning
1. Opret et nyt **ASP.NET Core Web API** projekt (f.eks. `MyResilientApi`).
2. Tilføj NuGet-pakken `Microsoft.Extensions.Http.Resilience`.
3. Registrer projektet i `SystemIntergration13.AppHost/AppHost.cs`, så det kan køre sammen med test-API'et.

### Trin 2: Implementer Retry-logik
1. I en controller skal du konfigurere en `ResiliencePipeline` vha. en `ResiliencePipelineBuilder`.
2. Tilføj en **Retry** strategi med følgende krav:
   - Maksimalt 3 forsøg i alt.
   - En fast pause mellem forsøgene.
3. Lav et endpoint, der bruger denne pipeline til at kalde `https://localhost:7181/api/fault` via en `HttpClient`.

---

## Opgave 2: Implementering af Timeout

Nogle gange svarer en tjeneste slet ikke. I denne opgave skal du sikre, at din klient ikke venter for evigt på et svar.

### Trin 1: Definition af Timeout Pipeline
1. Opret en ny (eller udvid din eksisterende) `ResiliencePipeline`.
2. Tilføj en **Timeout** strategi til din builder.
3. Krav til strategien:
   - Den skal afbryde kaldet, hvis der ikke er modtaget svar inden for et kort tidsrum (f.eks. 5 sekunder).

### Trin 2: Test med Timeout endpointet
1. Opret et nyt endpoint i din controller, der kalder `https://localhost:7181/api/fault/timeout`.
2. Udfør kaldet igennem din pipeline med timeout-strategien.
3. Sørg for at fange den undtagelse (Exception), som pipelinen kaster, når timeouten indtræffer, så du kan returnere en pæn fejlbesked til brugeren.

---

## Opgave 3: Implementering af Circuit Breaker

Et Circuit Breaker mønster forhindrer din applikation i at forsøge at udføre en handling, der med stor sandsynlighed vil fejle, hvilket sparer ressourcer og giver det fejlede system ro til at komme sig.

### Trin 1: Definition af Circuit Breaker Pipeline
1. Tilføj en **Circuit Breaker** strategi til din `ResiliencePipelineBuilder`.
2. Konfigurer din Circuit Breaker med følgende (eksempel):
   - En lav fejl-tærskel (Failure Ratio), så den er nem at teste (f.eks. 0.5).
   - En kort periode hvor kredsløbet er åbent (Break Duration), før det forsøger at lukke igen.

### Trin 2: Test med Unstable endpointet
1. Opret et endpoint, der kalder `https://localhost:7181/api/fault/unstable`.
2. Lav mange kald hurtigt efter hinanden mod dette endpoint.
3. Observer hvordan din pipeline efter et par fejl stopper med overhovedet at forsøge at kalde serveren, og i stedet med det samme kaster en `BrokenCircuitException`.

### Trin 3: Observation
1. Prøv at kigge på, hvordan man kan overvåge tilstanden af en Circuit Breaker.
2. Hvordan reagerer din applikation, når kredsløbet går fra "Open" til "Half-Open" og til sidst "Closed"?

---

## Ekstra opgave: Opgave 4: Chaos Engineering med Simmy

Chaos Engineering handler om at teste dit systems modstandskraft ved bevidst at introducere fejl i et ellers velfungerende miljø. Til dette bruger vi "Simmy", som er Pollys bibliotek til chaos engineering.

Læs mere her: [Polly Chaos Engineering](https://www.pollydocs.org/chaos/index.html)

### Trin 1: Introducer kaos i din pipeline
I din `ResiliencePipelineBuilder` kan du tilføje kaos-strategier *før* dine resiliens-strategier. Prøv at eksperimentere med følgende:

1. **Injektion af fejl (Fault Injection)**:
   - Tilføj en strategi, der med en vis sandsynlighed (f.eks. 10%) kaster en exception eller returnerer en fejl, selvom det kaldte API faktisk virker.
2. **Injektion af forsinkelse (Latency Injection)**:
   - Tilføj en strategi, der introducerer en kunstig forsinkelse på f.eks. 10 sekunder på tilfældige kald.

### Trin 2: Test din modstandskraft
1. Konfigurer din pipeline til at kalde et endpoint, der normalt altid virker (f.eks. et standard WeatherForecast endpoint).
2. Se hvordan dine **Retry** og **Timeout** politikker fra de foregående opgaver automatisk håndterer det kaos, du nu selv har introduceret.

### Trin 3: Refleksion
- Hvorfor er det værdifuldt at introducere fejl bevidst i et testsystem?
- Hvordan hjælper dette dig med at finde svagheder i dine timeouts og retry-indstillinger?
