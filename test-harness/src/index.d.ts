declare module "main" {
  // NOTE: THESE MUST BE IN ALPHABETICAL ORDER OR IT WILL BREAK BADLY AND YOU WILL BE VERY SAD
  // Extism exports take no params and return an I32
  export function _init(): I32;
  export function doNothing(): I32;
  export function getClosest(): I32;
  export function intArrayStatsJSON(): I32;
  export function max(): I32;
  export function randomstr(): I32;
  export function returnHelloWorld(): I32;
  export function strlen(): I32;
  export function ubiqEcho(): I32;
}

declare module "extism:host" {
  interface user {
    ubiqDispatch(ptr: I64): I64;
  }
}
