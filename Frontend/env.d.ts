/// <reference types="vite/client" />

declare module 'dom-to-image-more' {
  interface Options {
    scale?: number;
    bgcolor?: string;
    width?: number;
    height?: number;
    style?: Partial<CSSStyleDeclaration>;
    filter?: (node: Node) => boolean;
    quality?: number;
  }

  function toPng(node: HTMLElement, options?: Options): Promise<string>;
  function toJpeg(node: HTMLElement, options?: Options): Promise<string>;
  function toBlob(node: HTMLElement, options?: Options): Promise<Blob>;
  function toSvg(node: HTMLElement, options?: Options): Promise<string>;
  function toCanvas(node: HTMLElement, options?: Options): Promise<HTMLCanvasElement>;

  export default { toPng, toJpeg, toBlob, toSvg, toCanvas };
}
