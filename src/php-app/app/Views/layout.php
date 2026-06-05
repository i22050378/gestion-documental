<?php
/** @var string $titulo */
/** @var string $vista */
?>
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title><?= htmlspecialchars($titulo) ?> - Portal de Consulta</title>
  <style>
    body { font-family: system-ui, sans-serif; margin:0; background:#f5f5f4; color:#1c1c1a; }
    header { background:#0f5132; color:#fff; padding:14px 24px; }
    header h1 { margin:0; font-size:19px; }
    nav.menu { background:#157347; padding:8px 24px; }
    nav.menu a { color:#d1f0dd; margin-right:18px; text-decoration:none; font-size:14px; }
    nav.menu a:hover { text-decoration:underline; }
    main { max-width:980px; margin:24px auto; padding:0 16px; }
    h2 { font-size:17px; margin-top:0; }
    .card { background:#fff; border:1px solid #e5e5e5; border-radius:8px; padding:16px; margin-bottom:20px; }
    table { border-collapse:collapse; width:100%; background:#fff; }
    th,td { text-align:left; padding:8px 12px; border-bottom:1px solid #e5e5e5; font-size:14px; }
    th { background:#e8f3ec; }
    .input { padding:8px; border:1px solid #ccc; border-radius:6px; margin-right:6px; }
    .btn { background:#0f5132; color:#fff; border:none; padding:9px 16px; border-radius:6px; cursor:pointer; font-size:14px; }
    .badge { display:inline-block; padding:2px 8px; border-radius:10px; font-size:12px; background:#e8f3ec; color:#0f5132; }
    .err { color:#991b1b; font-weight:600; }
  </style>
</head>
<body>
  <header><h1>Portal de Consulta y Reportes</h1></header>
  <nav class="menu">
    <a href="?r=documentos">Documentos vigentes</a>
    <a href="?r=reportes">Reporte de cumplimiento</a>
  </nav>
  <main>
    <?php require $vista; ?>
  </main>
</body>
</html>
