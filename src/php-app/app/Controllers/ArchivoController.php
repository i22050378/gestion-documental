<?php
declare(strict_types=1);

// Sirve el archivo de un documento. Como el archivo vive en el Central (SQL Server),
// este controlador se lo pide al Central por su API interna y lo reenvia al navegador.
final class ArchivoController
{
    public function ver(): void
    {
        $this->servir(false);
    }

    public function descargar(): void
    {
        $this->servir(true);
    }

    private function servir(bool $comoDescarga): void
    {
        $idVersion = (int)($_GET['id'] ?? 0);
        if ($idVersion <= 0) {
            http_response_code(400);
            echo 'Falta el id de version';
            return;
        }

        $doc = DocumentoAprobado::porVersion($idVersion);
        if ($doc === null) {
            http_response_code(404);
            echo 'Documento no encontrado';
            return;
        }

        // Pedir el archivo al modulo Central (.NET) por su API interna protegida con clave.
        $central = getenv('CENTRAL_URL') ?: 'http://host.docker.internal:5080';
        $apiKey  = getenv('API_KEY') ?: '';
        $url = rtrim($central, '/') . '/Api/Archivo/' . $idVersion;

        $ch = curl_init($url);
        curl_setopt_array($ch, [
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HTTPHEADER     => ['X-Api-Key: ' . $apiKey],
            CURLOPT_TIMEOUT        => 20,
        ]);
        $bytes  = curl_exec($ch);
        $status = (int)curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $err    = curl_error($ch);
        curl_close($ch);

        if ($bytes === false || $status !== 200) {
            http_response_code(502);
            echo 'No se pudo obtener el archivo del modulo Central (HTTP ' . $status . '). '
               . htmlspecialchars($err)
               . ' Verifica que la app .NET este corriendo.';
            return;
        }

        $ext    = strtolower((string)$doc['extension']);
        $nombre = (string)$doc['nombre_archivo'];

        header('Content-Type: ' . self::mime($ext));
        header('Content-Length: ' . strlen($bytes));
        $disposicion = $comoDescarga ? 'attachment' : 'inline';
        header('Content-Disposition: ' . $disposicion . '; filename="' . $nombre . '"');
        echo $bytes;
    }

    private static function mime(string $ext): string
    {
        return match (ltrim($ext, '.')) {
            'pdf'         => 'application/pdf',
            'png'         => 'image/png',
            'jpg', 'jpeg' => 'image/jpeg',
            'gif'         => 'image/gif',
            'txt'         => 'text/plain; charset=utf-8',
            default       => 'application/octet-stream',
        };
    }
}
