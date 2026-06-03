# Sistema Integral de Gestion Documental para Normativas de Calidad

Plataforma para la gestion, control de versiones y trazabilidad de documentos de
calidad (manuales, procedimientos, registros, auditorias). Arquitectura multi-stack
dividida en 3 modulos, cada uno con su propia base de datos, orquestados con Docker Compose.

## Arquitectura

| Modulo | Tecnologia | Base de datos | Responsabilidad |
|--------|-----------|---------------|-----------------|
| Central (Backend + Admin) | .NET 10 MVC (C#) | SQL Server | Login, usuarios, roles, flujo de aprobacion, versiones, bitacora |
| Consulta y Reportes | PHP 8 (MVC) | PostgreSQL | Consulta de documentos vigentes, descargas, reportes |
| Indexacion y Metadatos | Node.js + TypeScript | MongoDB | Busqueda rapida, metadatos, etiquetas |

El Modulo Central es la "fuente de la verdad". Al aprobarse un documento, notifica
via API al modulo de Indexacion (guarda metadatos en MongoDB) y al de Consulta
(deja el documento vigente en PostgreSQL). Las bases NO se comunican directamente
entre si: lo hacen a traves de las APIs de los modulos.

## Estructura del repositorio

```
gestion-documental/
├── src/
│   ├── dotnet-core/     Modulo Central (.NET 10 MVC)
│   ├── php-app/         Modulo de Consulta (PHP 8)
│   └── node-service/    Modulo de Indexacion (Node.js + TS)
├── db/
│   ├── sqlserver/       Scripts de esquema y datos semilla (SQL Server)
│   ├── postgres/        Scripts de esquema y datos semilla (PostgreSQL)
│   └── mongo/           Scripts de inicializacion (MongoDB)
├── docker/
│   ├── docker-compose.yml   Orquestacion de todo el entorno
│   ├── .env.example         Plantilla de variables de entorno
│   └── .env                 Variables reales (NO se sube a git)
├── docs/                Documentacion (Word, diagramas UML)
└── README.md
```

## Requisitos

- Docker Desktop (con WSL2 en Windows)
- Git

## Como levantar el entorno (estado actual: solo bases de datos)

```bash
# 1. Entra a la carpeta docker
cd docker

# 2. Si no existe .env, copialo de la plantilla
copy .env.example .env        # (Windows)   /   cp .env.example .env (Linux/Mac)

# 3. Levanta las 3 bases de datos
docker compose up -d

# 4. Revisa que esten corriendo
docker compose ps
```

Apagar sin borrar datos:        `docker compose down`
Apagar y BORRAR los datos:      `docker compose down -v`

## Credenciales por defecto

Estan en `docker/.env.example`. Cambialas antes de cualquier uso real.

## Roadmap

- [x] Fase 1: Estructura del repo y orquestacion de las 3 bases de datos
- [ ] Fase 2: Diseno y scripts de las 3 bases (esquemas + datos semilla)
- [ ] Fase 3: Modulo Central .NET 10 MVC (login, usuarios, roles, documentos)
- [ ] Fase 4: Flujo de aprobacion y control de versiones
- [ ] Fase 5: Bitacora de actividad
- [ ] Fase 6: Modulo de Indexacion (Node + MongoDB)
- [ ] Fase 7: Modulo de Consulta (PHP + PostgreSQL)
- [ ] Fase 8: Dockerizacion final e integracion end-to-end
- [ ] Fase 9: Documentacion en Word y diagramas UML
