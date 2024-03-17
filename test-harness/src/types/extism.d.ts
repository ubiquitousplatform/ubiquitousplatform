declare namespace Host {
  const inputBytes: () => ArrayBuffer;
  const inputString: () => string;
  const outputBytes: (bytes: ArrayBuffer) => boolean;
  const outputString: (str: string) => boolean;
  const getFunctions: () => any;
}

declare namespace Var {
  const set: (varName: string, data: string | ArrayBuffer) => void;
  const get: (varName: string) => ArrayBuffer;
}

type memoryDescriptor = {
  offset: number;
  len: number;
  readString: () => string;
  readJsonObject: () => any;
};

declare namespace Memory {
  const fromString: (str: string) => memoryDescriptor; // allocates bytes in memory, returns offset into memory.
  const fromBuffer: (bytes: ArrayBuffer) => memoryDescriptor;
  const find: (offset: number) => memoryDescriptor;
  const fromJsonObject: (obj: object) => memoryDescriptor;
  // fromBuffer
  // readBytes
}
