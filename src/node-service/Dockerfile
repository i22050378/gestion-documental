# Imagen para el Modulo de Indexacion (Node.js + TypeScript)
FROM node:22

WORKDIR /app

# Instalar dependencias (se cachea si no cambia package.json)
COPY package*.json ./
RUN npm install

# Copiar el codigo (el .dockerignore evita copiar node_modules)
COPY . .

EXPOSE 4000

# Arranca el servidor (tsx ejecuta TypeScript directamente)
CMD ["npx", "tsx", "src/server.ts"]
