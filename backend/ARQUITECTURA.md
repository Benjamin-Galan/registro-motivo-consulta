# Guía de arquitectura del backend

## Decisión: dos bases SQL Server

Sí. La API puede conectarse a dos bases SQL Server, incluso en servidores distintos. Configura dos conexiones independientes y dos adaptadores de persistencia: la base propia del módulo permite escrituras y migraciones; per cápita debe tratarse como una fuente externa de solo lectura. No unas estas bases con una transacción distribuida: primero consulta per cápita y luego registra el turno en la base propia. Maneja explícitamente los errores y el tiempo de espera de la fuente externa.

Mantén una interfaz de aplicación pequeña, por ejemplo `IVerificadorPerCapita`, y una implementación `PerCapitaSqlServerReader` en Infrastructure. La capa Application desconoce el esquema legado y solo recibe un resultado normalizado como vigente/no vigente, datos mínimos necesarios y fecha de verificación. Usa una cuenta SQL de solo lectura con permisos únicamente sobre las tablas o vistas necesarias. Si el dueño de la fuente ofrece una vista o procedimiento almacenado de consulta, úsalo en vez de acoplarte a sus tablas internas.

No guardes una copia del padrón per cápita. Para auditoría del registro, guarda el resultado de validación y su fecha, además del identificador usado según la política institucional; limita los datos personales duplicados y el tiempo de retención.

## Estructura propuesta

Empieza con cuatro proyectos y agrega proyectos separados solo cuando el tamaño del sistema lo justifique:

```text
backend/
  RegistroConsultas.sln
  src/
    RegistroConsultas.Api/             # HTTP, autenticación, composición y configuración
      Controllers/
      Contracts/                      # DTOs HTTP de entrada y salida
      Middleware/
      Authorization/
    RegistroConsultas.Application/    # casos de uso y puertos/interfaces
      Abstractions/
      Features/
        Admisiones/RegistrarConsulta/
        Admisiones/BuscarPaciente/
        Atencion/CambiarEstado/
        Programacion/
      Common/
    RegistroConsultas.Domain/         # reglas e invariantes del negocio
      Entities/
      ValueObjects/
      Enums/
      Exceptions/
    RegistroConsultas.Infrastructure/ # SQL, repositorios, identidad, reloj, auditoría
      Persistence/Registro/
      PerCapita/
      Identity/
      DependencyInjection.cs
  database/
    Registro/                          # scripts versionados de esquema y SP
    PerCapita/                         # solo documentación/contratos, no scripts ajenos
  docs/
```

Dependencias permitidas: `Domain` no depende de nadie; `Application` depende de `Domain`; `Infrastructure` depende de `Application` y `Domain`; `Api` referencia `Application` e `Infrastructure` para registrar implementaciones. Evita que Domain o Application conozcan ASP.NET, EF Core, SQL, DTOs HTTP o nombres de tablas externas.

Si prefieres avanzar gradualmente, conserva por ahora el proyecto `backend` como host/API y añade los otros proyectos a la solución. Evita crear capas que solo reenvían llamadas sin aportar una regla, un límite o una prueba aislada.

## Responsabilidades

- **Controllers:** traducen HTTP al caso de uso, validan el formato de entrada, devuelven códigos HTTP y DTOs. Delgados; sin SQL ni reglas de turnos.
- **DTOs/Contracts:** formas públicas de las solicitudes y respuestas. Nunca devuelvas entidades EF ni entidades de dominio directamente. No aceptes desde el cliente campos como usuario de admisión, contador atendido o estado calculado si el servidor debe decidirlos.
- **Application:** casos de uso como `RegistrarConsulta`, `BuscarPaciente` y `MarcarPacienteAusente`; orquesta interfaces, autorización del caso de uso cuando aplica, reglas de coordinación y resultados. Puede usar validadores y resultados tipados.
- **Domain:** reglas que siempre deben cumplirse, por ejemplo no admitir sobre el límite sin permiso, transición válida de estado y unicidad del turno en una programación/jornada. Las invariantes no deben vivir solo en el frontend.
- **Infrastructure:** EF Core/Dapper/ADO.NET, implementaciones de repositorios, llamadas parametrizadas a procedimientos almacenados, reloj, identidad y acceso al padrón.
- **Interfaces:** decláralas junto al consumidor. Application define puertos como `IRegistroConsultaRepository`, `IUnitOfWork` o `IVerificadorPerCapita`; Infrastructure los implementa. No crees interfaz por cada clase sin necesidad de sustituibilidad o límite externo.

