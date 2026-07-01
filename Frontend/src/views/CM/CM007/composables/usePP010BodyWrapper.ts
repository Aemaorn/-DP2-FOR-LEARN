import type { Cm007Detail, Cm007OldData } from '@/models/CM/cm007';

/**
 * Transforms CM007 API data into PP010-compatible structure by building
 * a `detail` sub-object. The API now returns a flat structure that mirrors
 * ContractDraftDetailBase, so no unwrapping of TermsConditions/EquipmentRental
 * wrappers is needed. Payment terms are already inside payment.details.
 *
 * PP010 expects: body.detail.payment.details, body.detail.warranty, etc.
 * CM007 API now returns: body.payment (with details), body.warranty, etc.
 */
export function apiToPP010(source: Cm007Detail | Cm007OldData, parent?: Cm007Detail): void {
  const b = source as any;
  if (b.__pp010Transformed) return;

  // Ensure termination.vendorProcessingTime is never null (PP010 v-model binds directly to .year/.month/.day)
  if (b.termination && !b.termination.vendorProcessingTime) {
    b.termination.vendorProcessingTime = { year: 0, month: 0, day: 0 };
  }

  // Build detail object from flat response fields (same shape PP010 expects)
  b.detail = {
    buyer: b.buyer,
    vendor: b.vendor,
    agreement: b.agreement,
    payment: b.payment,
    delivery: b.delivery,
    termination: b.termination,
    attachments: b.attachments ?? [],
    warranty: b.warranty,
    guarantee: b.guarantee,
    penalty: b.penalty ?? { isPenalty: true },
    redelivery: b.redelivery,
    advancePayment: b.advancePayment,
    retentionPayment: b.retentionPayment,
    defectWarrantyTypeCode: b.defectWarrantyTypeCode,
    copierLease: b.copierLease,
    carLease: b.carLease,
    computerLease: b.computerLease,
  };

  // For oldData: copy read-only fields from parent that PP010 components need
  if (parent) {
    const p = parent as any;
    if (b.budget == null) b.budget = p.budget;
    b.template = p.templateCode;
    b.templateCode = p.templateCode;
    b.contractNumber = p.contractNumber;
    b.id = p.id;
    b.status = p.status;
    b.contractStatus = p.contractStatus;
    b.periodConditionType = p.periodConditionTypeCode;
    b.periodConditionTypeCode = p.periodConditionTypeCode;
  } else {
    // PP010 accesses body.template (CM007 has templateCode)
    if (b.templateCode !== undefined) {
      b.template = b.templateCode;
    }
    // PP010 accesses body.periodConditionType (CM007 has periodConditionTypeCode)
    if (b.periodConditionTypeCode !== undefined) {
      b.periodConditionType = b.periodConditionTypeCode;
    }
  }

  b.__pp010Transformed = true;
}

/**
 * Syncs PP010 `detail` object back to CM007 flat structure before saving.
 * Call this before sending data to the API.
 */
export function pp010ToApi(source: Cm007Detail): void {
  const b = source as any;
  const d = b.detail;
  if (!d) return;

  // Sync section data back to flat body fields
  b.buyer = d.buyer;
  b.vendor = d.vendor;
  b.agreement = d.agreement;
  b.payment = d.payment;
  b.delivery = d.delivery;
  b.termination = d.termination;
  b.attachments = d.attachments ?? [];
  b.warranty = d.warranty;
  b.guarantee = d.guarantee;
  b.penalty = d.penalty;
  b.redelivery = d.redelivery;
  b.advancePayment = d.advancePayment;
  b.retentionPayment = d.retentionPayment;
  b.defectWarrantyTypeCode = d.defectWarrantyTypeCode;
  b.copierLease = d.copierLease;
  b.carLease = d.carLease;
  b.computerLease = d.computerLease;

  // Sync aliases back
  if (b.template !== undefined) {
    b.templateCode = b.template;
  }
  if (b.periodConditionType !== undefined) {
    b.periodConditionTypeCode = b.periodConditionType;
  }
}
