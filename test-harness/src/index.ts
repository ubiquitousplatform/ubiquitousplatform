import { distance, closest } from "fastest-levenshtein";

const _init = registerFn(() => "Doing one-time initialization.");
const { ubiqDispatch } = Host.getFunctions();

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

const strlen = registerFn((input: string) => input.length.toString());

const getClosest = registerFn(
  (input: string) =>
    `${closest(input, ["slow", "faster", "fastest"])} ${privateFunc()}`
);

// why doesn't this work? is math.random not working?
// why don't we see the output from teh console.log? is it because of extism call or because of the wrapping?
const randomstr = registerFn((input: string) => {
  // console.log("randomstr called");
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
  // console.log(`Length: ${length}`);
  return result;
});

const returnHelloWorld = registerFn(() => "Hello, world!");

const doNothing = registerFn(() => "");

const max = registerFn((input: string) => {
  let numbers = input.split(",").map((i) => parseInt(i));
  return Math.max(...numbers).toString();
});

const intArrayStatsJSON = registerFn((input: string) => {
  let numbers: number[] = JSON.parse(input);
  let max = Math.max(...numbers);
  let min = Math.min(...numbers);
  let sum = numbers.reduce((a: number, b: number) => a + b, 0);
  let mean = Math.floor(sum / numbers.length);
  let median = numbers.sort((a: number, b: number) => a - b)[
    Math.floor(numbers.length / 2)
  ];
  let result = {
    max,
    min,
    sum,
    mean,
    median,
  };
  return JSON.stringify(result);
});

const ubiqEcho = registerFn((input: string) => {
  console.log("ubiqEcho called with input: ", input);
  let msg = "Hello from js!";
  // TODO: build a byte array that has the full header and try to pass it over.
  let mem = Memory.fromString(msg);

  let offset = ubiqDispatch(mem.offset);
  let response = Memory.find(offset).readString();
  if (response != "myHostFunction1: " + msg) {
    throw Error(`wrong message came back from myHostFunction1: ${response}`);
  }

  let msg2 = { hello: "world!" };
  mem = Memory.fromJsonObject(msg2);
  offset = ubiqDispatch(mem.offset);
  let response2 = Memory.find(offset).readJsonObject();
  if (response2.hello != "myHostFunction2") {
    throw Error(`wrong message came back from myHostFunction2: ${response}`);
  }
  return "called host function!";
  //Host.outputString(`Hello, World!`);
});

export {
  _init,
  doNothing,
  getClosest,
  intArrayStatsJSON,
  max,
  randomstr,
  returnHelloWorld,
  strlen,
  ubiqEcho,
  //ubiqKVStoreRW,
};
