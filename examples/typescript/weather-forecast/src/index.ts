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

export const getParameterCount = () => withExtism(countParameters);
export const getWeatherForecast = () => withExtism(getForecast);

export const reverseByteArray = () => {
  const input = Host.inputBytes();
  Host.outputString(JSON.stringify(input));
  // input.reverse();
  // Host.outputBytes(input);
};

export const getSetVar = () => {
  Var.set("testString", "someValue");
  const resp = Var.get("testString");
  Host.outputString(JSON.stringify(resp));
};
