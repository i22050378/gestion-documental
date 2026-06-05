import dotenv from "dotenv";

dotenv.config();

// Configuracion del servicio. Usa variables de entorno (.env) y, si no existen,
// valores por defecto que apuntan al contenedor de Mongo en tu maquina.
export const config = {
  port: parseInt(process.env.PORT || "4000", 10),
  mongoUri: process.env.MONGO_URI || "mongodb://localhost:27017",
  mongoUser: process.env.MONGO_USER || "mongoadmin",
  mongoPass: process.env.MONGO_PASS || "M0ngo!2026",
  mongoAuthSource: process.env.MONGO_AUTH_SOURCE || "admin",
  mongoDb: process.env.MONGO_DB || "indexacion_db",
};
