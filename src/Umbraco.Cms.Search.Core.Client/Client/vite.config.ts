import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "search-bundle": "src/bundle/search-bundle.ts",
        "search-library": "src/lib/search-library.ts"
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
