### Explicación de Endpoints

La API está diseñada para simular una arquitectura de **cifrado del lado del
cliente** (_client-side encryption_) o de **conocimiento cero**
(_zero-knowledge_), similar a la que utiliza Bitwarden.

El principio fundamental es que el servidor actúa como un "almacenamiento tonto"
que solo guarda datos cifrados y nunca tiene acceso a la contraseña maestra
del usuario ni a la clave de cifrado. Para demostrar este flujo sin necesidad
de un front-end, se crearon endpoints especiales bajo la ruta `/client/...`
que simulan las operaciones criptográficas que ocurrirían en la aplicación
del usuario (por ejemplo, en su navegador).

---

#### `/auth` - Autenticación y Registro

- `POST /auth/register`: Registra un nuevo usuario. Recibe un email y una
  `master_password`. El servidor **nunca guarda la contraseña maestra**;
  en su lugar, calcula un _hash_ de la misma y lo almacena para futuras
  verificaciones.
- `POST /auth/login`: Autentica a un usuario. Recibe el email y la
  `master_password`. Si el _hash_ de la contraseña coincide con el
  almacenado, devuelve un **JSON Web Token (JWT)** que se usará para
  autorizar las siguientes peticiones.

#### `/server` - Lógica del Servidor (Almacenamiento)

- `GET /server/vault`: Obtiene la "bóveda" (_vault_) de contraseñas del
  usuario. Este endpoint está protegido y requiere un JWT válido. Es
  importante destacar que **solo devuelve un bloque de texto cifrado**, ya
  que el servidor no tiene forma de leer su contenido.
- `PUT /server/vault`: Actualiza la bóveda cifrada del usuario. Recibe un
  cuerpo JSON con el nuevo contenido cifrado (ej: `{"vault": "texto..."}`)
  y lo reemplaza por completo en la base de datos para el usuario
  autenticado. Al igual que el `GET`, este endpoint no puede leer ni validar
  el contenido que guarda, solo lo almacena.

#### `/client` - Simulación de Lógica del Cliente

Estos endpoints contienen la lógica que, en una aplicación real, se ejecutaría
en el dispositivo del usuario.

- `POST /client/encrypt-and-update-vault`: Simula el proceso de añadir o
  actualizar una contraseña en la bóveda.
  1.  Recibe la `master_password` y los datos en **texto plano** que se
      quieren guardar.
  2.  Deriva la clave de cifrado a partir de la `master_password`.
  3.  Llama internamente a `GET /server/vault` para obtener la bóveda
      cifrada actual.
  4.  Descifra la bóveda en memoria, le añade la nueva información y
      **vuelve a cifrar todo el contenido**.
  5.  Finalmente, llama a `PUT /server/vault` para guardar este nuevo bloque
      de datos cifrados, reemplazando el anterior.
- `POST /client/decrypt-vault`: Simula el proceso de visualizar las
  contraseñas.
  1.  Recibe la `master_password` del usuario.
  2.  Deriva la clave de cifrado, obtiene la bóveda del servidor y la
      descifra en memoria para devolver los datos en texto plano.

#### `/docs` - Documentación de la API

- `GET /docs`: Presenta la documentación interactiva de la API generada con
  Swagger / OpenAPI. Este endpoint permite visualizar y probar de forma
  sencilla todos los demás endpoints disponibles.

---

### Vinculación con las Tácticas de la Tarea

Este diseño permite demostrar las tres tácticas de arquitectura seleccionadas:

1.  **Gestionar Pedidos de Trabajo (Rendimiento)**: Se implementó
    **Rate Limiting** en el endpoint `POST /auth/login`. Esto previene
    ataques de fuerza bruta y protege el rendimiento del sistema al limitar
    la cantidad de solicitudes de autenticación, cumpliendo con el requisito.

2.  **Autorización (Seguridad - Resistir a ataques)**: Todos los endpoints de
    `/server` y `/client` están protegidos. Requieren un JWT válido que se
    obtiene en el login. El sistema verifica que el JWT corresponda al
    usuario dueño de los datos, impidiendo que un usuario acceda a la bóveda
    de otro. Esto implementa la táctica de **autorización**.

3.  **Cifrado de Datos (Seguridad - Resistir a ataques)**: Esta es la táctica
    central demostrada con la arquitectura _zero-knowledge_. La base de datos
    (representada por los endpoints `/server/vault`) **solo almacena datos
    cifrados**. El servidor nunca tiene acceso a la contraseña maestra ni a la
    clave de cifrado, por lo que no puede descifrar los datos de los usuarios.
    Esto garantiza la confidencialidad de la información incluso si la base
    de datos es comprometida, cumpliendo con la táctica de **cifrado de datos**
    para resistir ataques.
