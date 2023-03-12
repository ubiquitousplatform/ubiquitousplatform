declare namespace Host {
  const inputBytes: () => ArrayBuffer;
  const inputString: () => string;
  const outputBytes: (bytes: ArrayBuffer) => boolean;
  const outputString: (str: string) => boolean;
}

declare namespace Var {
  const set: (varName: string, data: string | ArrayBuffer) => void;
  const get: (varName: string) => ArrayBuffer;
}
