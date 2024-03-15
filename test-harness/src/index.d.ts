declare module "main" {
  export function ubiqEcho(): I32;
}

declare module "extism:host" {
  interface user {
    ubiqDispatch(ptr: I64): I64;
  }
}
