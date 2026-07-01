enum EWorkProcess {
  InProcess = 'InProcess',
  Related = 'Related',
  Completed = 'Completed',
  All = 'All',
}

enum EDateType {
  Year = 'Year',
  Month = 'Month',
  Day = 'Day',
}

enum EGroupCode {
  // ReturnCollateralTransferDocumentType
  RCTDT = 'RCTDT',
  // DocumentTemplate
  DocTpl = 'DocTpl',
  // DefectsWarrantyCountUnit
  DWCUnit = 'DWCUnit',
  // assignDepartment
  AssignDept = 'AssignDept',
  // AdvanceDeductionType
  ADType = 'ADType',
  // RevenueStamp
  RevStamp = 'RevStamp',
  // NoticationBeforeContractWorkPhaseDueDay
  NBCWPD = 'NBCWPD',
  // PmFineType
  PMFineType = 'PMFineType',
  // contractAppendixOther
  CAppendOther = 'CAppendOther',
  // Quarter
  Qtr = 'Qtr',
  // DefaultDutyProcurement60
  DDP60 = 'DDP60',
  // BiddingResultConsiderPeriodType
  BRCPType = 'BRCPType',
  // PerformanceBondType
  PBondType = 'PBondType',
  // CriteriaConsideration
  CriteriaCons = 'CriteriaCons',
  // DayOfWeek
  DOW = 'DOW',
  // VatType
  VATType = 'VATType',
  // traderType
  TraderType = 'TraderType',
  // positionOnBoardMA
  PosBoardMA = 'PosBoardMA',
  // MedianPriceConsiderInformation
  MPCInfo = 'MPCInfo',
  // ContractManagementType
  CMType = 'CMType',
  // NoticationContractReturnCollateralDay
  NCRCDay = 'NCRCDay',
  // maintenancePeriodType
  MPeriodType = 'MPeriodType',
  // WinnerReason
  WinReason = 'WinReason',
  // FineType
  FineType = 'FineType',
  // SectionContractFormat
  SecCFormat = 'SecCFormat',
  // WebLink
  WebLink = 'WebLink',
  // ContractAppendix
  CAppendix = 'CAppendix',
  // ReceivceCommittee
  RecCommittee = 'RecCommittee',
  // supplyMethodType
  SMethodType = 'SMethodType',
  // supplyMethod
  SMethod = 'SMethod',
  // RepairPeriodType
  RPeriodType = 'RPeriodType',
  // PeriodCondition
  PeriodCond = 'PeriodCond',
  // ProcessType
  ProcType = 'ProcType',
  // DeliveryCountUnit
  DelvCUnit = 'DelvCUnit',
  // PeriodTimeType
  PTimeType = 'PTimeType',
  // Bank
  Bank = 'Bank',
  // TrainingUnitType
  TUnitType = 'TUnitType',
  // PerformanceBondReturn
  PBondReturn = 'PBondReturn',
  // ContractType
  CType = 'CType',
  // DefaultDutyInspect80
  DDI80 = 'DDI80',
  // ProcurementCommerce
  ProcCom = 'ProcCom',
  // NoticationBeforeTheContractEndDay
  NBCEDay = 'NBCEDay',
  // ContractAttachment
  CAttachment = 'CAttachment',
  // positionOnBoardSupervisor
  PosBoardSup = 'PosBoardSup',
  // ReturnCollateralConsiderResultType
  RCCRType = 'RCCRType',
  // WarrantyCondition
  WtyCond = 'WtyCond',
  // DefaultDutyInspect60
  DDI60 = 'DDI60',
  // ApprovalAuthorityCommand
  AACmd = 'AACmd',
  // PeriodType
  PeriodType = 'PeriodType',
  // ContractStartDatePeriodCondition
  CSDPCond = 'CSDPCond',
  // ContractFormat
  CFormat = 'CFormat',
  // JorPorDirectorPosition
  JPDirPos = 'JPDirPos',
  // positionOnBoardProcurement
  PosBoardProc = 'PosBoardProc',
  // ContractFormatLevel2
  CFormatLv2 = 'CFormatLv2',
  // positionOnBoard
  PosBoard = 'PosBoard',
  // MedianPriceTemplate
  MPTpl = 'MPTpl',
  // positionOnBoardInSpection
  PosBoardInsp = 'PosBoardInsp',
  // PaymentType
  PayType = 'PayType',
  // DefaultDutyProcurement80
  DDP80 = 'DDP80',
  // CmFineType
  CMFineType = 'CMFineType',
  // BudgetConstant
  BdgConst = 'BdgConst',
  // UnitOfMea
  UnitOfMea = 'UnitOfMea',
  // GLAccount
  GLAcc = 'GLAcc',
  // BudgetType
  BudgetTyp = 'BudgetTyp',
  //ExpenseItemW119
  ExpenseItemW119 = 'ExpenseItemW119',
  //PaymentMethod
  PaymentMethod = 'PaymentMethod',

