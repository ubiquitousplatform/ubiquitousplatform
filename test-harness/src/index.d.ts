declare module "main" {
  export function ubiqEcho(): I32;
}

declare module "extism:host" {
  interface user {
    debug(ptr: I64): void;
    ubiqDispatch(ptr: I64): I64;
  }
}
