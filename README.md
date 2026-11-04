# ParkDesk API

API REST para la operación de un estacionamiento: entrada y salida de vehículos, reservas de
espacio por horario, tarifas con periodo de gracia y tope diario, membresías con descuento y
cobro de recargo por exceso de tiempo. No es un CRUD de ejemplo: el corazón del proyecto es
garantizar que **nunca se asigne el mismo espacio físico a dos vehículos** cuando llegan
solicitudes de entrada al mismo tiempo, y que las reservas respeten la capacidad real del
estacionamiento.

## Qué resuelve

Un estacionamiento necesita tres cosas que un CRUD simple no te da:

1. **Asignación segura de espacios bajo concurrencia.** Cuando dos operadores registran una
   entrada casi al mismo tiempo y solo queda un espacio libre, el sistema debe garantizar que
   solo uno de los dos lo consiga, sin bloquear la base de datos ni perder la otra solicitud
   (que debe fallar con un error claro, no con una condición de carrera silenciosa).
2. **Reservas con capacidad real.** Un cliente puede reservar un tipo de espacio (Regular,
   Compacto, Eléctrico, Discapacitados) para una ventana de tiempo futura. El sistema valida que
   no se comprometan más reservas que espacios físicos existen para ese tipo, considerando
   traslapes de horario.
3. **Tarifas con reglas de negocio reales.** Periodo de gracia sin costo, tarifa por hora
   redondeada hacia arriba, tope diario, descuento por membresía activa y recargo por exceso de
   tiempo cuando el checkout ocurre después de la ventana reservada.

## Arquitectura

Solución .NET 8 con dos proyectos:

```
src/ParkDesk.Api/        API ASP.NET Core (controllers + EF Core + JWT)
tests/ParkDesk.Api.Tests/ Pruebas xUnit (unitarias + integración con WebApplicationFactory)
```

Dentro de la API:

