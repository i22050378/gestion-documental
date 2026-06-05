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
}