## Persistencia propia y procedimientos almacenados

Usa `RegistroDbContext` exclusivamente para la base nueva y sus migraciones. EF Core es útil para CRUD y mapeo; puedes llamar procedimientos almacenados parametrizados desde Infrastructure para operaciones que requieran una transacción corta y atómica. Dapper o `Microsoft.Data.SqlClient` también son opciones válidas para consultas/procedimientos; no introduzcas tres tecnologías de acceso a datos sin una necesidad concreta.

La asignación del turno y el control de cupo deben ser atómicos en SQL: dentro de un procedimiento, valida cupo y vigencia, obtiene el siguiente consecutivo y crea el registro dentro de una transacción con el nivel de aislamiento/bloqueo apropiado. La secuencia de admisión es global para el área Consulta Externa IPSS: todos los pacientes de la fecha y ciclo activo comparten un consecutivo, sin particionarlo por clínica o médico. No calcules el turno con `MAX(numero_turno) + 1` fuera de una transacción. Define estrategia de reintento/idempotencia para que un reintento HTTP no emita dos turnos.

El contador de atendidos se puede derivar de los registros atendidos o mantener en programación si se actualiza transaccionalmente; no lo reinicies borrando historia. Modela estados con transiciones controladas y registra quién/cuándo cambió cada estado.

## Conexiones y secretos

No pongas contraseñas en `appsettings*.json`, Dockerfile, Compose versionado ni en el repositorio. Deja nombres de configuración sin secretos (por ejemplo `ConnectionStrings:Registro` y `ConnectionStrings:PerCapita`) y almacena los valores localmente con Secret Manager (`dotnet user-secrets`) o variables de entorno. En producción usa un gestor de secretos y una identidad de servicio con permisos mínimos. User Secrets ayuda en desarrollo, pero no cifra los valores.

Compose recibe la conexión de la base nueva por la variable de entorno `REGISTRO_SQL_CONNECTION`; al integrar per cápita, se añadirá una segunda variable de entorno independiente. No uses `TrustServerCertificate=True` en producción salvo una excepción aprobada y controlada; configura TLS con certificado validado.

La aplicación debe fallar de forma clara si falta la conexión requerida. Usa `IOptions<T>` con validación al inicio, conexiones con pool y timeouts razonables. No expongas cadenas ni detalles SQL en logs o respuestas HTTP.

## Autenticación implementada

El backend usa ASP.NET Core Identity sobre `RegistroDbContext` y SQL Server. El username es el identificador de acceso; el correo no es requerido. Las contraseñas nunca se guardan directamente: Identity administra el hash, y se configura mínimo 12 caracteres con mayúscula, minúscula, número y símbolo, además de bloqueo por cinco intentos fallidos durante 15 minutos.

La sesión usa cookie de autenticación `HttpOnly`, `Secure`, `SameSite=None`, con expiración deslizante de ocho horas. Las operaciones que modifican datos requieren antiforgery: primero llama `GET /api/v1/auth/csrf` con `credentials: "include"`; envía el `requestToken` recibido en `X-CSRF-TOKEN` y credenciales en `POST /api/v1/auth/login`. Después de iniciar sesión, vuelve a pedir el token CSRF para las siguientes operaciones autenticadas. Al cerrar sesión usa `POST /api/v1/auth/logout` con el token. `GET /api/v1/auth/me` devuelve el usuario actual.

Solo el rol `Administrador` puede crear usuarios vía `POST /api/v1/users`; el cuerpo lleva `username`, `password` y `role` (`Administrador`, `Admisionista` o `Medico`). No existe registro público. El primer administrador se crea de forma interactiva con `dotnet run --project backend/backend/backend.csproj -- --bootstrap-admin`; el comando aplica migraciones, solo procede si aún no hay usuarios, y oculta la contraseña al escribirla.

