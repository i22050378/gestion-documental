<?php
declare(strict_types=1);

// Front controller: punto de entrada unico (patron MVC).
require_once __DIR__ . '/../app/Config.php';
require_once __DIR__ . '/../app/Database.php';
require_once __DIR__ . '/../app/Models/DocumentoAprobado.php';
require_once __DIR__ . '/../app/Controllers/DocumentosController.php';
require_once __DIR__ . '/../app/Controllers/ReportesController.php';
require_once __DIR__ . '/../app/Controllers/ApiController.php';

// Enrutado simple por query string:
//   ?r=documentos | ?r=reportes | ?r=api/aprobados (POST, lo llama el Central)
$ruta = $_GET['r'] ?? 'documentos';

switch ($ruta) {
    case 'reportes':
        (new ReportesController())->index();
        break;
    case 'api/aprobados':
        (new ApiController())->aprobados();
        break;
    case 'documentos':
    default:
        (new DocumentosController())->index();
        break;
}
