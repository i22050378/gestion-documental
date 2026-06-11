import { Router, Request, Response } from "express";
import { getDb } from "../db";

export const metadatosRouter = Router();

const COLECCION = "documentos_meta";

// Busca todas las apariciones de un termino dentro de un texto y devuelve
// cuantas veces aparece y fragmentos de contexto (lo de antes y lo de despues
// de cada aparicion) para poder mostrar DONDE esta la palabra en el documento.
function analizarCuerpo(texto: string, termEscapado: string, maxFragmentos = 6) {
  const fragmentos: Array<{ antes: string; coincidencia: string; despues: string; porcentaje: number }> = [];
  let veces = 0;
  if (!texto || !termEscapado) return { veces, fragmentos };
  const CTX = 70; // cuantos caracteres de contexto a cada lado
  const rx = new RegExp(termEscapado, "gi");
  let m: RegExpExecArray | null;
  while ((m = rx.exec(texto)) !== null) {
    veces++;
    if (fragmentos.length < maxFragmentos) {
      const ini = m.index;
      const fin = ini + m[0].length;
      const antes = texto.slice(Math.max(0, ini - CTX), ini).replace(/\s+/g, " ");
      const despues = texto.slice(fin, fin + CTX).replace(/\s+/g, " ");
      fragmentos.push({
        antes: (ini - CTX > 0 ? "\u2026 " : "") + antes,
        coincidencia: m[0],
        despues: despues + (fin + CTX < texto.length ? " \u2026" : ""),
        porcentaje: texto.length > 0 ? Math.round((ini / texto.length) * 100) : 0,
      });
    }
    if (m.index === rx.lastIndex) rx.lastIndex++; // evita bucle en coincidencia vacia
  }
  return { veces, fragmentos };
}