- **Entities/** — modelo de dominio (`ParkingLot`, `ParkingSpot`, `Tariff`, `ParkingSession`,
  `Reservation`, `MembershipPlan`, `UserMembership`, `User`).
- **Data/** — `AppDbContext` (configuración de EF Core) y `DbSeeder` (datos iniciales: admin,
  operador, planes de membresía y un estacionamiento de ejemplo).
- **Services/** — la lógica de negocio real vive aquí, no en los controllers:
  - `ParkingService` — asignación concurrente de espacios, check-in/check-out.
  - `ReservationService` — capacidad por traslape de horarios, expiración de reservas.
  - `PricingService` — cálculo puro de tarifas (sin dependencias de EF, 100% testeable).
  - `AuthService` / `Pbkdf2PasswordHasher` — registro, login y hash de contraseñas.
  - `MembershipService` — suscripción a planes de membresía.
- **Controllers/** — delgados: validan el modelo, llaman al servicio correspondiente y mapean a
  DTOs. Toda excepción de negocio (`NotFoundException`, `ConflictException`,
  `BusinessRuleException`) se traduce a `ProblemDetails` en `ExceptionHandlingMiddleware`.
- **Common/** — `PagedResult<T>`, excepciones de dominio y el middleware de errores.

## Decisiones técnicas

### Concurrencia: token manual en vez de `rowversion`

`ParkingSpot.ConcurrencyStamp` es un `Guid` marcado como `IsConcurrencyToken()` en vez de un
`rowversion` nativo de SQL Server. Un `rowversion` solo funciona contra SQL Server; como las
pruebas corren contra SQLite (requisito del proyecto), se necesitaba un mecanismo de
concurrencia optimista que EF Core traduzca igual en cualquier proveedor. El token se
regenera manualmente en cada escritura, y EF Core genera un `UPDATE ... WHERE Id = @id AND
ConcurrencyStamp = @stamp` — si otra transacción ya cambió el valor, la actualización afecta
cero filas y EF lanza `DbUpdateConcurrencyException`.

`ParkingService.AssignSpotAsync` usa esto así: toma la lista de espacios libres de un tipo
(ordenados por código) e intenta reclamarlos uno por uno. Si pierde la carrera por el primero
(porque otra solicitud ya lo tomó), no falla: simplemente continúa con el siguiente candidato.
Solo se devuelve "sin espacios disponibles" cuando de verdad no queda ninguno libre. Esto se
prueba en `ParkingConcurrencyTests`, que lanza 10 check-ins simultáneos contra un
estacionamiento con 3 espacios y verifica que exactamente 3 tengan éxito, con espacios
distintos y sin que el conteo final de ocupados se desfase.

### Reservas: capacidad por traslape, no "un hold por espacio"

A diferencia de un sistema de boletos con holds temporales sobre un asiento específico, aquí
una reserva compromete un **tipo** de espacio para una ventana de tiempo, no un espacio físico
concreto (el espacio físico se asigna hasta el check-in, con el mismo mecanismo de concurrencia
optimista). Al crear una reserva, se cuentan las reservas `Pending`/`CheckedIn` del mismo tipo
cuyo horario se traslapa con el solicitado; si ese conteo ya iguala el total de espacios de ese
tipo, se rechaza con `409 Conflict`. Una reserva vencida (el cliente no llegó dentro de los 30
minutos de gracia después de la hora de inicio) se marca `Expired` de forma perezosa (al
leerla o listarla), sin necesidad de un `BackgroundService` sondeando la base de datos.

### Tarifas: cálculo puro y testeable

`PricingService.Calculate` no toca la base de datos: recibe una tarifa y fechas, y devuelve el
monto base, el recargo por exceso y el total. Reglas aplicadas en orden: minutos de gracia
gratuitos → redondeo hacia arriba a la hora completa → tope diario → descuento por membresía →
recargo por exceso de tiempo si el checkout ocurre después de la ventana reservada. Al ser una
función pura, se prueba exhaustivamente sin `WebApplicationFactory` ni base de datos.

### Autenticación y autorización

JWT con claims de rol (`Admin`, `Operator`, `Customer`). Las contraseñas se protegen con
PBKDF2-SHA256 (100,000 iteraciones) en vez de un hash simple. Los roles están repartidos así:

| Acción | Rol |
|---|---|
| Administrar estacionamientos, espacios y tarifas | `Admin` |
| Registrar entradas/salidas y check-in de reservas | `Admin`, `Operator` |
| Crear/cancelar reservas y suscribirse a una membresía | `Customer` (dueño del recurso) |
| Consultar estacionamientos, espacios y tarifas | público |

### Rate limiting y CORS

El login (`POST /api/auth/login`) está protegido con un limitador de ventana fija
(`RateLimiting:Login:PermitLimit`, 5 intentos/minuto por defecto) para dificultar fuerza bruta.
El límite es configurable para no interferir con las pruebas de integración (que inician sesión
muchas veces dentro de la misma ejecución); `RateLimitingTests` verifica el comportamiento real
con un límite bajo inyectado solo para ese test. CORS está abierto por defecto y se restringe
con `Cors:AllowedOrigins` en producción.

### Errores como `ProblemDetails`

Toda excepción de dominio hereda de `ApiException` y declara su propio código HTTP
(`NotFoundException` → 404, `ConflictException` → 409, `BusinessRuleException` → 422,
`UnauthorizedException` → 401). `ExceptionHandlingMiddleware` las traduce a
`application/problem+json` consistente; cualquier excepción no prevista cae a 500 sin filtrar
detalles internos al cliente.

## Cómo correrlo

### Con Docker (API + SQL Server)

```bash
docker compose up --build
```

La API queda disponible en `http://localhost:8080` (Swagger en `/swagger` solo en
`Development`). Al iniciar, aplica migraciones de EF Core contra SQL Server y siembra datos de
ejemplo: un usuario administrador (`admin@parkdesk.local` / `Admin123!`), un operador
(`operator@parkdesk.local` / `Operator123!`), tres planes de membresía y un estacionamiento
("Plaza Centro") con espacios y tarifas ya configurados.

### Local (sin Docker)

Requiere .NET 8 SDK y una instancia de SQL Server accesible (ajustar
`ConnectionStrings:DefaultConnection` en `appsettings.json` o vía variable de entorno
`ConnectionStrings__DefaultConnection`).

```bash
dotnet run --project src/ParkDesk.Api
```

### Migraciones de EF Core

El repositorio ya incluye la migración inicial (`src/ParkDesk.Api/Migrations`). Para generar una
nueva tras cambiar el modelo:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add NombreDeLaMigracion -p src/ParkDesk.Api -o Migrations
```

## Cómo probarlo

```bash
dotnet test
```

Las pruebas **no requieren SQL Server ni Docker**: usan SQLite en memoria con caché compartida
(`Mode=Memory;Cache=Shared`) a través de un `WebApplicationFactory<Program>` personalizado
(`ParkDeskWebApplicationFactory`). El esquema se crea con `EnsureCreated()` (no se corren las
migraciones de SQL Server contra SQLite, para evitar tipos de columna incompatibles) y se
siembra con los mismos datos base que usa producción.

Cobertura:

- **Unitarias** (`Unit/PricingServiceTests.cs`) — reglas de tarifas: gracia, redondeo, tope
  diario, descuento, recargo por exceso.
- **Concurrencia** (`Integration/ParkingConcurrencyTests.cs`) — 10 check-ins simultáneos contra
  3 espacios disponibles; verifica que no haya doble asignación.
- **Flujo de autenticación y autorización** (`Integration/AuthFlowTests.cs`) — registro, login,
  credenciales inválidas, endpoints protegidos por rol.
- **Flujo de sesiones** (`Integration/SessionsFlowTests.cs`) — check-in/check-out, placa
  duplicada, estacionamiento lleno, restricción por rol.
- **Flujo de reservas** (`Integration/ReservationsFlowTests.cs`) — creación, conflicto de
  capacidad por traslape, check-in, cancelación, aislamiento entre usuarios.
- **Paginación** (`Integration/PaginationTests.cs`).
- **Rate limiting** (`Integration/RateLimitingTests.cs`) — verifica el `429` con un límite bajo
  inyectado solo en ese test, sin afectar al resto de la suite.

## Endpoints principales

| Método | Ruta | Rol |
|---|---|---|
| POST | `/api/auth/register` | público |
| POST | `/api/auth/login` | público (rate-limited) |
| GET | `/api/lots` | público |
| POST | `/api/lots` | Admin |
| POST | `/api/lots/{lotId}/spots` | Admin |
| PUT | `/api/lots/{lotId}/tariffs` | Admin |
| POST | `/api/lots/{lotId}/sessions/checkin` | Admin, Operator |
| POST | `/api/sessions/{id}/checkout` | Admin, Operator |
| GET | `/api/sessions` | Admin, Operator (paginado) |
| POST | `/api/reservations` | Customer |
| POST | `/api/reservations/{id}/checkin` | Admin, Operator |
| POST | `/api/reservations/{id}/cancel` | dueño de la reserva o Admin/Operator |
| GET | `/api/reservations` | autenticado (paginado; Customer solo ve las suyas) |
| GET | `/api/memberships/plans` | público |
| POST | `/api/memberships/subscribe` | Customer |

## Stack

C# / .NET 8, ASP.NET Core Web API, Entity Framework Core, SQL Server (SQLite en pruebas), JWT +
PBKDF2, rate limiting, xUnit + WebApplicationFactory, Docker, Docker Compose, GitHub Actions.
