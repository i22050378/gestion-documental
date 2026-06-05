<?php
declare(strict_types=1);

// Front controller: punto de entrada unico (patron MVC).
require_once __DIR__ . '/../app/Config.php';
require_once __DIR__ . '/../app/Database.php';
require_once __DIR__ . '/../app/Models/DocumentoAprobado.php';
require_once __DIR__ . '/../app/Controllers/DocumentosController.php';
require_once __DIR__ . '/../app/Controllers/ReportesController.php';

// Enrutado simple por query string: ?r=documentos | ?r=reportes
$ruta = $_GET['r'] ?? 'documentos';

switch ($ruta) {
    case 'reportes':
        (new ReportesController())->index();
        break;
    case 'documentos':
    default:
        (new DocumentosController())->index();
        break;
}
