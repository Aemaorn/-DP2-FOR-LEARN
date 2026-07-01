export type MappingDocumentResponse = {
  single: MappingDocumentSingle[];
  multiple: MappingDocumentMultiple[];
};

export type MappingDocumentSingle = {
  key: string;
  description: string;
}

export type MappingDocumentMultiple = {
  key: string;
  description: string;
}