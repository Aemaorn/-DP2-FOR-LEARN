import { type ModelRef, type Ref } from 'vue';

type SequencedItem = { sequence: number };

type ArrayType<T> = T[] | undefined;
type ArrayRefType<T> = Ref<T[] | undefined> | ModelRef<T[] | undefined>;

export const ArrayHelper = () => {
  const getArrayValue = <T>(
    value: ArrayType<T> | ArrayRefType<T>
  ): T[] => {
    if (value && typeof value === 'object' && 'value' in value) {
      return (value as ArrayRefType<T>).value || [];
    }

    return value || [];
  };

  /**
 * ใช้ในกรณีที่ต้องการเพิ่ม Item ตอน add ข้อมูลลง Array
 * ตัวอย่าง การใช้งาน addSequenceWithItem<Model>(value, {description: ''});
 */
  const addSequence = <T extends SequencedItem>(
    value: ArrayType<T> | ArrayRefType<T>,
    item: T
  ): T[] => {
    const array = getArrayValue(value);

    const newItem = {
      ...item,
      sequence: array.length + 1,
    } as T;

    return [...array, newItem];
  };

  const reSequence = <T extends SequencedItem>(value: T[]): T[] => {
    return value.map((item, index): T => ({
      ...item,
      sequence: index + 1,
    })
    );
  };

  const deleteItemAndReSequence = <T extends SequencedItem>(
    value: ArrayType<T> | ArrayRefType<T>,
    index: number
  ): T[] => {
    const array = getArrayValue(value);
    const filteredArray = array.filter((_, i) => i !== index);
    return reSequence(filteredArray);
  };

  return {
    addSequence,
    reSequence,
    deleteItemAndReSequence,
  };
};
