export type PersonnelRecord = {
  id: number;
  currentOperator: {
    name: string;
    position: string;
    assignmentDate: string;
  };
  previousOperator: {
    name: string;
    position: string;
    accessDate: string;
  };
};
export type personInformation = {
  information: {
    position: string;
    email: string;
    tel: string;
  };
};
export type Person = {
  name: string;
  position: string;
  email: string;
};
