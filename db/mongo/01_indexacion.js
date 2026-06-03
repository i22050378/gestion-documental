// ============================================================
//  Modulo de Indexacion - MongoDB  (base: indexacion_db)
//  Metadatos y etiquetas de los documentos para BUSQUEDA RAPIDA.
//  Central manda aqui la metadata cuando se sube/aprueba un documento.
//  Se puede ejecutar varias veces sin error.
// ============================================================

const dbIdx = db.getSiblingDB('indexacion_db');

// Indices para busqueda rapida (crearlos de nuevo no causa error)
dbIdx.documentos_meta.createIndex({ idEmpresa: 1 });
dbIdx.documentos_meta.createIndex({ etiquetas: 1 });
dbIdx.documentos_meta.createIndex({ titulo: "text", etiquetas: "text" }); // busqueda por texto
dbIdx.documentos_meta.createIndex({ idDocumentoCentral: 1, numeroVersion: 1 }, { unique: true });

// Datos demo (se reemplazan por datos reales al subir/aprobar documentos)
if (dbIdx.documentos_meta.countDocuments() === 0) {
  dbIdx.documentos_meta.insertMany([
    {
      idDocumentoCentral: 1, idVersionCentral: 1, idEmpresa: 1, nombreEmpresa: "Metalmex",
      titulo: "Procedimiento de soldadura", categoria: "Procedimiento", numeroVersion: 1,
      estado: "Obsoleto", etiquetas: ["soldadura", "seguridad"],
      nombreArchivo: "procedimiento_soldadura_v1.pdf", extension: "pdf",
      subidoPor: "Supervisor Metalmex", fechaSubida: new Date(), fechaAprobacion: new Date()
    },
    {
      idDocumentoCentral: 1, idVersionCentral: 2, idEmpresa: 1, nombreEmpresa: "Metalmex",
      titulo: "Procedimiento de soldadura", categoria: "Procedimiento", numeroVersion: 2,
      estado: "Aprobado", etiquetas: ["soldadura", "seguridad", "calidad"],
      nombreArchivo: "procedimiento_soldadura_v2.pdf", extension: "pdf",
      subidoPor: "Supervisor Metalmex", fechaSubida: new Date(), fechaAprobacion: new Date()
    },
    {
      idDocumentoCentral: 2, idVersionCentral: 3, idEmpresa: 1, nombreEmpresa: "Metalmex",
      titulo: "Manual de calidad", categoria: "Manual", numeroVersion: 1,
      estado: "Aprobado", etiquetas: ["calidad", "iso", "manual"],
      nombreArchivo: "manual_calidad_v1.pdf", extension: "pdf",
      subidoPor: "Supervisor Metalmex", fechaSubida: new Date(), fechaAprobacion: new Date()
    }
  ]);
}

print("indexacion_db lista: coleccion documentos_meta con indices y datos demo.");
