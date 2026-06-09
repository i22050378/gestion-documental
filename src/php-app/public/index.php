<?php
declare(strict_types=1);

// Front controller: punto de entrada unico (patron MVC).
require_once __DIR__ . '/../app/Config.php';
require_once __DIR__ . '/../app/Database.php';
require_once __DIR__ . '/../app/Models/DocumentoAprobado.php';
require_once __DIR__ . '/../app/Controllers/DocumentosController.php';
require_once __DIR__ . '/../app/Controllers/ReportesController.php';
require_once __DIR__ . '/../app/Controllers/ApiController.php';
require_once __DIR__ . '/../app/Controllers/ArchivoController.php';

// Enrutado simple por query string:
//   ?r=documentos | ?r=reportes | ?r=ver&id=N | ?r=descargar&id=N | ?r=api/aprobados (POST)
$ruta = $_GET['r'] ?? 'documentos';

switch ($ruta) {
    case 'reportes':
        (new ReportesController())->index();
        break;
    case 'ver':
        (new ArchivoController())->ver();
        break;
    case 'descargar':
        (new ArchivoController())->descargar();
        break;
    case 'api/aprobados':
        (new ApiController())->aprobados();
        break;
    case 'documentos':
    default:
        (new DocumentosController())->index();
        break;
}
