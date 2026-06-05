import { MongoClient, Db } from "mongodb";
import { config } from "./config";

let client: MongoClient | null = null;
let db: Db | null = null;

// Abre la conexion a MongoDB. Se llama una vez al arrancar el servidor.
export async function connectDb(): Promise<void> {
  client = new MongoClient(config.mongoUri, {
    auth: { username: config.mongoUser, password: config.mongoPass },
    authSource: config.mongoAuthSource,
    serverSelectionTimeoutMS: 5000,
  });
  await client.connect();
  db = client.db(config.mongoDb);
  console.log(`[mongo] conectado a la base "${config.mongoDb}"`);
}

// Devuelve la base ya conectada (para usarla en las rutas).
export function getDb(): Db {
  if (!db) {
    throw new Error("La base de datos aun no esta conectada");
  }
  return db;
}

// Comprueba si Mongo responde (para el endpoint /health).
export async function pingDb(): Promise<boolean> {
  if (!client) return false;
  try {
    await client.db(config.mongoDb).command({ ping: 1 });
    return true;
  } catch {
    return false;
  }
}