Para desarrollo local, configura el secreto fuera del repositorio y aplica el esquema antes de iniciar la API:

```powershell
dotnet user-secrets set "ConnectionStrings:Registro" "Server=localhost;Database=RegistroConsultas;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" --project backend/backend/backend.csproj
dotnet ef database update --project backend/src/RegistroConsultas.Infrastructure/RegistroConsultas.Infrastructure.csproj --startup-project backend/backend/backend.csproj
dotnet run --project backend/backend/backend.csproj -- --bootstrap-admin
dotnet run --project backend/backend/backend.csproj --launch-profile https
```

En Docker Compose define `REGISTRO_SQL_CONNECTION` en el entorno del proceso que ejecuta Compose; no lo escribas en `docker-compose.yml`. En producción, usa una conexión cifrada con certificado válido y un administrador de secretos. Configura `Frontend:Origin` (o `Frontend__Origin`) con el origen exacto de React; el valor de desarrollo actual es `http://localhost:5173`.

## API y seguridad

- Versiona la API (por ejemplo `/api/v1`) y usa nombres de recursos: `/api/v1/consultas`, `/api/v1/programaciones/{id}/cola`.
- La cuenta se identifica con nombre de usuario y contraseña; el correo no es obligatorio para iniciar sesión. Si no existe proveedor institucional, usa ASP.NET Core Identity con username como identificador, hash de contraseñas administrado por Identity, políticas de contraseña, bloqueo gradual por intentos fallidos, recuperación administrativa controlada y roles/permisos. No guardes contraseñas en texto plano ni implementes tu propio algoritmo de hash.
- Para React, decide el mecanismo según el despliegue: si frontend y API comparten sitio/origen, prioriza cookie segura `HttpOnly`, `Secure`, `SameSite` y protección CSRF; si la API necesita bearer tokens, utiliza un emisor OIDC/OAuth estándar o un diseño de tokens con expiración y renovación bien definido. No guardes tokens de larga duración en `localStorage` ni inventes un emisor JWT casero. Un proveedor de identidad puede presentar formulario de usuario/contraseña y no requiere que el usuario se identifique por correo.
- Aplica políticas/roles en endpoints y permisos por recurso: Administrador, Admisión, Médico y Pantalla pública. Verifica también que el médico solo pueda operar su programación/cola. Una pantalla pública debe recibir solo datos estrictamente públicos, nunca la respuesta interna completa del paciente.
- HTTPS obligatorio, CORS con orígenes conocidos, límites de tamaño y frecuencia, validación de entrada, errores uniformes (Problem Details), protección contra SQL injection con parámetros, y autorización en cada operación sensible.
- Auditoría de accesos/cambios sensibles: actor, acción, entidad, fecha, correlación y cambios mínimos necesarios; no registres cédulas, INSS, tokens ni datos clínicos en logs generales. Protege auditoría de modificación y define retención.
- Separa secretos de configuración, restringe permisos de la cuenta de aplicación, cifra tráfico y copias de seguridad, y define procedimiento de rotación de credenciales.

## Realtime y operación

Para cola del médico y pantallas, empieza con SignalR para notificar cambios tras confirmar la transacción. La base de datos sigue siendo la fuente de verdad; los clientes deben recargar estado tras reconectar. Si luego hay varias instancias de API, configura un backplane/servicio administrado y afinidad o transporte adecuado. No uses polling de alta frecuencia contra SQL.

Incluye health checks separados para API, base propia y fuente per cápita; una caída de per cápita debe producir un error operativo explícito y no marcar al paciente como vigente. Añade logs estructurados, métricas, trazas/correlation ID y alertas sin información identificable. Planifica copias, restauración probada y migraciones SQL controladas.

## Modelo del documento: decisiones para revisar

