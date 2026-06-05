// Forma de un documento de metadatos en la coleccion documentos_meta.
// Mongo es flexible (NoSQL), pero definimos la interfaz para tener autocompletado
// y dejar claro que campos manejamos.
export interface DocumentoMeta {
  idDocumentoCentral: number;
  idVersionCentral: number;
  idEmpresa: number;
  nombreEmpresa: string;
  titulo: string;
  categoria: string;
  numeroVersion: number;
  estado: string;
  etiquetas: string[];
  nombreArchivo: string;
  extension: string;
  subidoPor: string;
  fechaSubida: Date | string;
  fechaAprobacion: Date | string;
}
