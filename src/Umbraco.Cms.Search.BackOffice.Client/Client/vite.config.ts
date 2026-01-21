import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/bundle.manifests.ts", // Bundle registers one or more manifests
      formats: ["es"],
      fileName: "umbraco-cms-search-back-office-client",
    },
    outDir: "../wwwroot/App_Plugins/UmbracoCmsSearchBackOfficeClient", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
      output: {
        manualChunks: undefined,
        dynamicImportInCjs: false
      }
    },
  },
});