- El médico puede tener varias especialidades: usa `especialidades_medicos` como relación muchos a muchos y no mantengas a la vez una FK singular `medicos.id_especialidad`.
- Aclara si `consultorios` representan clínicas y si `programacion_consulta` representa una combinación clínica/médico/fecha/jornada. En la tabla el FK aparece como `clinica_id`, mientras algunos textos dicen consultorio.
- El diagrama no incluye `pacientes`, aunque el documento lo requiere y `registro_consulta` lo referencia. Defínelo sin copiar más información personal del origen de la necesaria.
- Define un catálogo de estados de atención y transiciones (espera, llamado/en atención, atendido, ausente, anulado, trasladado). El alcance del consecutivo ya está definido como global por área/fecha/ciclo.
- Decide tratamiento de citas, sobrecupos, reactivación de ausentes, reasignaciones, concurrencia, privacidad de pantallas y datos de beneficiarios antes de fijar restricciones.
- La columna `contador_atendidos` en programación no debe confundirse con cupos emitidos. Define claramente qué representa cada contador y cómo se reconstruye.

### Reglas confirmadas

- Médico y especialidad tienen relación muchos a muchos mediante `especialidades_medicos`.
- La numeración del turno es una cola global de admisión por área y fecha, compartida entre clínicas. La política de mediodía configura si se abre un ciclo nuevo (reinicia a 1) o si el consecutivo continúa. La jornada médica AM/PM y el período de numeración son conceptos distintos: el reinicio de números no altera cupos, contador de atendidos ni registros históricos.
- El reinicio al mediodía queda como opción configurable: al activarla inicia un ciclo de numeración nuevo desde 1; al desactivarla, el número sigue aumentando durante ese día. Recomiendo configurar hora y política por área, con mediodía como valor inicial, en vez de codificar las 12:00 en el procedimiento.
- La numeración no se particiona por clínica. Para permitir repetir números después del reinicio y conservar historia, modela el ámbito/ciclo de numeración con un identificador persistente: por ejemplo `ciclos_turno` (área, fecha, número de ciclo, inicio/cierre) y `registro_consulta.ciclo_turno_id`. La unicidad es `(ciclo_turno_id, numero_turno)`. El contador se actualiza bajo bloqueo/transacción en el ciclo activo. No uses una restricción única por fecha y número, porque impediría el reinicio configurable.
- La admisión se bloquea cuando per cápita no confirma que el paciente está activo. Una falla o timeout del servicio/base externa no equivale a estado activo. No se crea turno pendiente para saltarse esta regla.

El CSV adjunto contiene datos personales identificables. Úsalo solo en un entorno autorizado, evita copiarlo al repositorio, y prepara datos sintéticos para desarrollo y ejemplos.

## Secuencia sugerida de implementación

1. Acordar reglas y estados del MVP; dibujar el ERD final y definir claves, índices, restricciones, auditoría y privacidad.
2. Crear proyectos y referencias de capas; agregar configuración tipada y los dos adaptadores SQL sin exponer secretos.
3. Implementar autenticación con username/contraseña y políticas de autorización antes de exponer operaciones reales.
4. Implementar catálogos y búsqueda de paciente/per cápita en solo lectura.
5. Implementar el caso transaccional `RegistrarConsulta` con concurrencia, límite, turno idempotente y auditoría.
6. Añadir cola y transiciones de atención.
7. Añadir SignalR/pantallas públicas, observabilidad, despliegue, copias y recuperación.

Mantén contratos de la fuente per cápita y scripts de la base propia versionados. Las pruebas de concurrencia y permisos serán necesarias antes de producción, aunque el desarrollo inicial puede empezar con datos sintéticos.

## Catálogos implementados: áreas y zonas/filiales

La API administra los catálogos `areas` y `zonas_filiales` con `BIGINT IDENTITY` como clave primaria, código único, nombre, indicador `reg_anulado` y fechas UTC de creación/modificación. `reg_anulado` se añadió también a `zonas_filiales` (el diagrama no lo dibuja) para permitir baja lógica y restauración sin borrar información referenciable. El código se guarda recortado y en mayúsculas; admite letras, números, guion y guion bajo, hasta 20 caracteres. Se conserva único incluso en filas anuladas para que un código histórico no se recicle accidentalmente.

Los dos catálogos son independientes en el esquema actual: el diagrama no define una FK de zona a área ni de área a zona, así que la API no inventa esa relación. En el modelo completo previsto, `consultorios.area_id` referencia `areas.id` y `registro_consulta.filial_id` referencia `zonas_filiales.id`; esas FKs se agregarán al implementar consultorios y registro de consulta.

