<?php
declare(strict_types=1);

final class DocumentosController
{
    public function index(): void
    {
        $q         = trim((string)($_GET['q'] ?? ''));
        $empresa   = trim((string)($_GET['empresa'] ?? ''));
        $categoria = trim((string)($_GET['categoria'] ?? ''));

        $error = null;
        $documentos = [];
        $empresas = [];
        $categorias = [];

        try {
            $documentos = DocumentoAprobado::vigentes($q, $empresa, $categoria);
            $empresas   = DocumentoAprobado::empresas();
            $categorias = DocumentoAprobado::categorias();
        } catch (Throwable $e) {
            $error = 'No se pudo consultar la base de datos: ' . $e->getMessage();
        }

        $titulo = 'Documentos vigentes';
        $vista  = __DIR__ . '/../Views/documentos.php';
        require __DIR__ . '/../Views/layout.php';
    }
}
