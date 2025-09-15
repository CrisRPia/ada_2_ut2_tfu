### Arquitectura y Simulación

Esta API demuestra una arquitectura de **conocimiento cero** (_zero-knowledge_),
donde el servidor no tiene acceso a los datos sensibles del usuario. Para
lograrlo, el proyecto se divide en tres tipos de endpoints:

1.  **`/auth`**: Maneja la autenticación y registro de usuarios.
2.  **`/server`**: Actúa como un almacenamiento seguro pero "tonto"
    (_dumb storage_). Solo guarda y sirve un bloque de datos cifrados sin
    poder leerlos.
3.  **`/client`**: Simula las operaciones criptográficas que ocurrirían en una
    aplicación cliente. Estos endpoints reciben datos en texto plano y la
    contraseña maestra para realizar el cifrado y descifrado.

---

### Cómo Ejecutar

Para levantar la aplicación y la base de datos, ejecuta el siguiente comando
en la raíz del proyecto:

```bash
docker compose down -v && docker compose up --build
```

---

### Cómo Probar

Una vez que la aplicación está en funcionamiento, la documentación interactiva
de Swagger estará disponible para probar todos los endpoints.

[**Abrir Documentación de Swagger**](http://localhost:8080/swagger/index.html) 🚀

El flujo de prueba recomendado es:

1.  Usar `POST /auth/register` para crear un nuevo usuario.
2.  Copiar el `token` JWT de la respuesta.
3.  Hacer clic en el botón "Authorize" en Swagger y pegar el token.
4.  Usar `POST /client/encrypt-and-update-vault` para guardar de forma
    segura las credenciales.
5.  Usar `POST /client/decrypt-vault` para descifrar y ver las
    credenciales guardadas.

---

### Vinculación con las Tácticas de la Tarea

Este diseño permite demostrar las tres tácticas de arquitectura seleccionadas:

1.  **Gestionar Pedidos de Trabajo (Rendimiento)**: Se implementó
    **Rate Limiting** utilizando un `FixedWindowLimiter` de ASP.NET Core. Esta
    configuración permite un máximo de 4 peticiones cada 12 segundos,
    previniendo ataques de fuerza bruta y protegiendo el rendimiento del
    sistema. La configuración se encuentra en `Program.cs`:

    ```csharp
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name
                    ?? httpContext.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 4,
                    QueueLimit = 2,
                    Window = TimeSpan.FromSeconds(12),
                }
            )
        );

        options.OnRejected = (context, cancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.",
                CancellationToken.None
            );

            return new ValueTask();
        };
    });
    ```

2.  **Autorización (Seguridad - Resistir a ataques)**: Todos los endpoints de
    `/server` y `/client` están protegidos y requieren un JWT válido. El
    sistema verifica que el JWT corresponda al usuario dueño de los datos,
    impidiendo que un usuario acceda a la bóveda de otro.

3.  **Cifrado de Datos (Seguridad - Resistir a ataques)**: Esta es la táctica
    central. La base de datos, a través de los endpoints `/server/vault`,
    **solo almacena un bloque de datos cifrados**. El servidor nunca tiene
    acceso a la contraseña maestra ni a la clave de cifrado. Todo el proceso
    criptográfico es simulado en los endpoints `/client/...`, garantizando la
    confidencialidad de la información.
