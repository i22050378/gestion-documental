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
