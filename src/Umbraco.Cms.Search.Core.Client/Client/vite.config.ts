import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "search-bundle": "src/bundle.manifests.ts",
        "search-library": "src/index.ts"
      }, // Bundle registers one or more manifests
      formats: ["es"],
    },
    outDir: "../wwwroot/App_Plugins/UmbracoSearch", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [
        /^@umbraco/,
        '/App_Plugins/UmbracoSearch/search-library.js'
      ]
    },
  },
});
