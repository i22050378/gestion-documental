<?php
declare(strict_types=1);

// Configuracion: lee variables de entorno (las define docker-compose).
// Si no existen, usa valores por defecto que apuntan al contenedor de Postgres.
final class Config
{
    public static function db(): array
    {
        return [
            'host' => getenv('DB_HOST') ?: 'postgres',
            'port' => getenv('DB_PORT') ?: '5432',
            'name' => getenv('DB_NAME') ?: 'consulta_db',
            'user' => getenv('DB_USER') ?: 'appuser',
            'pass' => getenv('DB_PASS') ?: 'Postgr3s!2026',
        ];
    }
}
