import "./types/extism";

export function withExtism(func: (...params: any[]) => void) {
  (async () => {
    let input = [];
    try {
      input = JSON.parse(Host.inputString());
    } catch (e) {
      console.error(
        "\nERROR: Function input not JSON formatted, unable to parse!",
        e
      );
      return -1;
    }
    const resp = await func(...input);
    Host.outputString(JSON.stringify(resp));
  })();
}
