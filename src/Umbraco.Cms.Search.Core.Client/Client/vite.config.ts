import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "search-bundle": "src/bundle/search-bundle.ts",
        "search-global": "src/global/search-global.ts",
        "search-core": "src/core/search-core.ts"
      }, // Bundle registers one or more manifests
      formats: ["es"],
    },
    outDir: "../wwwroot/App_Plugins/UmbracoSearch", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [
        /^@umbraco/,
      ]
    },
  },
});
