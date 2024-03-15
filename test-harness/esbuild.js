const esbuild = require("esbuild");
// include this if you need some node support:
// npm i @esbuild-plugins/node-modules-polyfill --save-dev
// const { NodeModulesPolyfillPlugin } = require('@esbuild-plugins/node-modules-polyfill')

// const { Generator } = require("npm-dts");

// // Generate the typings
// new Generator({
//   entry: "src/index.ts",
//   output: "dist/index.d.ts",
//   tsc: "--extendedDiagnostics",
// }).generate();

esbuild.build({
  entryPoints: ["src/index.ts"],
  bundle: true,
  //minify: true,
  sourcemap: true,
  minify: false,
  outdir: "dist",
  //platform: "node", // for CJS
  format: "cjs", // needs to be CJS for now
  target: ["es2020"], // don't go over es2020 because quickjs doesn't support it
});
/*
build({
  ...sharedConfig,
  outfile: "dist/index.esm.js",
  platform: 'neutral', // for ESM
  format: "esm",
});*/

// esbuild.build({
//   // supports other types like js or ts
//   entryPoints: ["src/index.js"],
//   outdir: "dist",
//   bundle: true,
//   sourcemap: true,
//   //plugins: [NodeModulesPolyfillPlugin()], // include this if you need some node support
//   minify: false, // might want to use true for production build
//   format: "cjs", // needs to be CJS for now
//   target: ["es2020"], // don't go over es2020 because quickjs doesn't support it
// });
