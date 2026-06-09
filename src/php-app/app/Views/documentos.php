<?php
/** @var array $documentos */
/** @var array $empresas */
/** @var array $categorias */
/** @var ?string $error */
/** @var string $q */
/** @var string $empresa */
/** @var string $categoria */
?>
<div class="card">
  <h2>Documentos vigentes</h2>

  <?php if ($error !== null): ?>
    <p class="err"><?= htmlspecialchars($error) ?></p>
  <?php endif; ?>

  <form method="get" style="margin-bottom:14px;">
    <input type="hidden" name="r" value="documentos">
    <input class="input" type="text" name="q" placeholder="Buscar por titulo" value="<?= htmlspecialchars($q) ?>">
    <select class="input" name="empresa">
      <option value="">Todas las empresas</option>
      <?php foreach ($empresas as $e): ?>
        <option value="<?= htmlspecialchars($e) ?>"<?= $e === $empresa ? ' selected' : '' ?>><?= htmlspecialchars($e) ?></option>
      <?php endforeach; ?>
    </select>
    <select class="input" name="categoria">
      <option value="">Todas las categorias</option>
      <?php foreach ($categorias as $c): ?>
        <option value="<?= htmlspecialchars($c) ?>"<?= $c === $categoria ? ' selected' : '' ?>><?= htmlspecialchars($c) ?></option>
      <?php endforeach; ?>
    </select>
    <button class="btn" type="submit">Filtrar</button>
  </form>

  <?php if (count($documentos) === 0): ?>
    <p>No hay documentos vigentes que coincidan.</p>
  <?php else: ?>
    <table>
      <thead>
        <tr><th>Titulo</th><th>Empresa</th><th>Categoria</th><th>Version</th><th>Aprobado por</th><th>Fecha</th><th>Archivo</th></tr>
      </thead>
      <tbody>
        <?php foreach ($documentos as $d): ?>
          <tr>
            <td><?= htmlspecialchars($d['titulo']) ?></td>
            <td><?= htmlspecialchars($d['nombre_empresa']) ?></td>
            <td><span class="badge"><?= htmlspecialchars($d['categoria']) ?></span></td>
            <td><?= htmlspecialchars((string)$d['numero_version']) ?>.0</td>
            <td><?= htmlspecialchars($d['aprobado_por']) ?></td>
            <td><?= htmlspecialchars(substr((string)$d['fecha_aprobacion'], 0, 10)) ?></td>
            <td>
              <?php $ext = strtolower((string)$d['extension']); ?>
              <?php if (in_array($ext, ['pdf', 'png', 'jpg', 'jpeg', 'gif', 'txt'], true)): ?>
                <a href="?r=ver&id=<?= (int)$d['id_version_central'] ?>" target="_blank">Ver</a> &nbsp;
              <?php endif; ?>
              <a href="?r=descargar&id=<?= (int)$d['id_version_central'] ?>">Descargar</a>
            </td>
          </tr>
        <?php endforeach; ?>
      </tbody>
    </table>
  <?php endif; ?>
</div>
