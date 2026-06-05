import express from "express";
import { config } from "./config";
import { connectDb, pingDb } from "./db";
import { metadatosRouter } from "./routes/metadatos";

function main(): void {
  const app = express();
  app.use(express.json());

  // Pagina raiz: dice que es y que endpoints tiene.
  app.get("/", (_req, res) => {
    res.json({
      servicio: "Modulo de Indexacion y Metadatos",
      endpoints: ["/health", "/api/metadatos", "/api/documentos/:id/versiones"],
    });
  });

  // Health check: dice si el servicio vive y si Mongo responde.
  app.get("/health", async (_req, res) => {
    const dbOk = await pingDb();
    res.json({ ok: true, servicio: "indexacion", db: dbOk ? "conectada" : "desconectada" });
  });

  app.use("/api", metadatosRouter);

  // 1) Arrancamos el servidor HTTP de inmediato (responde aunque Mongo tarde).
  app.listen(config.port, () => {
    console.log(`Servicio de indexacion escuchando en http://localhost:${config.port}`);
  });

  // 2) Conectamos a Mongo en segundo plano. Si falla, avisamos pero el
  //    servidor sigue arriba (puedes ver el estado en /health).
  connectDb().catch((err) => {
    console.error("[mongo] No se pudo conectar:", err);
    console.error("[mongo] Revisa que el contenedor gd_mongo este corriendo (docker compose ps).");
  });
}

main();
