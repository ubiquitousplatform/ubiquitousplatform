declare namespace Host {
  const inputBytes: () => number[];
  const inputString: () => string;
  const outputBytes: (bytes: number[]) => boolean;
  const outputString: (str: string) => boolean;
}

declare namespace Var {
  const set: (varName: string, data: string | number[]) => void;
  const get: (varName: string) => number[];
}
