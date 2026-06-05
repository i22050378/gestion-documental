# Imagen para el Modulo de Consulta (PHP 8 + PostgreSQL)
FROM php:8.3-cli

# Instalar la extension de PostgreSQL para PDO (necesita libpq-dev)
RUN apt-get update \
 && apt-get install -y --no-install-recommends libpq-dev \
 && docker-php-ext-install pdo pdo_pgsql \
 && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY . /app

EXPOSE 8080

# Servidor web de desarrollo de PHP, sirviendo la carpeta public/
CMD ["php", "-S", "0.0.0.0:8080", "-t", "public"]
