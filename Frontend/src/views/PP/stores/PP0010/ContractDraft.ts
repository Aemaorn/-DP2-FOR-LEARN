import { defineStore } from "pinia";
import { type TAdvancePayment, type TAttachmentBase, type TBuyerInfo, type TCarLeaseInfo, type TComputerLeaseInfo, type TContractDraftBody, type TCopierLeaseInfo, type TDeliveryInfo, type TGuaranteeInfo, type TPaymentTermDetail, type TPenaltyInfo, type TRedelivery, type TRetentionPayment, type TTerminationInfo, type TVendor, type TWarrantyInfo } from "../../models/PP0010/ContractDraft";
import { computed, ref, type Ref } from "vue";
import type { CardSelectItems, Option } from "@/models/shared/option";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import { EGroupCode, EntrepreneurType } from "@/enums/shared";
import { ContractDraftTemplate } from "@/enums/contractDraftt";
import Template15 from "../../components/PP010/components/Templates/Template15.vue";
import Template1 from "../../components/PP010/components/Templates/Template1.vue";
import Template14 from "../../components/PP010/components/Templates/Template14.vue";
import Template13 from "../../components/PP010/components/Templates/Template13.vue";
import Template12 from "../../components/PP010/components/Templates/Template12.vue";
import Template11 from "../../components/PP010/components/Templates/Template11.vue";
import Template10 from "../../components/PP010/components/Templates/Template10.vue";
import Template9 from "../../components/PP010/components/Templates/Template9.vue";
import Template8 from "../../components/PP010/components/Templates/Template8.vue";
import Template7 from "../../components/PP010/components/Templates/Template7.vue";
import Template6 from "../../components/PP010/components/Templates/Template6.vue";
import Template5 from "../../components/PP010/components/Templates/Template5.vue";
import Template4 from "../../components/PP010/components/Templates/Template4.vue";
import Template3 from "../../components/PP010/components/Templates/Template3.vue";
import Template2 from "../../components/PP010/components/Templates/Template2.vue";
import TemplateRental from "../../components/PP010/components/Templates/TemplateRental.vue";
import contractDraftService from "../../services/PP010/ContractDraftService";
import { usePPDetailStore } from "@/stores/PP/ppStore";
import ToastHelper from "@/helpers/toast";
import { TAgreementBaseType, TContractDraftStatus, TPaymentBaseType, TRedeliveryBaseType } from "../../enums/pp010";
import { useAuthenticationStore } from "@/stores/authentication";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";
import { showReasonDialogAsync } from "@/helpers/dialog";
import { ReasonDialogType } from "@/enums/dialog";
import type { EntrepreneurAttachments } from "@/models/shared/uploadFile";
import type { defaultAcceptorCriteria } from "@/models/shared/operations";
import { SectionProcessType } from "@/enums/operations";
import operationService from "@/services/Shared/operations";
import type { ParticipantsAcceptor } from "@/models/shared/participants";