### Rutas y permisos

Todas las rutas requieren cookie de sesión válida. Las lecturas están disponibles a usuarios autenticados; crear, actualizar, anular y restaurar requieren el rol `Administrador`. Las escrituras también requieren cookie antiforgery y header `X-CSRF-TOKEN`.

| Método y ruta | Acción | Respuesta normal |
| --- | --- | --- |
| `GET /api/v1/areas` | Listar áreas activas, ordenadas por nombre | `200` con arreglo |
| `GET /api/v1/areas?incluirAnulados=true` | Incluir anuladas; solo Administrador | `200` con arreglo |
| `GET /api/v1/areas/{id}` | Obtener área por PK | `200` con objeto |
| `POST /api/v1/areas` | Crear área | `201` y encabezado `Location` |
| `PUT /api/v1/areas/{id}` | Reemplazar código y nombre | `200` con objeto actualizado |
| `DELETE /api/v1/areas/{id}` | Anulación lógica (`reg_anulado=true`) | `204` |
| `PATCH /api/v1/areas/{id}/estado` | Activar/restaurar o anular | `204` |
| `GET /api/v1/zonas-filiales` | Listar zonas activas, ordenadas por nombre | `200` con arreglo |
| `GET /api/v1/zonas-filiales?incluirAnulados=true` | Incluir anuladas; solo Administrador | `200` con arreglo |
| `GET /api/v1/zonas-filiales/{id}` | Obtener zona por PK | `200` con objeto |
| `POST /api/v1/zonas-filiales` | Crear zona | `201` y encabezado `Location` |
| `PUT /api/v1/zonas-filiales/{id}` | Reemplazar código y nombre | `200` con objeto actualizado |
| `DELETE /api/v1/zonas-filiales/{id}` | Anulación lógica | `204` |
| `PATCH /api/v1/zonas-filiales/{id}/estado` | Activar/restaurar o anular | `204` |

Los cuerpos de creación/actualización son, por ejemplo, `{"codigo":"CE-IPSS","nombre":"Consulta Externa IPSS"}`. Para cambiar estado usa `{"activo":true}` o `{"activo":false}`. El endpoint de listado excluye anulados por defecto. Para obtener una fila anulada por ID, el administrador usa `?incluirAnulado=true`.

### Claves, códigos y relaciones

La PK numérica no es una contraseña ni un secreto; puede aparecer en una API administrativa y debe protegerse el recurso mediante autenticación/autorización. Ocultarla no impide que alguien pruebe otros IDs, por eso cada operación debe validar sus permisos. Si algún consumidor externo requiere identificadores no predecibles, se puede agregar un `public_id` UUID sin reemplazar la PK relacional.

El código es una clave de negocio legible (por ejemplo, `CE-IPSS`) y tiene índice único. En `POST`, el cliente envía código y nombre; SQL Server genera el `id` y la respuesta `201` devuelve ambos. Al seleccionar un área desde React para asociarla a un consultorio, el UI muestra nombre/código, conserva `id` como valor seleccionado y envía `areaId`; el backend valida el ID y guarda la FK. Así no se busca por código en cada relación y los cambios de código no rompen referencias. Si otro sistema solo conoce un código, puede buscarse explícitamente por código y luego resolverse a la PK, con la misma validación de autorización.

### Respuestas y errores

Las respuestas usan JSON con `id`, `codigo`, `nombre`, `activo`, `creadoEl` y `modificadoEl`. El middleware devuelve Problem Details: `400` para campos/códigos con formato inválido, `401` sin sesión, `403` sin rol o permiso, `404` para ID inexistente o anulado en una consulta que no lo incluye, `409` para código duplicado y `500` genérico con trace ID en errores inesperados. Nunca devuelve SQL, excepciones internas ni contraseñas.

La migración `AddAreasAndZonasFiliales` crea las dos tablas e índices únicos en la base propia junto con el historial normal de EF Core. Después de revisar la migración, se aplica con `dotnet ef database update`; no altera datos de per cápita.
