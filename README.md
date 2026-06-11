# Sistema Integral de Gestion Documental para Normativas de Calidad

Plataforma para la gestion, control de versiones y trazabilidad de documentos de
calidad (manuales, procedimientos, registros, auditorias). Arquitectura multi-stack
dividida en 3 modulos, cada uno con su propia base de datos, comunicados por API y
orquestados con Docker.

## Arquitectura

| Modulo | Tecnologia | Base de datos | Responsabilidad |
|--------|-----------|---------------|-----------------|
| Central (Backend + Admin) | .NET 10 MVC (C#) | SQL Server | Login, usuarios, roles, flujo de revision/aprobacion en 2 pasos, control de versiones, bitacora |
| Consulta y Reportes | PHP 8 (MVC) | PostgreSQL | Consulta de documentos vigentes, descargas y reportes |
| Indexacion y Metadatos | Node.js + TypeScript | MongoDB | Busqueda de texto completo con resaltado, metadatos y etiquetas |

El Modulo Central es la "fuente de la verdad". Cuando un documento se aprueba, Central
notifica via API al modulo de Indexacion (guarda metadatos y texto en MongoDB) y al de
Consulta (deja el documento vigente en PostgreSQL). Las bases de datos NO se comunican
entre si: lo hacen a traves de las APIs de cada modulo, todas conectadas por una red
Docker comun llamada `gd_backbone`.

## Funcionalidades principales

- **Control de versiones**: cada documento nace en la version **0.1**; las correcciones
  suben de decimal (0.2, 0.3...) y, al recibir la aprobacion final, la version se
  promueve a numero entero (1.0, 2.0...).
- **Flujo de aprobacion en 2 pasos**:
  1. El **Supervisor** sube el documento -> queda *Pendiente de revision*.
  2. El **Revisor** lo revisa y lo pasa al Aprobador -> queda *Pendiente de aprobacion*
     (o lo rechaza indicando un motivo).
  3. El **Aprobador** da la aprobacion final -> queda *Aprobado* y *vigente* (se publica
     en Consulta y se indexa en Busqueda); o lo rechaza.
  - Si un documento es rechazado, el Supervisor sube una version corregida y el ciclo
    vuelve a empezar en revision.
- **Busqueda de texto completo**: en el modulo de Indexacion se puede buscar una palabra
  y ver en que documentos aparece, cuantas veces, fragmentos de contexto con la palabra
  resaltada, y abrir el texto del documento con todas las apariciones marcadas.
- **Bitacora de actividad**: registro de acciones (subir, revisar, aprobar, rechazar,
  publicar, indexar, etc.) con alcance segun el rol.
- **Notificaciones** entre roles segun el paso del flujo.

## Roles

| Rol | Que puede hacer |
|-----|-----------------|
| Admin | Gestiona empresas y usuarios; ve los documentos y la bitacora de todas las empresas |
| Supervisor | Sube documentos y sube versiones corregidas de su empresa |
| Revisor | Revisa los documentos pendientes y los pasa al Aprobador (o los rechaza) |
| Aprobador | Da la aprobacion final a los documentos revisados (o los rechaza) |
| Empleado | Consulta y descarga los documentos vigentes de su empresa |

## Estructura del repositorio

```
gestion-documental/
├── src/
│   ├── dotnet-core/     Modulo Central (.NET 10 MVC)
│   ├── php-app/         Modulo de Consulta (PHP 8)
│   └── node-service/    Modulo de Indexacion (Node.js + TS)
├── db/
│   ├── sqlserver/       Esquema + datos semilla y migraciones (01, 02, 03, 04)
│   ├── postgres/        Esquema + datos semilla (PostgreSQL, se cargan solos)
│   └── mongo/           Inicializacion (MongoDB, se carga sola)
├── docker/
│   ├── central/         Compose del modulo Central (SQL Server + .NET)
│   ├── consulta/        Compose del modulo de Consulta (PostgreSQL + PHP)
│   ├── indexacion/      Compose del modulo de Indexacion (MongoDB + Node)
│   └── .env.example     Plantilla de variables de entorno
├── docs/                Documentacion (Word, diagramas UML)
└── README.md
```

## Requisitos

- Docker Desktop (con WSL2 en Windows)
- Git

## Como levantar el entorno

Cada modulo arranca por separado con su propio compose, pero todos comparten la red
`gd_backbone` para poder comunicarse entre si.

```bash
# 1. Crear la red comun (una sola vez)
docker network create gd_backbone

# 2. Levantar cada modulo desde su carpeta
cd docker/central     && docker compose up -d --build
cd ../consulta        && docker compose up -d --build
cd ../indexacion      && docker compose up -d --build
```

Esto deja 6 contenedores corriendo: 3 aplicaciones (`gd_dotnet`, `gd_php`, `gd_node`)
y 3 bases de datos (`gd_sqlserver`, `gd_postgres`, `gd_mongo`).

### Inicializar las bases de datos

- **PostgreSQL** y **MongoDB** se inicializan **solas** la primera vez (sus scripts de
  `db/postgres` y `db/mongo` se montan como scripts de arranque del contenedor).
- **SQL Server** se inicializa **a mano**: con `gd_sqlserver` ya corriendo, ejecuta los
  scripts de `db/sqlserver` en orden (01 -> 02 -> 03 -> 04). El 01 crea la base
  `CentralDB`, su esquema y los datos de ejemplo; los demas agregan mejoras (control de
  versiones, roles del flujo y reglas de revision).

Ejemplo en Windows / PowerShell:

```powershell
# El 01 crea la base y el esquema (se corre contra el servidor, sin -d)
Get-Content db/sqlserver/01_*.sql | docker exec -i gd_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "Str0ng!Passw0rd_2026" -C

# 02, 03 y 04 se corren contra CentralDB
Get-Content db/sqlserver/02_*.sql | docker exec -i gd_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "Str0ng!Passw0rd_2026" -d CentralDB -C
Get-Content db/sqlserver/03_*.sql | docker exec -i gd_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "Str0ng!Passw0rd_2026" -d CentralDB -C
Get-Content db/sqlserver/04_*.sql | docker exec -i gd_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "Str0ng!Passw0rd_2026" -d CentralDB -C
```

> En imagenes de SQL Server mas antiguas la ruta es `/opt/mssql-tools/bin/sqlcmd` y se
> omite el `-C`.

Apagar un modulo sin borrar datos (desde su carpeta): `docker compose down`
Apagar un modulo y BORRAR sus datos: `docker compose down -v`

## Accesos

| Modulo | URL |
|--------|-----|
| Central (Backend + Admin) | http://localhost:5080 |
| Consulta y Reportes | http://localhost:8080 |
| Indexacion y Busqueda | http://localhost:4000 |

## Usuarios de ejemplo

Todos con la contrasena `Demo123!` (cambialas antes de cualquier uso real).

| Correo | Rol | Empresa |
|--------|-----|---------|
| admin@sistema.com | Admin | — |
| supervisor@metalmex.com | Supervisor | Metalmex |
| revisor@metalmex.com | Revisor | Metalmex |
| director@metalmex.com | Aprobador | Metalmex |
| empleado@metalmex.com | Empleado | Metalmex |

## Roadmap

- [x] Fase 1: Estructura del repo y orquestacion de las bases de datos
- [x] Fase 2: Diseno y scripts de las 3 bases (esquemas + datos semilla)
- [x] Fase 3: Modulo Central .NET 10 MVC (login, usuarios, roles, documentos)
- [x] Fase 4: Control de versiones y flujo de revision/aprobacion en 2 pasos
- [x] Fase 5: Bitacora de actividad
- [x] Fase 6: Modulo de Indexacion (Node + MongoDB) con busqueda de texto y resaltado
- [x] Fase 7: Modulo de Consulta (PHP + PostgreSQL)
- [x] Fase 8: Dockerizacion e integracion end-to-end
- [ ] Fase 9: Documentacion en Word y diagramas UML
