<?php
declare(strict_types=1);

final class ReportesController
{
    public function index(): void
    {
        $error = null;
        $resumen = [];

        try {
            $resumen = DocumentoAprobado::resumen();
        } catch (Throwable $e) {
            $error = 'No se pudo consultar la base de datos: ' . $e->getMessage();
        }

        $titulo = 'Reporte de cumplimiento';
        $vista  = __DIR__ . '/../Views/reportes.php';
        require __DIR__ . '/../Views/layout.php';
    }
}
