export interface ExamineField {
  name: string;
  type: string;
  values: Array<string>;
}

export interface ExamineDocument {
  fields: Array<ExamineField>;
}
