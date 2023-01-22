export function withExtism(func) {
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
}
