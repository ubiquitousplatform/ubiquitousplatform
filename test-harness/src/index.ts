import { distance, closest } from "fastest-levenshtein";

// this function is private to the module
function privateFunc() {
  return "world";
}

function asExtismFunction(fn: Function) {
  return function () {
    let input = Host.inputString();
    let result = fn(input);
    Host.outputString(result + " " + privateFunc());
  };
}

export var get_closest: () => void = asExtismFunction(
  (input: string) =>
    `${closest(input, ["slow", "faster", "fastest"])} ${privateFunc()}`
);

/*
// use any export syntax to export a function be callable by the extism host
export function get_closest() {
  let input = Host.inputString();
  let result = closest(input, ["slow", "faster", "fastest"]);
  Host.outputString(result + " " + privateFunc());
}
*/
