import { distance, closest } from "fastest-levenshtein";

// this function is private to the module
function privateFunc() {
  return "world";
}

function registerFn(fn: Function) {
  // TODO: have this method called registerFunction and have it return a function that takes no params and returns an I32
  // and generate the .d.ts file from the function signature.

  // generate the .d.ts file from the function signature.
  /*fs.writeFileSync(
    "src/index.d.ts",
    `declare module "main" {
  export function get_closest(): I32;
  export function strlen(): I32;
    }`
  );*/

  return function () {
    let input = Host.inputString();
    // TODO: try and parse as JSON or messagepack, and if it fails, just use the string directly.
    // or, don't do magic and make different registration functions for each.
    let result = fn(input);
    // TODO: depending on the return type of the function, use outputBytes or outputString
    Host.outputString(result);
  };
}

export var strlen = registerFn((input: string) => input.length.toString());

export var get_closest = registerFn(
  (input: string) =>
    `${closest(input, ["slow", "faster", "fastest"])} ${privateFunc()}`
);

// why doesn't this work? is math.random not working?
// why don't we see the output from teh console.log? is it because of extism call or because of the wrapping?
export var b = registerFn((input: string) => {
  console.log("randomstr called");
  var length = parseInt(input);
  let result = "";
  const characters =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  const charactersLength = characters.length;
  let counter = 0;
  while (counter < length) {
    result += characters.charAt(Math.floor(Math.random() * charactersLength));
    counter += 1;
  }
  console.log(`Length: ${length}`);
  return length.toString() + " " + result;
});

// the downside to this type of wrapping is that it happens at runtime. also it doesn't seem to work right.
/*
export var get_closest = () =>
  withExtism(
    async (input: string) =>
      `${closest(input, ["slow", "faster", "fastest"])} ${privateFunc()}`
  );*/

/*
// use any export syntax to export a function be callable by the extism host
export function get_closest() {
  let input = Host.inputString();
  let result = closest(input, ["slow", "faster", "fastest"]);
  Host.outputString(result + " " + privateFunc());
}
*/