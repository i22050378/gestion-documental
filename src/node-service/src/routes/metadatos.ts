import { Router, Request, Response } from "express";
import { getDb } from "../db";

export const metadatosRouter = Router();

const COLECCION = "documentos_meta";

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
    if (req.query.q) filtro.$text = { $search: String(req.query.q) };

    const datos = await col
      .find(filtro)
      .sort({ fechaAprobacion: -1 })
      .limit(100)
      .toArray();

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
