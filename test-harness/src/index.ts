const { debug, ubiqDispatch } = Host.getFunctions();

// TODO: override error, warn,
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

// TODO: typecheck the object? or perhaps we declare the type in the SDK and this is an interior function
// that just does the conversion to the right format, and doesn't do any typechecking.
const buildHostJson = (
  namespace: number,
  functionId: number,
  functionVersion: number,
  msg: object
) => {
  let interopVersion = 0;
  let encoding = 0; // encoding 0 is JSON, 1 is messagepack
  let payload = JSON.stringify(msg);
  let bytes = new Uint8Array(5 + payload.length);
  bytes[0] = interopVersion;
  bytes[1] = encoding;
  bytes[2] = namespace;
  bytes[3] = functionId;
  bytes[4] = functionVersion;

  /*Array.from(payload).forEach((char, i) => {
    bytes[i + 5] = char.charCodeAt(0);
  }*/

  // TODO: optimize this call. try Array.prototype.concat to combine 2 arrays.
  // Does this have UTF-8 issues?  Should we use TextEncoder?

  /*for (let i = 0; i < payload.length; i++) {
    bytes[i + 5] = payload.charCodeAt(i);
  }*/

  console.log("building textEncoder...");
  const encoder = new TextEncoder();
  console.log("encoding payload...");
  const payloadBytes = encoder.encode(payload);
  console.log("Iterating payload bytes...");
  for (let i = 0; i < payloadBytes.length; i++) {
    bytes[i + 5] = payloadBytes[i];
  }
  console.log("returning byte array...");
  return bytes.buffer;
  //let msgBytes = new TextEncoder().encode(msg);
};

var functionMapping = {
  ubiquitousHostReserved: {
    namespace: 255,
    functions: {
      stdoutWrite: {
        id: 0,
        version: 0,
      },
      stderrWrite: {
        id: 1,
        version: 0,
      },
    },
  },
};

const stdoutWrite = (msg: string) => {
  console.log("looking up write function...");
  let functionNamespace = functionMapping["ubiquitousHostReserved"].namespace;
  let functionId =
    functionMapping["ubiquitousHostReserved"].functions["stdoutWrite"].id;
  let functionVersion =
    functionMapping["ubiquitousHostReserved"].functions["stdoutWrite"].version;
  let bytes = buildHostJson(functionNamespace, functionId, functionVersion, {
    message: msg,
  });
  console.log("getting memory from bytes...");
  let memory = Memory.fromBuffer(bytes);
  console.log("calling dispatch...");
  ubiqDispatch(memory.offset);
};

const ubiqEcho = registerFn((input: string) => {
  try {
    console.log("ubiqEcho called with input: " + input);
    console.log("building message...");

    stdoutWrite("[from ubiqEcho] input");
    /*
    console.log("Call complete.  Parsing response..");
    console.log("Response offset: " + offset);
    let response = Memory.find(offset).readString();
    console.log("Response read: " + response);
    if (response != "myHostFunction1: " + msg) {
      console.log("Response did not equal expected. throwing error.");
      throw Error(`wrong message came back from myHostFunction1: ${response}`);
    }
    */
    /*

    console.log("Trying another call using json...");
    let msg2 = { hello: "world!" };
    mem = Memory.fromJsonObject(msg2);
    console.log("Object constructed at offset: " + mem.offset);
    console.log("Calling ubiqDispatch...");
    offset = ubiqDispatch(mem.offset);
    console.log("Call complete.  Parsing response..");
    let response2 = Memory.find(offset).readJsonObject();
    console.log("Response read: " + JSON.stringify(response2));
    if (response2.hello != "myHostFunction2") {
      console.log("Response did not equal expected. throwing error.");
      throw Error(`wrong message came back from myHostFunction2: ${response}`);
    }
    */
    return "called host function!";
  } catch (e: any) {
    console.log(
      "Error in ubiqEcho. Error: " +
        JSON.stringify(e) +
        "stack: " +
        e.stack +
        ", message: " +
        e.message
    );
    return "Error in ubiqEcho: " + e;
  }
  //Host.outputString(`Hello, World!`);
});

// TODO: throw an exception, access memory wrong, and see if it gets caught and handled properly.

export { ubiqEcho };
