import { withExtism } from "./withExtism";
import "./types/extism";

const summaries = [
  "Freezing",
  "Bracing",
  "Chilly",
  "Cool",
  "Mild",
  "Warm",
  "Balmy",
  "Hot",
  "Sweltering",
  "Scorching",
];

// Get random number from range, inclusive of start and exclusive of end.
const getRandomNumber = (start: number, end: number) => {
  const range = end - start;
  const rand = Math.floor(Math.random() * range);
  return rand + start;
};

const getForecast = (forecastCount: number) => {
  if (forecastCount === undefined) {
    forecastCount = Math.random() * 5 + 1;
  }

  const results = [];
  for (let i = 0; i < forecastCount; i++) {
    const forecastC = getRandomNumber(-20, 55);
    const forecastF = 32 + Math.floor(forecastC / 0.5556);
    results.push({
      Date: new Date(),
      TemperatureC: forecastC,
      TemperatureF: forecastF,
      Summary: summaries[Math.floor(Math.random() * summaries.length)],
    });
  }
  return results;
};

const countParameters = (
  a: any,
  b: any,
  c: any,
  d: any,
  e: any,
  f: any,
  g: any,
  h: any,
  i: any,
  j: any,
  k: any
) => {
  let params = [a, b, c, d, e, f, g, h, i, j, k].filter((x) => x !== undefined);

  // QuickJS doesn't seem to support arguments.length so we'll test manually.
  return { argumentCount: params.length };
};

export const uppercaseAsBytes = () => {
  const input = Host.inputBytes();
  var enc = new TextDecoder("utf-8");

  const result = enc.decode(input);
  Host.outputString(result.toLocaleUpperCase());
  // input.reverse();
  // Host.outputBytes(input);
};

export const getSetVar = () => {
  Var.set("testString", "someValue");
  const resp = Var.get("testString");
  Host.outputString(JSON.stringify(resp));
};

const vowels = ["a", "e", "i", "o", "u", "y"];
export const count_vowels = () => {
  const input = Host.inputString();
  let vowelCount = 0;
  for (var i = 0; i < input.length; i++) {
    for (var j = 0; j < vowels.length; j++) {
      if (input.charAt(i) === vowels[j]) {
        vowelCount += 1;
        break;
      }
    }
  }
  Host.outputString(JSON.stringify({ count: vowelCount }));
};

/* 
//this doesn't work because top level async doesn't work in quickjs yet
function timeoutPromise(time) {
  return new Promise(function (resolve) {
    setTimeout(function () {
      resolve(true);
    }, time);
  });
}

export const sleepMs = () => {
  (async () => {
    await timeoutPromise(4000);
  })();
};

export const sleepMsAsync = async () => {
  //const input = Host.inputString();
  await timeoutPromise(4000);
  Host.outputString("Slept");
};

export const sleep_ms = async (milliseconds: number) => {
  if (!milliseconds) {
    milliseconds = 10000;
  }

  await new Promise((r) => setTimeout(r, milliseconds));
};

// export const sleepMs = () => withExtism(sleep_ms);

*/

export const sleep_ms = (milliseconds: number) => {
  if (!milliseconds) {
    milliseconds = 10000;
  }
  var startTime = Date.now();
  while (Date.now() - startTime < milliseconds) {
    continue;
  }
};

export const sleepMs = () => withExtism(sleep_ms);
export const getParameterCount = () => withExtism(countParameters);
export const getWeatherForecast = () => withExtism(getForecast);
