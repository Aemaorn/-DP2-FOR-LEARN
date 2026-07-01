type TaskStatus = 'completed' | 'inProgress';

export type TaskItem = {
  title: string;
  startDate?: Date;
  endDate?: Date;
  status?: TaskStatus;
  startAt?: number;
}
