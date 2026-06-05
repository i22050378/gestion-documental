<?php
declare(strict_types=1);

// Modelo: consultas a la base de Consulta (PostgreSQL).
final class DocumentoAprobado
{
    // Documentos vigentes, con filtros opcionales por titulo, empresa y categoria.
    public static function vigentes(string $q, string $empresa, string $categoria): array
    {
        $sql = 'SELECT * FROM vista_documentos_vigentes WHERE 1=1';
        $params = [];

        if ($q !== '') {
            $sql .= ' AND titulo ILIKE :q';
            $params[':q'] = '%' . $q . '%';
        }
        if ($empresa !== '') {
            $sql .= ' AND nombre_empresa = :empresa';
            $params[':empresa'] = $empresa;
        }
        if ($categoria !== '') {
            $sql .= ' AND categoria = :categoria';
            $params[':categoria'] = $categoria;
        }
        $sql .= ' ORDER BY fecha_aprobacion DESC';

        $stmt = Database::pdo()->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchAll();
    }

    // Valores distintos para poblar los filtros.
    public static function empresas(): array
    {
        $stmt = Database::pdo()->query(
            'SELECT DISTINCT nombre_empresa FROM documentos_aprobados ORDER BY nombre_empresa'
        );
        return array_column($stmt->fetchAll(), 'nombre_empresa');
    }

    public static function categorias(): array
    {
        $stmt = Database::pdo()->query(
            'SELECT DISTINCT categoria FROM documentos_aprobados ORDER BY categoria'
        );
        return array_column($stmt->fetchAll(), 'categoria');
    }

    // Resumen de cumplimiento (documentos vigentes por empresa y categoria).
    public static function resumen(): array
    {
        $stmt = Database::pdo()->query('SELECT * FROM vista_resumen_cumplimiento');
        return $stmt->fetchAll();
    }

    // Recibe un documento aprobado desde el Central y lo guarda/actualiza.
    // Las versiones anteriores del mismo documento dejan de ser vigentes;
    // la version recien aprobada queda como vigente. Todo en una transaccion.
    public static function registrarAprobado(array $d): void
    {
        $pdo = Database::pdo();
        $pdo->beginTransaction();
        try {
            $up = $pdo->prepare(
                'UPDATE documentos_aprobados SET es_vigente = FALSE WHERE id_documento_central = :doc'
            );
            $up->execute([':doc' => (int)$d['idDocumentoCentral']]);

            $sql = 'INSERT INTO documentos_aprobados
                (id_documento_central, id_version_central, id_empresa, nombre_empresa, titulo, categoria,
                 numero_version, nombre_archivo, extension, tamano_bytes, aprobado_por, fecha_aprobacion, es_vigente)
                VALUES
                (:doc, :ver, :emp, :nemp, :tit, :cat, :num, :narch, :ext, :tam, :aprob, :fap, TRUE)
                ON CONFLICT (id_version_central) DO UPDATE SET
                    id_documento_central = EXCLUDED.id_documento_central,
                    id_empresa           = EXCLUDED.id_empresa,
                    nombre_empresa       = EXCLUDED.nombre_empresa,
                    titulo               = EXCLUDED.titulo,
                    categoria            = EXCLUDED.categoria,
                    numero_version       = EXCLUDED.numero_version,
                    nombre_archivo       = EXCLUDED.nombre_archivo,
                    extension            = EXCLUDED.extension,
                    tamano_bytes         = EXCLUDED.tamano_bytes,
                    aprobado_por         = EXCLUDED.aprobado_por,
                    fecha_aprobacion     = EXCLUDED.fecha_aprobacion,
                    es_vigente           = TRUE';
            $stmt = $pdo->prepare($sql);
            $stmt->execute([
                ':doc'   => (int)$d['idDocumentoCentral'],
                ':ver'   => (int)$d['idVersionCentral'],
                ':emp'   => (int)($d['idEmpresa'] ?? 0),
                ':nemp'  => (string)($d['nombreEmpresa'] ?? ''),
                ':tit'   => (string)$d['titulo'],
                ':cat'   => (string)($d['categoria'] ?? ''),
                ':num'   => (int)($d['numeroVersion'] ?? 1),
                ':narch' => (string)($d['nombreArchivo'] ?? ''),
                ':ext'   => (string)($d['extension'] ?? ''),
                ':tam'   => (int)($d['tamanoBytes'] ?? 0),
                ':aprob' => (string)($d['aprobadoPor'] ?? ''),
                ':fap'   => (string)($d['fechaAprobacion'] ?? date('c')),
            ]);

            $pdo->commit();
        } catch (Throwable $e) {
            $pdo->rollBack();
            throw $e;
        }
    }
}
