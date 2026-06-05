<?php
/** @var array $resumen */
/** @var ?string $error */
?>
<div class="card">
  <h2>Reporte de cumplimiento</h2>
  <p style="font-size:13px;color:#555;">Cantidad de documentos vigentes por empresa y categoria.</p>

  <?php if ($error !== null): ?>
    <p class="err"><?= htmlspecialchars($error) ?></p>
  <?php endif; ?>

  <?php if (count($resumen) === 0): ?>
    <p>No hay datos para el reporte.</p>
  <?php else: ?>
    <table>
      <thead><tr><th>Empresa</th><th>Categoria</th><th>Documentos vigentes</th></tr></thead>
      <tbody>
        <?php foreach ($resumen as $r): ?>
          <tr>
            <td><?= htmlspecialchars($r['nombre_empresa']) ?></td>
            <td><?= htmlspecialchars($r['categoria']) ?></td>
            <td><?= htmlspecialchars((string)$r['total_vigentes']) ?></td>
          </tr>
        <?php endforeach; ?>
      </tbody>
    </table>
  <?php endif; ?>
</div>