// Escapa texto para insertarlo de forma segura dentro del HTML.
function escHtml(s: string): string {
  return String(s).replace(/[&<>"']/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c] as string)
  );
}

// GET /api/metadatos
//   Lista metadatos. Filtros opcionales por query string:
//     ?empresa=1        -> solo de esa empresa
//     ?etiqueta=soldadura -> que tengan esa etiqueta
//     ?q=texto          -> busqueda de texto (usa el indice de texto de Mongo)
metadatosRouter.get("/metadatos", async (req: Request, res: Response) => {
  try {
    const col = getDb().collection(COLECCION);
    const filtro: Record<string, unknown> = {};

    if (req.query.empresa) filtro.idEmpresa = Number(req.query.empresa);
    if (req.query.etiqueta) filtro.etiquetas = String(req.query.etiqueta);
    const termRaw = req.query.q ? String(req.query.q) : "";
    const termEsc = termRaw.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    if (termRaw) {
      // Busca el texto en el titulo, las etiquetas y DENTRO del contenido del documento.
      const rx = new RegExp(termEsc, "i");
      filtro.$or = [{ titulo: rx }, { etiquetas: rx }, { textoCompleto: rx }];
    }

    const crudos = await col
      .find(filtro)
      .sort({ fechaAprobacion: -1 })
      .limit(100)
      .toArray();

    const rxTitulo = termRaw ? new RegExp(termEsc, "i") : null;

    // El texto completo puede ser largo: no lo mandamos entero en la lista,
    // solo avisamos si existe (tieneTexto). Ademas, si hay busqueda, agregamos
    // "coincidencias": si la palabra esta en el titulo/etiquetas/cuerpo, cuantas
    // veces aparece en el cuerpo y fragmentos de contexto de donde aparece.
    const datos = crudos.map((d) => {
      const obj = d as Record<string, unknown>;
      const { textoCompleto: tc, ...resto } = obj;
      const textoCompleto = typeof tc === "string" ? tc : "";
      const base = { ...resto, tieneTexto: textoCompleto.length > 0 };
      if (!termRaw) return base;

      const enTitulo = rxTitulo!.test(String(obj.titulo ?? ""));
      const enEtiquetas =
        Array.isArray(obj.etiquetas) &&
        (obj.etiquetas as unknown[]).some((t) => rxTitulo!.test(String(t)));
      const { veces, fragmentos } = analizarCuerpo(textoCompleto, termEsc);

      return {
        ...base,
        coincidencias: {
          termino: termRaw,
          enTitulo,
          enEtiquetas,
          enCuerpo: veces > 0,
          vecesEnCuerpo: veces,
          fragmentos,
        },
      };
    });

    res.json({ total: datos.length, datos });
  } catch (err) {
    res.status(500).json({ error: "Error consultando metadatos", detalle: String(err) });
  }
});

// GET /api/documentos/:id/versiones
//   Todas las versiones (metadatos) de un documento del modulo Central.
metadatosRouter.get("/documentos/:id/versiones", async (req: Request, res: Response) => {
  try {
    const id = Number(req.params.id);
    const col = getDb().collection(COLECCION);
    const versiones = await col
      .find({ idDocumentoCentral: id })
      .sort({ numeroVersion: -1 })
      .toArray();

    res.json({ idDocumentoCentral: id, total: versiones.length, versiones });
  } catch (err) {
    res.status(500).json({ error: "Error consultando versiones", detalle: String(err) });
  }
});

// POST /api/metadatos
//   Crea o actualiza (upsert) el metadato de una version. Este es el endpoint
//   que el modulo Central llamara al aprobar un documento. La clave para no
//   duplicar es (idDocumentoCentral + numeroVersion), que tiene indice unico.
metadatosRouter.post("/metadatos", async (req: Request, res: Response) => {
  try {
    const b = req.body ?? {};

    // Validacion minima de los campos imprescindibles.
    const requeridos = ["idDocumentoCentral", "idVersionCentral", "numeroVersion", "titulo"];
    const faltantes = requeridos.filter((k) => b[k] === undefined || b[k] === null || b[k] === "");
    if (faltantes.length > 0) {
      return res.status(400).json({ error: "Faltan campos requeridos", campos: faltantes });
    }

    const doc = {
      idDocumentoCentral: Number(b.idDocumentoCentral),
      idVersionCentral: Number(b.idVersionCentral),
      idEmpresa: Number(b.idEmpresa ?? 0),
      nombreEmpresa: String(b.nombreEmpresa ?? ""),
      titulo: String(b.titulo),
      categoria: String(b.categoria ?? ""),
      numeroVersion: Number(b.numeroVersion),
      estado: String(b.estado ?? "Aprobado"),
      etiquetas: Array.isArray(b.etiquetas) ? b.etiquetas.map(String) : [],
      nombreArchivo: String(b.nombreArchivo ?? ""),
      extension: String(b.extension ?? ""),
      textoCompleto: typeof b.textoCompleto === "string" ? b.textoCompleto : "",
      subidoPor: String(b.subidoPor ?? ""),
      fechaSubida: b.fechaSubida ? new Date(b.fechaSubida) : new Date(),
      fechaAprobacion: b.fechaAprobacion ? new Date(b.fechaAprobacion) : new Date(),
      fechaIndexado: new Date(),
    };

    const col = getDb().collection(COLECCION);
    const resultado = await col.updateOne(
      { idDocumentoCentral: doc.idDocumentoCentral, numeroVersion: doc.numeroVersion },
      { $set: doc },
      { upsert: true }
    );

    res.status(200).json({
      ok: true,
      operacion: resultado.upsertedCount > 0 ? "insertado" : "actualizado",
      idDocumentoCentral: doc.idDocumentoCentral,
      numeroVersion: doc.numeroVersion,
    });
  } catch (err) {
    res.status(500).json({ error: "Error guardando metadato", detalle: String(err) });
  }
});

// GET /api/versiones/:idVersion/texto
//   Devuelve el texto extraido del documento. Con ?descargar=1 lo baja como .txt.
metadatosRouter.get("/versiones/:idVersion/texto", async (req: Request, res: Response) => {
  try {
    const idVersion = Number(req.params.idVersion);
    const col = getDb().collection(COLECCION);
    const doc = await col.findOne({ idVersionCentral: idVersion });

    if (!doc) {
      return res.status(404).type("text/plain; charset=utf-8").send("Documento no encontrado.");
    }

    const texto = typeof doc.textoCompleto === "string" ? doc.textoCompleto : "";
    res.type("text/plain; charset=utf-8");
    if (req.query.descargar) {
      res.setHeader("Content-Disposition", `attachment; filename="texto-v${idVersion}.txt"`);
    }
    res.send(texto.length > 0 ? texto : "(Este documento no tiene texto extraido.)");
  } catch (err) {
    res.status(500).type("text/plain; charset=utf-8").send("Error obteniendo el texto: " + String(err));
  }
});

// GET /api/versiones/:idVersion/ver
//   Pagina HTML que muestra el texto extraido del documento. Con ?resaltar=palabra
//   resalta todas las apariciones de esa palabra y deja saltar entre ellas
//   (Anterior / Siguiente). Es la "visualizacion" de donde esta la palabra.
metadatosRouter.get("/versiones/:idVersion/ver", async (req: Request, res: Response) => {
  try {
    const idVersion = Number(req.params.idVersion);
    const col = getDb().collection(COLECCION);
    const doc = await col.findOne({ idVersionCentral: idVersion });
    if (!doc) {
      return res
        .status(404)
        .type("text/html; charset=utf-8")
        .send("<p style='font-family:sans-serif'>Documento no encontrado.</p>");
    }

    const texto = typeof doc.textoCompleto === "string" ? doc.textoCompleto : "";
    const titulo = String(doc.titulo ?? "Documento");
    const archivo = String(doc.nombreArchivo ?? "");
    const term = req.query.resaltar ? String(req.query.resaltar) : "";

    let cuerpoHtml = "";
    let total = 0;
    if (texto.length === 0) {
      cuerpoHtml = "<em>(Este documento no tiene texto extraido.)</em>";
    } else if (term) {
      const termEsc = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
      const rx = new RegExp(termEsc, "gi");
      let last = 0;
      let m: RegExpExecArray | null;
      while ((m = rx.exec(texto)) !== null) {
        cuerpoHtml += escHtml(texto.slice(last, m.index));
        cuerpoHtml += '<mark id="m' + total + '">' + escHtml(m[0]) + "</mark>";
        last = m.index + m[0].length;
        total++;
        if (m.index === rx.lastIndex) rx.lastIndex++;
      }
      cuerpoHtml += escHtml(texto.slice(last));
    } else {
      cuerpoHtml = escHtml(texto);
    }

    const barra = term
      ? '<div class="barra"><span><b>' + total + '</b> coincidencia(s) de "<b>' + escHtml(term) +
        '</b>"</span><button class="nav" onclick="ir(-1)">\u25C0 Anterior</button>' +
        '<button class="nav" onclick="ir(1)">Siguiente \u25B6</button>' +
        '<span id="pos" class="pos"></span></div>'
      : "";

    const page =
      '<!DOCTYPE html><html lang="es"><head><meta charset="utf-8">' +
      '<meta name="viewport" content="width=device-width, initial-scale=1.0">' +
      "<title>Texto - " + escHtml(titulo) + "</title><style>" +
      "body{font-family:system-ui,sans-serif;margin:0;background:#f4f6fb;color:#1c1c28;}" +
      "header{background:#312e81;color:#fff;padding:14px 20px;position:sticky;top:0;z-index:2;}" +
      "header h1{margin:0;font-size:16px;}header p{margin:4px 0 0;font-size:12.5px;color:#c7c9f0;}" +
      ".barra{background:#eef2ff;border-bottom:1px solid #e0e7ff;padding:8px 20px;font-size:13px;" +
      "color:#3730a3;display:flex;gap:10px;align-items:center;flex-wrap:wrap;position:sticky;top:54px;z-index:1;}" +
      ".barra b{color:#1e1b4b;}.pos{color:#64748b;}" +
      ".nav{background:#4f46e5;color:#fff;border:none;padding:5px 12px;border-radius:7px;cursor:pointer;font-size:13px;}" +
      ".nav:hover{background:#4338ca;}" +
      "pre.texto{white-space:pre-wrap;word-wrap:break-word;font-family:ui-monospace,Consolas,monospace;" +
      "font-size:13.5px;line-height:1.55;background:#fff;border:1px solid #e5e7eb;border-radius:10px;margin:18px;padding:18px;}" +
      "mark{background:#fde047;color:#1c1c28;padding:0 2px;border-radius:2px;}" +
      "mark.activa{background:#fb923c;outline:2px solid #ea580c;}" +
      "</style></head><body>" +
      "<header><h1>" + escHtml(titulo) + "</h1><p>" + escHtml(archivo) + "</p></header>" +
      barra +
      '<pre class="texto" id="texto">' + cuerpoHtml + "</pre>" +
      "<script>" +
      'var marks=Array.prototype.slice.call(document.querySelectorAll("mark"));var i=-1;' +
      'function foco(n){if(!marks.length)return;if(i>=0&&marks[i])marks[i].className="";' +
      'i=(n+marks.length)%marks.length;marks[i].className="activa";' +
      'marks[i].scrollIntoView({behavior:"smooth",block:"center"});' +
      'var p=document.getElementById("pos");if(p)p.textContent=(i+1)+" / "+marks.length;}' +
      "function ir(d){foco(i<0?0:i+d);}" +
      "if(marks.length){setTimeout(function(){foco(0);},150);}" +
      "</script></body></html>";

    res.type("text/html; charset=utf-8").send(page);
  } catch (err) {
    res
      .status(500)
      .type("text/html; charset=utf-8")
      .send("<p>Error obteniendo el texto: " + escHtml(String(err)) + "</p>");
  }
});
