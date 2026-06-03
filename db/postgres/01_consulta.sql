-- ============================================================
--  Modulo de Consulta - PostgreSQL  (base: consulta_db)
--  Espejo de solo lectura de los documentos APROBADOS que
--  Central publica. De aqui consultan los operarios y salen
--  los reportes. El archivo en si NO vive aqui (lo guarda Central).
--  Se puede ejecutar varias veces sin error.
-- ============================================================

-- Tabla principal: todas las versiones aprobadas (vigentes + historial)
CREATE TABLE IF NOT EXISTS documentos_aprobados (
    id                   SERIAL PRIMARY KEY,
    id_documento_central INTEGER      NOT NULL,          -- id del documento en SQL Server
    id_version_central   INTEGER      NOT NULL UNIQUE,   -- id de la version en SQL Server
    id_empresa           INTEGER      NOT NULL,
    nombre_empresa       VARCHAR(150) NOT NULL,
    titulo               VARCHAR(200) NOT NULL,
    categoria            VARCHAR(60)  NOT NULL,
    numero_version       INTEGER      NOT NULL,
    nombre_archivo       VARCHAR(255) NOT NULL,
    extension            VARCHAR(20)  NOT NULL,
    tamano_bytes         BIGINT       NOT NULL,
    aprobado_por         VARCHAR(150) NOT NULL,
    fecha_aprobacion     TIMESTAMPTZ  NOT NULL,
    es_vigente           BOOLEAN      NOT NULL DEFAULT TRUE,  -- TRUE = version actual; FALSE = aprobada anterior
    fecha_registro       TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_aprob_empresa ON documentos_aprobados(id_empresa);
CREATE INDEX IF NOT EXISTS ix_aprob_doc     ON documentos_aprobados(id_documento_central);
CREATE INDEX IF NOT EXISTS ix_aprob_vigente ON documentos_aprobados(es_vigente);

-- Vista: solo los documentos vigentes (la version aprobada actual de cada documento)
CREATE OR REPLACE VIEW vista_documentos_vigentes AS
SELECT id, id_documento_central, id_empresa, nombre_empresa, titulo, categoria,
       numero_version, nombre_archivo, extension, tamano_bytes, aprobado_por, fecha_aprobacion
FROM documentos_aprobados
WHERE es_vigente = TRUE;

-- Vista: resumen de cumplimiento (cuantos documentos vigentes por empresa y categoria)
CREATE OR REPLACE VIEW vista_resumen_cumplimiento AS
SELECT nombre_empresa, categoria, COUNT(*) AS total_vigentes
FROM documentos_aprobados
WHERE es_vigente = TRUE
GROUP BY nombre_empresa, categoria
ORDER BY nombre_empresa, categoria;

-- Datos demo (se reemplazan por datos reales cuando se aprueben documentos)
INSERT INTO documentos_aprobados
 (id_documento_central, id_version_central, id_empresa, nombre_empresa, titulo, categoria,
  numero_version, nombre_archivo, extension, tamano_bytes, aprobado_por, fecha_aprobacion, es_vigente)
VALUES
 (1, 1, 1, 'Metalmex', 'Procedimiento de soldadura', 'Procedimiento', 1,
  'procedimiento_soldadura_v1.pdf', 'pdf', 102400, 'Director Metalmex', now() - interval '30 days', FALSE),
 (1, 2, 1, 'Metalmex', 'Procedimiento de soldadura', 'Procedimiento', 2,
  'procedimiento_soldadura_v2.pdf', 'pdf', 110592, 'Director Metalmex', now() - interval '2 days', TRUE),
 (2, 3, 1, 'Metalmex', 'Manual de calidad', 'Manual', 1,
  'manual_calidad_v1.pdf', 'pdf', 256000, 'Director Metalmex', now() - interval '10 days', TRUE)
ON CONFLICT (id_version_central) DO NOTHING;
