import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// O build vai direto para o wwwroot da API: em produção a API serve o React.
// Em desenvolvimento (npm run dev), o Vite faz proxy de /api para a API em :5080.
export default defineConfig({
  plugins: [react()],
  build: { outDir: "../Alibi.Api/wwwroot", emptyOutDir: true },
  server: { proxy: { "/api": "http://localhost:5165", "/docs": "http://localhost:5165", "/openapi": "http://localhost:5165" } },
});