  PRentalTpl = 'PRentalTpl',

  CMRentalType = 'CMRentalType',

  CMRentalTpl = 'CMRentalTpl',
  //InvoiceDocumentType
  InvoiceDocType = 'InvoiceDocType',
  //SolId
  SolId = 'SolId',
  //PettyCashStandardType
  PettyCashStandardType = 'PettyCashStandardType',
  //PettyCashConvenienceType
  PettyCashConvenienceType = 'PettyCashConvenienceType',
  //PettyCashWithoutForm001Type
  PettyCashWithoutForm001Type = 'PettyCashWithoutForm001Type',
  SplitPayment = 'SplitPayment',
  AttachmentType = 'AttachmentType',
  MedianPriceDuties = 'MedianPriceDuties',
  TorDuties = 'TorDuties',

  ContractDefectWarranty = 'ContractDefectWarranty',

  CTR = 'CTR',

  // AnnouncementCategory
  AnnCategory = 'AnnCategory',

  // AnnouncementReportType
  AnnReportType = 'AnnReportType',
}

/**
 * TemplateGroup
 *
 *  **Appoint**
 *    - Ap
 *    - Apcancel
 *    - Apedit
 *
 *  **Contract Management**
 *    - Cm
 *    - Cm80
 *    - CmAd
 *    - CmChange60
 *    - CmChange80
 *    - CmCltr
 *    - CmComplete
 *    - CmInv
 *    - CmPdpa
 *    - CmPdpa80
 *    - Cmr
 *    - CmSign
 *
 *  **Element**
 *    - Element
 *
 *  **JP04**
 *    - Jp04
 *
 *  **JP05**
 *    - Jp05
 *    - JP05Ord
 *
 *  **Jp06**
 *    - Jp06
 *
 *  **Median Price**
 *    - Mdp
 *
 *  **TOR**
 *    - Tor
 *
 *  **Plan**
 *    - Pl
 *    - Plcancel
 *    - Pledit
 *
 *  **User Manual**
 *    - UserManual
 *
 *  **WA**
 *    - WA
 */
enum TemplateGroup {
  Ap = 'Ap',
  Apcancel = 'Apcancel',
  Apedit = 'Apedit',
  Cm = 'Cm',
  Cm80 = 'Cm80',
  CmAd = 'CmAd',
  CmChange60 = 'CmChange60',
  CmChange80 = 'CmChange80',
  CmCltr = 'CmCltr',
  CmComplete = 'CmComplete',
  CmInv = 'CmInv',
  CmPdpa = 'CmPdpa',
  CmPdpa80 = 'CmPdpa80',
  Cmr = 'Cmr',
  CmSign = 'CmSign',
  Element = 'Element',
  Inv = 'Inv',
  Jp04 = 'Jp04',
  Jp05 = 'Jp05',
  JP05Ord = 'JP05Ord',
  Jp06 = 'Jp06',
  Mdp = 'Mdp',
  Pl = 'Pl',
  Plcancel = 'Plcancel',
  Pledit = 'Pledit',
  Tor = 'Tor',
  UserManual = 'UserManual',
  Wa = 'Wa',
}


enum OrganizationLevelEnum {
  Head = 100,
  Group = 200,
  Line = 300,
  Department = 400,
  Segment = 600,
  Center = 401,
  Zone = 500,
  Branch = 601,
}

enum CommitteeType {
  TorDraft = 'TorDraft',
  MedianPrice = 'MedianPrice',
}

enum EntrepreneurType {
  Egp = 'Egp',
  Coi = 'Coi',
  Watchlist = 'Watchlist',
}

enum PeriodTypeCodeEnum {
  //วัน
  Day = 'PeriodType001',
  //เดือน
  Month = 'PeriodType002',
  //ปี
  Year = 'PeriodType003',
  //งวด
  PeriodType004 = 'PeriodType004'
}

enum ProRateTypeCodeEnum {
  //แบ่งจ่ายรายงวด
  SplitPayment001 = 'SplitPayment001',
  //ชำระเงินครั้งเดียว
  SplitPayment002 = 'SplitPayment002',
  // MA
  SplitPayment003 = 'SplitPayment003',
}

enum AssignDepartmentCodeEnum {
  // ส่วนงานอื่น (SegmentJorPorOther) → default-segment-other-manager
  SegmentJorPorOther = 'AssignDept001',
  // ส่วนงาน IT (SegmentJorPorIT) → default-segment-it-manager
  SegmentJorPorIT = 'AssignDept002',
}

export {
  EWorkProcess,
  EDateType,
  EGroupCode,
  OrganizationLevelEnum,
  CommitteeType,
  TemplateGroup,
  EntrepreneurType,
  PeriodTypeCodeEnum,
  ProRateTypeCodeEnum,
  AssignDepartmentCodeEnum
}