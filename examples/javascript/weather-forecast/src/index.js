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
const getRandomNumber = (start, end) => {
  const range = end - start;
  const rand = Math.floor(Math.random() * range);
  return rand + start;
};

const getForecast = (forecastCount) => {
  if (forecastCount === undefined) {
    forecastCount = Math.random() * 5 + 1;
  }

  const results = [];
  for (i = 0; i < forecastCount; i++) {
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

const withExtism = (func) => {
  try {
    input = JSON.parse(Host.inputString());
  } catch (e) {
    console.error(
      "\nERROR: Function input not JSON formatted, unable to parse!",
      e
    );
    return -1;
  }
  const resp = func(...input);
  Host.outputString(JSON.stringify(resp));
  return 0;
};

const countParameters = (a, b, c, d, e, f, g, h, i, j, k) => {
  let params = [a, b, c, d, e, f, g, h, i, j, k].filter((x) => x !== undefined);

  // QuickJS doesn't seem to support arguments.length so we'll test manually.
  return { argumentCount: params.length };
};

export const testFunction = () => withExtism(countParameters);
export const getWeatherForecast = () => withExtism(getForecast);
