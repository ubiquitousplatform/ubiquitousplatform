const { debug, ubiqDispatch } = Host.getFunctions();

console.log = (msg) => {
  let mem = Memory.fromString(msg);
  debug(mem.offset);
};

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

const ubiqEcho = registerFn((input: string) => {
  try {
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
  } catch (e) {
    console.error("Error in ubiqEcho: ", e);
    return "Error in ubiqEcho: " + e;
  }
  //Host.outputString(`Hello, World!`);
});

// TODO: throw an exception, access memory wrong, and see if it gets caught and handled properly.

export { ubiqEcho };
