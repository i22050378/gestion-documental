<?php
declare(strict_types=1);

// API interna: la usa el modulo Central (.NET) para publicar documentos aprobados.
final class ApiController
{
    public function aprobados(): void
    {
        header('Content-Type: application/json');

        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            echo json_encode(['error' => 'Usa POST']);
            return;
        }

        $raw = file_get_contents('php://input');
        $b = json_decode($raw, true);
        if (!is_array($b)) {
            http_response_code(400);
            echo json_encode(['error' => 'JSON invalido']);
            return;
        }

        foreach (['idDocumentoCentral', 'idVersionCentral', 'titulo'] as $campo) {
            if (!isset($b[$campo]) || $b[$campo] === '') {
                http_response_code(400);
                echo json_encode(['error' => 'Falta el campo: ' . $campo]);
                return;
            }
        }

        try {
            DocumentoAprobado::registrarAprobado($b);
            echo json_encode(['ok' => true, 'idVersionCentral' => (int)$b['idVersionCentral']]);
        } catch (Throwable $e) {
            http_response_code(500);
            echo json_encode(['error' => 'Error guardando', 'detalle' => $e->getMessage()]);
        }
    }
}