export const useContractDraftStore = defineStore("contract-draft-store", () => {
  const procurementStore = usePPDetailStore();
  const authStore = useAuthenticationStore();
  const isHasPermission = ref(false);

  const body = ref<TContractDraftBody>({
    detail: {
      buyer: {
        province: {},
        district: {},
        subDistrict: {}
      },
      vendor: {},
      attachments: [] as TAttachmentBase[],
    },
  } as TContractDraftBody);

  const cloneBody = ref<TContractDraftBody>({} as TContractDraftBody);

  const onClearBody = () => {
    const baseId = body.value.id;

    body.value = {
      id: baseId,
      detail: {
        buyer: {
          province: {},
          district: {},
          subDistrict: {}
        },
        vendor: {},
        attachments: [] as TAttachmentBase[],
      },
    } as TContractDraftBody;
  };

  const vendorId = ref<string>();

  const vendorList = ref<CardSelectItems[]>([] as CardSelectItems[]);

  const isEdit = ref<boolean>(false);

  const canEdit = computed(() => {
    const status = [TContractDraftStatus.Draft, TContractDraftStatus.Rejected, TContractDraftStatus.Edit].includes(body.value.status) || !body.value.status;

    return status && isHasPermission.value;
  });

  const canEditDoc = computed(() => {
    const status = [TContractDraftStatus.Approved].includes(body.value.status) || !body.value.status;

    return status && isHasPermission.value;
  });

  const isPending = computed(() => {

    return body.value.status === TContractDraftStatus.Pending;
  });

  const canAcceptAndReject = computed(() => {
    const status = body.value.status === TContractDraftStatus.Pending;
    const checkUser = body.value.acceptors?.some(a => (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id);

    return status && checkUser;
  });

  const isLastApproval = computed(() => {
    const approvalUser = body.value.acceptors?.filter(f => f.status === AcceptorStatus.Pending);

    if (!approvalUser) {
      return false;
    }

    const current = approvalUser.find(f => f.isCurrent);

    if (!current) {
      return false;
    }

    return canAcceptAndReject.value && current.sequence === approvalUser[approvalUser.length - 1].sequence;
  });

  const switchTemplate = () => {
    const currentBuyer = body.value.detail?.buyer ?? ({} as TBuyerInfo);
    const currentVendor = body.value.detail?.vendor ?? ({} as TVendor);
    const currentAttachments = [] as TAttachmentBase[];
    const currentPayments = body.value.detail?.payment ?? {
      type: TPaymentBaseType.Term, details: [{
        sequence: 1,
      }] as TPaymentTermDetail[]
    };
    const guarantee = body.value.detail?.guarantee ?? ({} as TGuaranteeInfo);

    const commonDetail = {
      attachments: currentAttachments,
      guarantee: guarantee,
      penalty: { isPenalty: body.value?.detail?.penalty?.isPenalty ?? true } as TPenaltyInfo,
      buyer: currentBuyer,
      vendor: currentVendor,
      payment: currentPayments,
    };

    switch (body.value.template) {
      case ContractDraftTemplate.CFormat001:
      case ContractDraftTemplate.CFormat016:
        {
          const isA = body.value.template == ContractDraftTemplate.CFormat001;

          const detailPayment = currentPayments.details;

          if (!isA) {
            detailPayment?.splice(1);
            detailPayment?.forEach((item) => {
              item.installmentPercentage = 100;
              item.amount = body.value.budget;
            });
          }

          body.value = {
            ...body.value,
            detail: {
              ...commonDetail,
              payment: {
                ...currentPayments,
                details: detailPayment,
                paymentTypeCode: isA ? 'PayType001' : 'PayType002',
              },
              agreement: {
                type: TAgreementBaseType.WorkplaceSerialNumber,
                workplaceDistrict: {},
                workplaceProvince: {},
                workplaceSubDistrict: {},
                vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
                itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              },
              retentionPayment: { hasRetentionPayment: false } as unknown as TRetentionPayment,
              advancePayment: { hasAdvancePayment: false } as TAdvancePayment,
              warranty: {
                ...body.value?.detail?.warranty,
                hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
                fixingDeadlinePeriod: body.value?.detail?.warranty?.fixingDeadlinePeriod || {},
                warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
              },
              termination: { duration: {}, vendorProcessingTime: {} },
              delivery: {
                leadTime: body.value?.detail?.delivery?.leadTime,
                leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
                periodTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              },
              penalty: {
                rate: body.value?.detail?.penalty?.rate,
                rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
                typeCode: body.value?.detail?.penalty?.typeCode,
                amount: body.value?.detail?.penalty?.amount,
              },
            }
          } as TContractDraftBody;
          break;
        }

      case ContractDraftTemplate.CFormat002:
      case ContractDraftTemplate.CFormat004:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.General,
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            delivery: {
              leadTime: body.value.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            } as TDeliveryInfo,
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              fixingDeadlinePeriod: body.value?.detail?.warranty?.fixingDeadlinePeriod || {},
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat003:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.RentalDuration,
              duration: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              fixingDeadlinePeriod: body.value?.detail?.warranty?.fixingDeadlinePeriod || {},
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat005:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.General,
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            redelivery: {
              type: TRedeliveryBaseType.Redelivery,
            } as TRedelivery,
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            } as TDeliveryInfo,
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              fixingDeadlinePeriod: body.value?.detail?.warranty?.fixingDeadlinePeriod || {},
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat006:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.LeaseComputer,
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            computerLease: { duration: {} } as TComputerLeaseInfo,
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            } as TDeliveryInfo,
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat007:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.WorkplaceSerialNumber,
              workplaceDistrict: {},
              workplaceProvince: {},
              workplaceSubDistrict: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            termination: { vendorProcessingTime: {} },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat008:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.LeaseCar,
              duration: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            carLease: {} as TCarLeaseInfo,
            redelivery: { type: TRedeliveryBaseType.Redelivery } as TRedelivery,
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat009:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.Workplace,
              workplaceDistrict: {},
              workplaceProvince: {},
              workplaceSubDistrict: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            advancePayment: {} as TAdvancePayment,
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat010:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.RentalDurationWorkplace,
              workplaceDistrict: {},
              workplaceProvince: {},
              workplaceSubDistrict: {},
              duration: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? 'ธนาคารอาคารสงเคราะห์ สำนักงานใหญ่',
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat011:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.Lease,
              duration: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            copierLease: {} as TCopierLeaseInfo,
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            } as TDeliveryInfo,
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat012:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.ExchangeGiver,
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            redelivery: {
              type: TRedeliveryBaseType.Acceptance,
              rentalDuration: {}
            } as TRedelivery,
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              fixingDeadlinePeriod: body.value.detail.warranty?.fixingDeadlinePeriod || {},
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat013:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.Workplace,
              workplaceDistrict: {},
              workplaceProvince: {},
              workplaceSubDistrict: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            advancePayment: {} as TAdvancePayment,
            termination: {} as TTerminationInfo,
            warranty: {
              ...body.value?.detail?.warranty,
              hasWarranty: body.value?.detail?.warranty?.hasWarranty ?? false,
              fixingDeadlinePeriod: body.value?.detail?.warranty?.fixingDeadlinePeriod || {},
              warrantyPeriod: body.value?.detail?.warranty?.warrantyPeriod || {},
            },
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat014:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.WorkplaceSerialNumber,
              workplaceDistrict: {},
              workplaceProvince: {},
              workplaceSubDistrict: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            retentionPayment: { hasRetentionPayment: false } as unknown as TRetentionPayment,
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
            delivery: {
              leadTime: body.value?.detail?.delivery?.leadTime,
              leadTimeTypeCode: body.value?.detail?.delivery?.leadTimeTypeCode,
              periodTypeCode: body.value?.detail?.delivery?.periodTypeCode,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CFormat015:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.RentalDuration,
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
            advancePayment: {} as TAdvancePayment,
            penalty: {
              isPenalty: body.value?.detail?.penalty?.isPenalty ?? true,
              rate: body.value?.detail?.penalty?.rate,
              rateTypeCode: body.value?.detail?.penalty?.rateTypeCode,
              typeCode: body.value?.detail?.penalty?.typeCode,
              amount: body.value?.detail?.penalty?.amount,
            },
          }
        } as TContractDraftBody;
        break;

      case ContractDraftTemplate.CMRentalTpl001:
      case ContractDraftTemplate.CMRentalTpl002:
      case ContractDraftTemplate.CMRentalTpl003:
      case ContractDraftTemplate.CMRentalTpl004:
        body.value = {
          ...body.value,
          detail: {
            ...commonDetail,
            agreement: {
              type: TAgreementBaseType.RentalDurationWorkplace,
              workplaceDistrict: {},
              workplaceProvince: {},
              workplaceSubDistrict: {},
              duration: {},
              itemDetail: body.value?.detail?.agreement?.itemDetail ?? body.value?.contractName,
              vatRateTypeCode: body.value?.detail?.agreement?.vatRateTypeCode || body.value?.vatRateTypeCode,
            },
          }
        } as TContractDraftBody;
        break;

      default:
        break;
    }
  };

  const getVendorListAsync = async (initialVendorId?: string) => {
    if (!procurementStore.procurementDetail.id) return;

    const { data, status } = await contractDraftService.getContractDraftVendorList(procurementStore.procurementDetail.id);

    if (status == HttpStatusCode.Ok) {
      body.value.id = data.id;
      isHasPermission.value = data.hasEditPermission;

      vendorList.value = data.vendors.map((item: any) => ({
        title: item.contractNumber,
        description: item.name,
        value: item.id,
        status: item.status,
        isCompleted: item.isCompleted,
      } as CardSelectItems));

      vendorList.value.sort((a, b) => a.title.localeCompare(b.title, undefined, { numeric: true }));

      const matchedVendor = initialVendorId && vendorList.value.find(v => v.value === initialVendorId);
      const selectedVendor = matchedVendor ? matchedVendor.value : vendorList.value[0]?.value;

      if (selectedVendor) {
        vendorId.value = selectedVendor.toString();

        await getContractDraftByVendorIdAsync();
      }

      return;
    }

    isHasPermission.value = false;
  };

  const INVALID_FIELDS = [
    "contractType",
    "contractSignedDate",
    "template",
    "periodConditionType"
  ];

  const isInvalidValue = (key: string, value: any) => {
    return INVALID_FIELDS.includes(key) && (value === "" || value === "0001-01-01T00:00:00+00:00");
  };

  const filterSelectedInvalidFields = (obj: Record<string, any>) => {
    const result: Record<string, any> = {};
    for (const key in obj) {
      const value = obj[key];

      if (value !== null && typeof value === "object" && !Array.isArray(value)) {
        result[key] = filterSelectedInvalidFields(value);
      } else if (!isInvalidValue(key, value)) {
        result[key] = value;
      }
    }
    return result;
  };

  const getContractDraftByVendorIdAsync = async () => {
    const procurementId = procurementStore.procurementDetail.id;
    const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const { data, status } = await contractDraftService.getContractDraftByVendorId(procurementId, contractDraftId, vendor);

    if (status === HttpStatusCode.Ok) {
      const cleanedData = filterSelectedInvalidFields(data) as Record<string, any>;
      const originalId = body.value.id;

      body.value = {
        ...body.value,
        ...cleanedData,
        detail: {
          ...body.value.detail,
          ...cleanedData.detail,
        },
        id: originalId,
      };

      // Ensure termination is initialized for templates that require it
      const templatesWithTermination = [
        ContractDraftTemplate.CFormat001,
        ContractDraftTemplate.CFormat016,
        ContractDraftTemplate.CFormat007,
        ContractDraftTemplate.CFormat013,
      ];

      if (templatesWithTermination.includes(body.value.template) && !body.value.detail.termination) {
        body.value.detail.termination = { vendorProcessingTime: {} } as TTerminationInfo;
      }

      // Ensure warrantyPeriod and fixingDeadlinePeriod are initialized
      if (body.value.detail.warranty) {
        if (!body.value.detail.warranty.warrantyPeriod) {
          body.value.detail.warranty.warrantyPeriod = {} as TWarrantyInfo['warrantyPeriod'];
        }
        if (!body.value.detail.warranty.fixingDeadlinePeriod) {
          body.value.detail.warranty.fixingDeadlinePeriod = {} as TWarrantyInfo['fixingDeadlinePeriod'];
        }
      }

      // Clear fields not used by CFormat007 to prevent FK violations (e.g. DeliveryLeadTimeTypeCode)
      if (body.value.template === ContractDraftTemplate.CFormat007) {
        body.value.detail.delivery = undefined as any;
      }

      // Fix agreement type for CMRentalTpl templates (must be RentalDurationWorkplace to include workplaceAddress)
      const rentalTemplates = [
        ContractDraftTemplate.CMRentalTpl001,
        ContractDraftTemplate.CMRentalTpl002,
        ContractDraftTemplate.CMRentalTpl003,
        ContractDraftTemplate.CMRentalTpl004,
      ];
      if (rentalTemplates.includes(body.value.template) && body.value.detail.agreement) {
        if (body.value.detail.agreement.type !== TAgreementBaseType.RentalDurationWorkplace) {
          body.value.detail.agreement.type = TAgreementBaseType.RentalDurationWorkplace;
        }
        if (!body.value.detail.agreement.workplaceProvince) {
          body.value.detail.agreement.workplaceProvince = {} as any;
        }
        if (!body.value.detail.agreement.workplaceDistrict) {
          body.value.detail.agreement.workplaceDistrict = {} as any;
        }
        if (!body.value.detail.agreement.workplaceSubDistrict) {
          body.value.detail.agreement.workplaceSubDistrict = {} as any;
        }
        if (!body.value.detail.agreement.duration) {
          body.value.detail.agreement.duration = {} as any;
        }
      }

      const hasAcceptorSign = body.value.acceptors?.some(
        a => a.acceptorType === AcceptorType.AcceptorSign
      );

      if (!hasAcceptorSign) {
        await onGetDefaultAcceptorAsync();
      }

      cloneBody.value = JSON.parse(JSON.stringify(body.value));

      vendorList.value.forEach((v) => {
        if (v.value === vendor) {
          v.status = body.value.status;
          v.isCompleted = body.value.status === TContractDraftStatus.Approved && !!body.value.contractSignedDate;
        }
      });
    }
  };

  const onUpdateContractDraft = async (isSaveDraft?: boolean, newStatus?: TContractDraftStatus) => {
    const procurementId = procurementStore.procurementDetail.id;
    const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
    const bodyId = vendorId.value;

    if (!procurementId || !contractDraftId || !bodyId) return;

    const { status } = await contractDraftService.updateContractDraft(procurementId, contractDraftId, bodyId, body.value, newStatus, isSaveDraft);

    if (status === HttpStatusCode.Ok) {
      await getContractDraftByVendorIdAsync();

      newStatus == TContractDraftStatus.Pending && ToastHelper.sendApproveConfirmMessageToast();
      newStatus == TContractDraftStatus.Edit && ToastHelper.recallEditMessageToast();
      !newStatus && ToastHelper.updatedMessageToast();
    }
  };

  const getOptionsAsync = async (
    target: Ref<Option[]>,
    groupCode: EGroupCode,
    parentCode?: string
  ): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode);
    if (status === HttpStatusCode.Ok) {
      target.value = data;
    }
  };

  const getOptionsParentIdAsync = async (
    target: Ref<Option[]>,
    groupCode: EGroupCode,
    parentId?: string,
    parentCode?: string
  ): Promise<void> => {
    const { data, status } = await SharedService.onGetParamByGroupCodeWithParentIdAsync(groupCode, parentId, undefined, parentCode);
    if (status === HttpStatusCode.Ok) {
      target.value = data;

      if (target.value === attacementTypeOptions.value) {
        target.value.push({
          value: 'CAppendOther001',
          label: 'อื่นๆ ระบุ',
        });
      }
    }
  };

  const approveAsync = async () => {
    const procurementId = procurementStore.procurementDetail.id;
    const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Confirm);

    if (res.isConfirm) {
      const { status } = await contractDraftService.approveContractDraft(procurementId, contractDraftId, vendor, res.reason);

      if (status == HttpStatusCode.Ok) {
        ToastHelper.approvedMessageToast();

        await procurementStore.onGetProcurementById(procurementId);
        await getContractDraftByVendorIdAsync();
      }
    }
  };

  const rejectAsync = async (isCommittee = false) => {
    const procurementId = procurementStore.procurementDetail.id;
    const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
    const vendor = vendorId.value;

    if (!procurementId || !contractDraftId || !vendor) return;

    const res = await showReasonDialogAsync(isCommittee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject, true);

    if (res.isConfirm) {
      const { status } = await contractDraftService.rejectContractDraft(procurementId, contractDraftId, vendor, res.reason);

      if (status == HttpStatusCode.Ok) {
        isCommittee ? ToastHelper.notAgreeMessageToast() : ToastHelper.sendEditMessageToast();

        await getContractDraftByVendorIdAsync();
      }
    }
  };

  const conditionTypeOptions = ref<Option[]>([]);
  const periodConditionTypeOptions = ref<Option[]>([]);
  const contractTypeOptions = ref<Option[]>([]);
  const templateTypeOptions = ref<Option[]>([]);
  const subTemplateTypeOptions = ref<Option[]>([]);
  const attacementTypeOptions = ref<Option[]>([]);
  const vatTypeOptions = ref<Option[]>([]);
  const unitTypeOptions = ref<Option[]>([]);
  const fineTypeOptions = ref<Option[]>([]);
  const unitMeaTypeOptions = ref<Option[]>([]);
  const periodTypeOptions = ref<Option[]>([]);
  const periodConTypeOptions = ref<Option[]>([]);
  const payTypeOptions = ref<Option[]>([]);
  const rCCRTypeOptions = ref<Option[]>([]);
  const warrantyOptions = ref<Option[]>([]);
  const bankOptions = ref<Option[]>([]);
  const periodOptions = ref<Option[]>([]);
  const pTimeTypeOptions = ref<Option[]>([]);

  const getConditionTypeOptions = async () => getOptionsAsync(conditionTypeOptions, EGroupCode.CSDPCond);
  const getPeriodConditionTypeAsync = async () => getOptionsAsync(periodConditionTypeOptions, EGroupCode.PeriodType);
  const getContractTypeAsync = async () => getOptionsAsync(contractTypeOptions, EGroupCode.CMType);
  const getTemplateTypeAsync = async (parentCode: string) => getOptionsAsync(templateTypeOptions, EGroupCode.CMType, parentCode);
  const getSubTemplateTypeAsync = async (parentId: string) => getOptionsParentIdAsync(subTemplateTypeOptions, EGroupCode.CFormatLv2, parentId);
  const getAttacementTypeAsync = async (parentId?: string, parentCode?: string) => getOptionsParentIdAsync(attacementTypeOptions, EGroupCode.CAppendix, parentId, parentCode);
  const getAttacementGroupCodeTypeAsync = async (parentCode: string) => getOptionsAsync(attacementTypeOptions, EGroupCode.CAppendix, parentCode);
  const getVatTypeAsync = async () => getOptionsAsync(vatTypeOptions, EGroupCode.VATType);
  const getUnitTypeAsync = async () => getOptionsAsync(unitTypeOptions, EGroupCode.UnitOfMea);
  const getFineTypeAsync = async () => getOptionsAsync(fineTypeOptions, EGroupCode.FineType);
  const getUnitMeaTypeAsync = async () => getOptionsAsync(unitMeaTypeOptions, EGroupCode.UnitOfMea);
  const getPeriodTypeAsync = async () => getOptionsAsync(periodTypeOptions, EGroupCode.PeriodType);
  const getPeriodConTypeAsync = async () => getOptionsAsync(periodConTypeOptions, EGroupCode.PeriodCond);
  const getPayTypeAsync = async () => getOptionsAsync(payTypeOptions, EGroupCode.PayType);
  const getRCCRTypeAsync = async () => getOptionsAsync(rCCRTypeOptions, EGroupCode.PBondType);
  const getWarrantyTypeAsync = async () => getOptionsAsync(warrantyOptions, EGroupCode.WtyCond);
  const getBankAsync = async () => getOptionsAsync(bankOptions, EGroupCode.Bank);
  const getPeriodAsync = async () => getOptionsAsync(periodOptions, EGroupCode.PeriodType);
  const getPTimeTypeAsync = async () => getOptionsAsync(pTimeTypeOptions, EGroupCode.PTimeType);

  const onSelectLocationInfo = (value: string, options: Option[], setLabel: (label: string) => void) => {
    const selectData = options.find(l => l.value == value);

    if (selectData) {
      setLabel(selectData.label);
    }
  };

  const TEMPLATE_COMPONENTS: Record<
    ContractDraftTemplate, any> = {
    [ContractDraftTemplate.CFormat002]: Template1,
    [ContractDraftTemplate.CFormat003]: Template2,
    [ContractDraftTemplate.CFormat004]: Template3,
    [ContractDraftTemplate.CFormat005]: Template4,
    [ContractDraftTemplate.CFormat012]: Template5,
    [ContractDraftTemplate.CFormat001]: Template6,
    [ContractDraftTemplate.CFormat013]: Template7,
    [ContractDraftTemplate.CFormat009]: Template8,
    [ContractDraftTemplate.CFormat014]: Template9,
    [ContractDraftTemplate.CFormat007]: Template10,
    [ContractDraftTemplate.CFormat010]: Template11,
    [ContractDraftTemplate.CFormat015]: Template12,
    [ContractDraftTemplate.CFormat011]: Template13,
    [ContractDraftTemplate.CFormat006]: Template14,
    [ContractDraftTemplate.CFormat008]: Template15,
    [ContractDraftTemplate.CMRentalTpl001]: TemplateRental,
    [ContractDraftTemplate.CMRentalTpl002]: TemplateRental,
    [ContractDraftTemplate.CMRentalTpl003]: TemplateRental,
    [ContractDraftTemplate.CMRentalTpl004]: TemplateRental,
    [ContractDraftTemplate.CFormat016]: Template6,
  };

  const currentTemplate = computed(() => {
    if (body.value.template) {
      const templateSelected = body.value.template;
      return TEMPLATE_COMPONENTS[templateSelected];
    }

    return null;
  });

  const getReviewDocumentAsync = async (id: string, procurementId: string, vendorId: string, documentType: string): Promise<string> => {
    const { data, status } = await contractDraftService.getReviewDocumentAsync(id, procurementId, vendorId, documentType);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const canSaveDateSign = computed(() => {
    return body.value.status === TContractDraftStatus.Approved && isHasPermission.value;
  });

  const canRecall = computed(() => {
    return body.value.status === TContractDraftStatus.Pending && isHasPermission.value;
  });

  const onUpsertAttachments = async (
    id: string,
    type: EntrepreneurType,
    attachments: EntrepreneurAttachments[]
  ) => {
    const otherTypeAttachments =
      body.value.checkerAttachments?.map(a => ({
        ...a,
        fileAttachments: a.fileAttachments.filter(f => f.type !== type)
      })).filter(a => a.fileAttachments.length > 0) ?? [];

    const newAttachments =
      attachments
        ?.map(att => ({
          ...att,
          fileAttachments: att.fileAttachments?.map(f => ({ ...f, type })) ?? []
        }))
        .filter(att => att.fileAttachments.length > 0) ?? [];

    body.value.checkerAttachments = [...otherTypeAttachments, ...newAttachments];

    const { status } = await contractDraftService.onUpsertAttachmentsAsync(
      id,
      body.value.checkerAttachments
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }
  };

  const onGetDefaultAcceptorAsync = async (): Promise<void> => {
    const operators = body.value.operators ?? [];

    const lastOperator = operators.length > 0
      ? operators.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, operators[0])
      : null;

    const dataSearch: defaultAcceptorCriteria = {
      processType: SectionProcessType.ContractDraft,
      budget: body.value.budget,
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
      supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
      userId: lastOperator ? lastOperator.userId : authStore.profile.id,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(dataSearch);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      const lastItem = data[data.length - 1];

      const existingAcceptorSignIndex = body.value.acceptors?.findIndex(
        a => a.acceptorType === AcceptorType.AcceptorSign
      ) ?? -1;

      if (existingAcceptorSignIndex !== -1) {
        body.value.acceptors[existingAcceptorSignIndex] = {
          ...body.value.acceptors[existingAcceptorSignIndex],
          userId: lastItem.userId,
          fullName: lastItem.fullName,
          positionName: lastItem.fullPositionName,
          departmentName: lastItem.businessUnitName,
        };
      } else {
        const newAcceptorSign: ParticipantsAcceptor = {
          acceptorType: AcceptorType.AcceptorSign,
          fullName: lastItem.fullName,
          positionName: lastItem.fullPositionName,
          sequence: 1,
          status: AcceptorStatus.Draft,
          userId: lastItem.userId,
          departmentName: lastItem.businessUnitName,
        } as ParticipantsAcceptor;

        body.value.acceptors = [newAcceptorSign, ...(body.value.acceptors ?? [])];
      }
    }
  };

  const onGetDefaultContractDirector = async (): Promise<void> => {
    const { data, status } = await operationService.getContractDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      const existingAcceptorSigns = (body.value.acceptors ?? []).filter(
        (a: ParticipantsAcceptor): boolean => a.acceptorType === AcceptorType.AcceptorSign
      );

      const newAcceptors: ParticipantsAcceptor[] = data.map((m, i: number): ParticipantsAcceptor => ({
        sequence: i + 1,
        userId: m.userId,
        fullName: m.fullName,
        positionName: m.positionName,
        departmentName: m.departmentName,
        acceptorType: AcceptorType.Approver,
        status: AcceptorStatus.Draft,
      } as ParticipantsAcceptor));

      body.value.acceptors = [...newAcceptors, ...existingAcceptorSigns];
    }
  };

  return {
    body,
    vendorId,
    onSelectLocationInfo,
    currentTemplate,
    vendorList,
    switchTemplate,
    onClearBody,
    cloneBody,
    isHasPermission,
    states: {
      isEdit,
      canEdit,
      canEditDoc,
      isPending,
      canAcceptAndReject,
      isLastApproval,
      canSaveDateSign,
      canRecall,
    },
    dropdown: {
      periodConditionTypeOptions,
      contractTypeOptions,
      templateTypeOptions,
      subTemplateTypeOptions,
      vatTypeOptions,
      unitTypeOptions,
      fineTypeOptions,
      unitMeaTypeOptions,
      attacementTypeOptions,
      periodTypeOptions,
      periodConTypeOptions,
      payTypeOptions,
      rCCRTypeOptions,
      bankOptions,
      warrantyOptions,
      conditionTypeOptions,
      periodOptions,
      pTimeTypeOptions,
    },
    api: {
      getPeriodConditionTypeAsync,
      getContractTypeAsync,
      getTemplateTypeAsync,
      getSubTemplateTypeAsync,
      getVatTypeAsync,
      getUnitTypeAsync,
      getFineTypeAsync,
      getUnitMeaTypeAsync,
      getAttacementTypeAsync,
      getAttacementGroupCodeTypeAsync,
      getPeriodTypeAsync,
      getPeriodConTypeAsync,
      getPayTypeAsync,
      getVendorListAsync,
      getContractDraftByVendorIdAsync,
      onUpdateContractDraft,
      approveAsync,
      rejectAsync,
      getRCCRTypeAsync,
      getWarrantyTypeAsync,
      getReviewDocumentAsync,
      getConditionTypeOptions,
      onUpsertAttachments,
      getBankAsync,
      getPeriodAsync,
      getPTimeTypeAsync,
      onGetDefaultAcceptorAsync,
      onGetDefaultContractDirector,
    }
  }
});