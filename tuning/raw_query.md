# Raw SQL Queries - Worklist API

จากการวิเคราะห์ log พบว่า API `/api/worklist` ใช้เวลาทั้งหมด **59,719ms (~60 วินาที)**
โดยมี queries หลักๆ ดังนี้:

---

## 1. User Context Query (11ms)

```sql
SELECT s4."Id", s4."EmployeeCode", s4."IsActive", s4."SignatureImageId",
       s4."CreatedAt", s4."CreatedBy", s4."CreatedByName", s4."LastModifiedAt",
       s4."LastModifiedBy", s4."LastModifiedByName", s4.c, s4."Id0",
       s4."BirthDate", s4."CitizenCardId", s4."CreatedAt0", s4."Email",
       s4."FirstName", s4."LastName", s4."Remark", s4."Title", s4."UpdatedAt",
       s4."EmployeeCode0", s5."EmployeeCode", s5."PositionId", s5."BusinessUnitId",
       s5."Acting", s5."EmployeeType", s5."ManagerEmployeeCode", s5."Remark",
       s5."Id", s5."BusinessUnitCode", s5."CreatedAt", s5."Level", s5."Name",
       s5."OrganizationLevel", s5."ParentId", s5."Remark0", s5."ShortName",
       s5."UpdatedAt", s5."Value", s5."Value2", s5."Value3", s4."RawBusinessUnitId",
       s4."RawBusinessUnitName", s4."FullName", s4."FullPositionName", s4."PositionId",
       s6."Id", s6."Acting", s6."Active", s6."BusinessUnitId", s6."DelegatorBusinessUnitId",
       s6."DelegatorId", s6."DelegatorPositionId", s6."DelegatorPositionName",
       s6."EmployeeCode", s6."FullPositionName", s6."ParentBusinessUnitId",
       s6."PositionId", s6."Sequence", s6."SuUserId", s6."SubBusinessUnitId",
       s6."UserFullName", s6."CreatedAt", s6."CreatedBy", s6."CreatedByName",
       s6."LastModifiedAt", s6."LastModifiedBy", s6."LastModifiedByName",
       s6."Id0", s6."Annotation", s6."DelegationEndDate", s6."DelegationStartDate",
       s6."EmployeeCode0", s6."FullPositionName0", s6."IsDeleted", s6."PositionId0",
       s6."SuUserId0", s6."UserFullName0", s6."CreatedAt0", s6."CreatedBy0",
       s6."CreatedByName0", s6."LastModifiedAt0", s6."LastModifiedBy0",
       s6."LastModifiedByName0", s6."Id1", s6."EmployeeCode1", s6."IsActive",
       s6."SignatureImageId", s6."CreatedAt1", s6."CreatedBy1", s6."CreatedByName1",
       s6."LastModifiedAt1", s6."LastModifiedBy1", s6."LastModifiedByName1",
       s6.c, s6."Id2", s6."BirthDate", s6."CitizenCardId", s6."CreatedAt2",
       s6."Email", s6."FirstName", s6."LastName", s6."Remark", s6."Title",
       s6."UpdatedAt", s6."EmployeeCode2", s6."RawBusinessUnitId", s6."RawBusinessUnitName",
       s6."FullName", s6."FullPositionName1", s6."PositionId1"
FROM (
    SELECT s."Id", s."EmployeeCode", s."IsActive", s."SignatureImageId",
           s."CreatedAt", s."CreatedBy", s."CreatedByName", s."LastModifiedAt",
           s."LastModifiedBy", s."LastModifiedByName", s."OtherInfo" AS c,
           r."Id" AS "Id0", r."BirthDate", r."CitizenCardId",
           r."CreatedAt" AS "CreatedAt0", r."Email", r."FirstName", r."LastName",
           r."Remark", r."Title", r."UpdatedAt", r0."EmployeeCode" AS "EmployeeCode0",
           r0."RawBusinessUnitId", r0."RawBusinessUnitName", r0."FullName",
           r0."FullPositionName", r0."PositionId"
    FROM "SystemUtility"."SuUser" AS s
    INNER JOIN "Raws"."RawEmployee" AS r ON s."EmployeeCode" = r."Id"
    LEFT JOIN "Raws".raw_employee_view AS r0 ON r."Id" = r0."EmployeeCode"
    WHERE s."Id" = @__userId_0
    LIMIT 1
) AS s4
LEFT JOIN (
    SELECT r1."EmployeeCode", r1."PositionId", r1."BusinessUnitId", r1."Acting",
           r1."EmployeeType", r1."ManagerEmployeeCode", r1."Remark",
           r2."Id", r2."BusinessUnitCode", r2."CreatedAt", r2."Level", r2."Name",
           r2."OrganizationLevel", r2."ParentId", r2."Remark" AS "Remark0",
           r2."ShortName", r2."UpdatedAt", r2."Value", r2."Value2", r2."Value3"
    FROM "Raws"."RawEmployeePosition" AS r1
    INNER JOIN "Raws"."RawBusinessUnit" AS r2 ON r1."BusinessUnitId" = r2."Id"
) AS s5 ON s4."Id0" = s5."EmployeeCode"
LEFT JOIN (
    SELECT s0."Id", s0."Acting", s0."Active", s0."BusinessUnitId",
           s0."DelegatorBusinessUnitId", s0."DelegatorId", s0."DelegatorPositionId",
           s0."DelegatorPositionName", s0."EmployeeCode", s0."FullPositionName",
           s0."ParentBusinessUnitId", s0."PositionId", s0."Sequence", s0."SuUserId",
           s0."SubBusinessUnitId", s0."UserFullName", s0."CreatedAt", s0."CreatedBy",
           s0."CreatedByName", s0."LastModifiedAt", s0."LastModifiedBy",
           s0."LastModifiedByName", s2."Id" AS "Id0", s2."Annotation",
           s2."DelegationEndDate", s2."DelegationStartDate",
           s2."EmployeeCode" AS "EmployeeCode0", s2."FullPositionName" AS "FullPositionName0",
           s2."IsDeleted", s2."PositionId" AS "PositionId0", s2."SuUserId" AS "SuUserId0",
           s2."UserFullName" AS "UserFullName0", s2."CreatedAt" AS "CreatedAt0",
           s2."CreatedBy" AS "CreatedBy0", s2."CreatedByName" AS "CreatedByName0",
           s2."LastModifiedAt" AS "LastModifiedAt0", s2."LastModifiedBy" AS "LastModifiedBy0",
           s2."LastModifiedByName" AS "LastModifiedByName0", s3."Id" AS "Id1",
           s3."EmployeeCode" AS "EmployeeCode1", s3."IsActive", s3."SignatureImageId",
           s3."CreatedAt" AS "CreatedAt1", s3."CreatedBy" AS "CreatedBy1",
           s3."CreatedByName" AS "CreatedByName1", s3."LastModifiedAt" AS "LastModifiedAt1",
           s3."LastModifiedBy" AS "LastModifiedBy1", s3."LastModifiedByName" AS "LastModifiedByName1",
           s3."OtherInfo" AS c, r3."Id" AS "Id2", r3."BirthDate", r3."CitizenCardId",
           r3."CreatedAt" AS "CreatedAt2", r3."Email", r3."FirstName", r3."LastName",
           r3."Remark", r3."Title", r3."UpdatedAt", r4."EmployeeCode" AS "EmployeeCode2",
           r4."RawBusinessUnitId", r4."RawBusinessUnitName", r4."FullName",
           r4."FullPositionName" AS "FullPositionName1", r4."PositionId" AS "PositionId1"
    FROM "SystemUtility"."SuDelegatee" AS s0
    INNER JOIN (
        SELECT s1."Id", s1."Annotation", s1."DelegationEndDate", s1."DelegationStartDate",
               s1."EmployeeCode", s1."FullPositionName", s1."IsDeleted", s1."PositionId",
               s1."SuUserId", s1."UserFullName", s1."CreatedAt", s1."CreatedBy",
               s1."CreatedByName", s1."LastModifiedAt", s1."LastModifiedBy",
               s1."LastModifiedByName"
        FROM "SystemUtility"."SuDelegator" AS s1
        WHERE NOT (s1."IsDeleted")
    ) AS s2 ON s0."DelegatorId" = s2."Id"
    INNER JOIN "SystemUtility"."SuUser" AS s3 ON s2."SuUserId" = s3."Id"
    INNER JOIN "Raws"."RawEmployee" AS r3 ON s3."EmployeeCode" = r3."Id"
    LEFT JOIN "Raws".raw_employee_view AS r4 ON r3."Id" = r4."EmployeeCode"
) AS s6 ON s4."Id" = s6."SuUserId"
ORDER BY s4."Id", s4."Id0", s4."EmployeeCode0", s5."EmployeeCode", s5."PositionId",
         s5."BusinessUnitId", s5."Acting", s5."Id", s6."Id", s6."Id0", s6."Id1", s6."Id2";
```

---

## 2. Plan Count Query (59ms)

```sql
SELECT count(*)::int
FROM "Plan"."Plan" AS p
WHERE NOT (p."IsDeleted")
  AND p."IsActive"
  AND (
    -- Creator conditions
    (p."Status" IN ('DraftPlan', 'RejectPlan', 'EditPlan') AND p."CreatedBy" = ANY (@__createdBys_0))
    OR
    -- Approver conditions
    (p."Status" IN ('WaitingApprovePlan', 'WaitingAcceptor') AND EXISTS (
        SELECT 1
        FROM "Plan"."PlanAcceptor" AS p0
        LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p0."DelegateeId" = s."Id"
        WHERE p."Id" = p0."PlanId"
          AND p0."IsCurrent"
          AND (p0."UserId" = ANY (@__userIds_1)
               OR (s."Id" IS NOT NULL
                   AND (s."SuUserId" = ANY (@__userIds_1_1)
                        OR (s."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
          AND p0."Type" IN ('Approver', 'DepartmentDirectorAgree')
    ))
    OR
    -- Director conditions
    (p."Status" IN ('WaitingAssign', 'WaitingAnnouncement') AND (
        (SELECT p1."UserId"
         FROM "Plan"."PlanAssignee" AS p1
         WHERE p."Id" = p1."PlanId" AND p1."Type" = 'Director'
         ORDER BY p1."Sequence" DESC
         LIMIT 1) = ANY (@__userIds_1)
        OR
        ((SELECT p1."UserId"
          FROM "Plan"."PlanAssignee" AS p1
          WHERE p."Id" = p1."PlanId" AND p1."Type" = 'Director'
          ORDER BY p1."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userIds_1, NULL) IS NOT NULL)
    ))
    OR
    -- Assignee conditions
    (p."Status" IN ('DraftRecordDocument', 'RejectToAssignee', 'Assigned') AND (
        (SELECT p2."UserId"
         FROM "Plan"."PlanAssignee" AS p2
         WHERE p."Id" = p2."PlanId" AND p2."Type" = 'Assignee'
         ORDER BY p2."Sequence" DESC
         LIMIT 1) = ANY (@__userIds_1)
        OR
        ((SELECT p2."UserId"
          FROM "Plan"."PlanAssignee" AS p2
          WHERE p."Id" = p2."PlanId" AND p2."Type" = 'Assignee'
          ORDER BY p2."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userIds_1, NULL) IS NOT NULL)
    ))
  );
```

---

## 3. Plan Announcement Count Query (3ms)

```sql
SELECT count(*)::int
FROM "Plan"."PlanAnnouncement" AS p
WHERE NOT (p."IsDeleted")
  AND (
    -- Director access
    (p."Status" IN ('Draft', 'WaitingAnnouncement', 'WaitingAssign', 'Rejected') AND EXISTS (
        SELECT 1
        FROM "Plan"."PlanAnnouncementAssignee" AS p0
        WHERE p."Id" = p0."PlanAnnouncementId"
          AND p0."UserId" = ANY (@__userIds_0)
          AND p0."Type" = 'Director'
    ))
    OR
    -- Assignee access
    (p."Status" = 'WaitingAssign' AND (
        (SELECT p1."UserId"
         FROM "Plan"."PlanAnnouncementAssignee" AS p1
         WHERE p."Id" = p1."PlanAnnouncementId" AND p1."Type" = 'Assignee'
         ORDER BY p1."Sequence" DESC
         LIMIT 1) = ANY (@__userIds_0)
        OR
        ((SELECT p1."UserId"
          FROM "Plan"."PlanAnnouncementAssignee" AS p1
          WHERE p."Id" = p1."PlanAnnouncementId" AND p1."Type" = 'Assignee'
          ORDER BY p1."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userIds_0, NULL) IS NOT NULL)
    ))
    OR
    -- Acceptor access
    (p."Status" = 'WaitingAcceptor' AND EXISTS (
        SELECT 1
        FROM "Plan"."PlanAnnouncementAcceptor" AS p2
        LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p2."DelegateeId" = s."Id"
        WHERE p."Id" = p2."PlanAnnouncementId"
          AND p2."IsCurrent"
          AND (p2."UserId" = ANY (@__userIds_0)
               OR (s."Id" IS NOT NULL
                   AND (s."SuUserId" = ANY (@__userIds_0_1)
                        OR (s."SuUserId" IS NULL AND array_position(@__userIds_0_1, NULL) IS NOT NULL))))
    ))
  );
```

---

## 4. Procurement Count Query - PreProcurement Step (10,021ms) - CRITICAL BOTTLENECK

**หมายเหตุ:** Query นี้ใช้เวลานานมากเพราะมี 134+ EXISTS subqueries

```sql
SELECT count(*)::int
FROM "Procurement"."Procurement" AS p
WHERE NOT (p."IsDeleted")
  AND (
    -- Step: PreProcurement, Procurement
    (p."Step" IN ('PreProcurement', 'Procurement') AND (
        -- PpAppoint Approver check
        EXISTS (
            SELECT 1
            FROM "Procurement"."PpAppoint" AS p0
            WHERE NOT (p0."IsDeleted")
              AND p."Id" = p0."ProcurementId"
              AND p0."Status" = 'WaitingApproval'
              AND EXISTS (
                  SELECT 1
                  FROM "Procurement"."PpAppointAcceptors" AS p1
                  LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p1."DelegateeId" = s."Id"
                  WHERE p0."Id" = p1."PpAppointId"
                    AND p1."Type" = 'Approver'
                    AND p1."Status" = 'Pending'
                    AND p1."Sequence" = (
                        SELECT min(p2."Sequence")
                        FROM "Procurement"."PpAppointAcceptors" AS p2
                        WHERE p0."Id" = p2."PpAppointId" AND p2."Status" = 'Pending'
                    )
                    AND (p1."UserId" = ANY (@__userIds_0)
                         OR (s."Id" IS NOT NULL
                             AND (s."SuUserId" = ANY (@__userIds_0_1)
                                  OR (s."SuUserId" IS NULL AND array_position(@__userIds_0_1, NULL) IS NOT NULL))))
              )
        )
        OR
        -- PpTorDraft Approver check
        EXISTS (
            SELECT 1
            FROM "Procurement"."PpTorDraft" AS p3
            WHERE NOT (p3."IsDeleted")
              AND p."Id" = p3."ProcurementId"
              AND p3."Status" = 'WaitingApproval'
              AND EXISTS (
                  SELECT 1
                  FROM "Procurement"."PpTorDraftAcceptors" AS p4
                  LEFT JOIN "SystemUtility"."SuDelegatee" AS s0 ON p4."DelegateeId" = s0."Id"
                  WHERE NOT (p4."IsDeleted")
                    AND p3."Id" = p4."PpTorDraftId"
                    AND p4."Type" = 'Approver'
                    AND p4."Status" = 'Pending'
                    AND NOT (p4."IsDeleted")
                    AND p4."Sequence" = (
                        SELECT min(p5."Sequence")
                        FROM "Procurement"."PpTorDraftAcceptors" AS p5
                        WHERE NOT (p5."IsDeleted")
                          AND p3."Id" = p5."PpTorDraftId"
                          AND NOT (p5."IsDeleted")
                          AND p5."Status" = 'Pending'
                    )
                    AND (p4."UserId" = ANY (@__userIds_0)
                         OR (s0."Id" IS NOT NULL
                             AND (s0."SuUserId" = ANY (@__userIds_0_1)
                                  OR (s0."SuUserId" IS NULL AND array_position(@__userIds_0_1, NULL) IS NOT NULL))))
              )
        )
        OR
        -- PpMedianPrice Approver check
        EXISTS (
            SELECT 1
            FROM "Procurement"."PpMedianPrice" AS p6
            WHERE NOT (p6."IsDeleted")
              AND p."Id" = p6."ProcurementId"
              AND p6."Status" = 'WaitingApproval'
              AND EXISTS (
                  SELECT 1
                  FROM "Procurement"."PpMedianPriceAcceptor" AS p7
                  LEFT JOIN "SystemUtility"."SuDelegatee" AS s1 ON p7."DelegateeId" = s1."Id"
                  WHERE NOT (p7."IsDeleted")
                    AND p6."Id" = p7."MedianPriceId"
                    AND p7."Type" = 'Approver'
                    AND p7."Status" = 'Pending'
                    AND NOT (p7."IsDeleted")
                    AND p7."Sequence" = (
                        SELECT min(p8."Sequence")
                        FROM "Procurement"."PpMedianPriceAcceptor" AS p8
                        WHERE NOT (p8."IsDeleted")
                          AND p6."Id" = p8."MedianPriceId"
                          AND NOT (p8."IsDeleted")
                          AND p8."Status" = 'Pending'
                    )
                    AND (p7."UserId" = ANY (@__userIds_0)
                         OR (s1."Id" IS NOT NULL
                             AND (s1."SuUserId" = ANY (@__userIds_0_1)
                                  OR (s1."SuUserId" IS NULL AND array_position(@__userIds_0_1, NULL) IS NOT NULL))))
              )
        )
        OR
        -- PpPurchaseRequisition Approver check
        EXISTS (
            SELECT 1
            FROM "Procurement"."PpPurchaseRequisition" AS p9
            WHERE NOT (p9."IsDeleted")
              AND p."Id" = p9."ProcurementId"
              AND p9."Status" = 'WaitingApproval'
              AND EXISTS (
                  SELECT 1
                  FROM "Procurement"."PpPurchaseRequisitionAcceptors" AS p10
                  LEFT JOIN "SystemUtility"."SuDelegatee" AS s2 ON p10."DelegateeId" = s2."Id"
                  WHERE NOT (p10."IsDeleted")
                    AND p9."Id" = p10."PpPurchaseRequisitionId"
                    AND p10."Type" = 'Approver'
                    AND p10."Status" = 'Pending'
                    AND NOT (p10."IsDeleted")
                    AND p10."Sequence" = (
                        SELECT min(p11."Sequence")
                        FROM "Procurement"."PpPurchaseRequisitionAcceptors" AS p11
                        WHERE NOT (p11."IsDeleted")
                          AND p9."Id" = p11."PpPurchaseRequisitionId"
                          AND NOT (p11."IsDeleted")
                          AND p11."Status" = 'Pending'
                    )
                    AND (p10."UserId" = ANY (@__userIds_0)
                         OR (s2."Id" IS NOT NULL
                             AND (s2."SuUserId" = ANY (@__userIds_0_1)
                                  OR (s2."SuUserId" IS NULL AND array_position(@__userIds_0_1, NULL) IS NOT NULL))))
              )
        )
        OR
        -- TorDraft DepartmentDirectorAgree
        EXISTS (
            SELECT 1
            FROM "Procurement"."PpTorDraft" AS p12
            WHERE NOT (p12."IsDeleted")
              AND p."Id" = p12."ProcurementId"
              AND p12."Status" = 'WaitingUnitApproval'
              AND EXISTS (
                  SELECT 1
                  FROM "Procurement"."PpTorDraftAcceptors" AS p13
                  LEFT JOIN "SystemUtility"."SuDelegatee" AS s3 ON p13."DelegateeId" = s3."Id"
                  WHERE NOT (p13."IsDeleted")
                    AND p12."Id" = p13."PpTorDraftId"
                    AND NOT (p13."IsDeleted")
                    AND p13."Type" = 'DepartmentDirectorAgree'
                    AND p13."IsCurrent"
                    AND (p13."UserId" = ANY (@__userIds_1)
                         OR (s3."Id" IS NOT NULL
                             AND (s3."SuUserId" = ANY (@__userIds_1_1)
                                  OR (s3."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
              )
        )
        OR
        -- MedianPrice DepartmentDirectorAgree
        EXISTS (
            SELECT 1
            FROM "Procurement"."PpMedianPrice" AS p14
            WHERE NOT (p14."IsDeleted")
              AND p."Id" = p14."ProcurementId"
              AND NOT (p14."IsDeleted")
              AND p14."Status" = 'WaitingUnitApproval'
              AND EXISTS (
                  SELECT 1
                  FROM "Procurement"."PpMedianPriceAcceptor" AS p15
                  LEFT JOIN "SystemUtility"."SuDelegatee" AS s4 ON p15."DelegateeId" = s4."Id"
                  WHERE NOT (p15."IsDeleted")
                    AND p14."Id" = p15."MedianPriceId"
                    AND p15."Type" = 'DepartmentDirectorAgree'
                    AND p15."IsCurrent"
                    AND (p15."UserId" = ANY (@__userIds_1)
                         OR (s4."Id" IS NOT NULL
                             AND (s4."SuUserId" = ANY (@__userIds_1_1)
                                  OR (s4."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
              )
        )
        -- ... (ยังมี conditions อีกมากมาย สำหรับ TorDraft Committee, MedianPrice Committee,
        --      PrincipleApprovalRental, Draft/Edit/Rejected statuses, Assignees, etc.)
    ))
    -- ... (ยังมี conditions สำหรับ Step = 'Procurement' และ Step = 'ContractAgreement')
  )
  AND NOT (p."IsDeleted")
  AND p."Step" = 'PreProcurement';
```

---

## 5. Pw119 Count Query (2ms)

```sql
SELECT count(*)::int
FROM "Procurement"."Pw119" AS p
WHERE NOT (p."IsDeleted")
  AND (
    (p."Status" IN ('Draft', 'Rejected', 'Edit') AND p."CreatedBy" = @__userId_0)
    OR
    (p."Status" = 'WaitingApproval' AND EXISTS (
        SELECT 1
        FROM "Procurement"."Pw119Acceptor" AS p0
        LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p0."DelegateeId" = s."Id"
        WHERE NOT (p0."IsDeleted")
          AND p."Id" = p0."Pw119Id"
          AND p0."IsCurrent"
          AND NOT (p0."IsDeleted")
          AND (p0."UserId" = ANY (@__userIds_1)
               OR (s."Id" IS NOT NULL
                   AND (s."SuUserId" = ANY (@__userIds_1_1)
                        OR (s."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
          AND p0."Status" = 'Pending'
          AND p0."Type" IN ('Approver', 'DepartmentDirectorAgree')
    ))
  );
```

---

## 6. P79Clause2 Count Query (2ms)

```sql
SELECT count(*)::int
FROM "Procurement"."P79Clause2" AS p
WHERE NOT (p."IsDeleted")
  AND (
    (p."Status" IN ('Draft', 'Rejected', 'Edit') AND p."CreatedBy" = @__userId_0)
    OR
    (p."Status" = 'WaitingApproval' AND EXISTS (
        SELECT 1
        FROM "Procurement"."P79Clause2Acceptor" AS p0
        LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p0."DelegateeId" = s."Id"
        WHERE NOT (p0."IsDeleted")
          AND p."Id" = p0."P79Clause2Id"
          AND p0."IsCurrent"
          AND NOT (p0."IsDeleted")
          AND (p0."UserId" = ANY (@__userIds_1)
               OR (s."Id" IS NOT NULL
                   AND (s."SuUserId" = ANY (@__userIds_1_1)
                        OR (s."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
          AND p0."Status" = 'Pending'
          AND p0."Type" IN ('Approver', 'DepartmentDirectorAgree')
    ))
  );
```

---

## 7. PPettyCash Count Query (3ms)

```sql
SELECT count(*)::int
FROM "Procurement"."PPettyCash" AS p
WHERE NOT (p."IsDeleted")
  AND (
    (p."Status" IN ('Draft', 'Rejected', 'Edit') AND p."CreatedBy" = @__userId_0)
    OR
    (p."Status" = 'WaitingApproval' AND EXISTS (
        SELECT 1
        FROM "Procurement"."PPettyCashAcceptor" AS p0
        LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p0."DelegateeId" = s."Id"
        WHERE NOT (p0."IsDeleted")
          AND p."Id" = p0."PettyCashId"
          AND p0."Type" = 'DepartmentDirectorAgree'
          AND NOT (p0."IsDeleted")
          AND (p0."UserId" = ANY (@__userIds_1)
               OR (s."Id" IS NOT NULL
                   AND (s."SuUserId" = ANY (@__userIds_1_1)
                        OR (s."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
          AND p0."Status" = 'Pending'
    ))
    OR
    (p."Status" = 'WaitingForInspector' AND EXISTS (
        SELECT 1
        FROM "Procurement"."PPettyCashAcceptor" AS p1
        WHERE NOT (p1."IsDeleted")
          AND p."Id" = p1."PettyCashId"
          AND p1."Type" = 'InspectionCommittee'
          AND NOT (p1."IsDeleted")
          AND p1."UserId" = ANY (@__userIds_1)
          AND p1."Status" = 'Pending'
    ))
    OR
    (p."Status" = 'WaitingForAssignment' AND (
        (SELECT p2."UserId"
         FROM "Procurement"."PPettyCashAssignee" AS p2
         WHERE p."Id" = p2."PettyCashId" AND p2."Type" = 'Director'
         ORDER BY p2."Sequence" DESC
         LIMIT 1) = ANY (@__userIds_1)
        OR
        ((SELECT p2."UserId"
          FROM "Procurement"."PPettyCashAssignee" AS p2
          WHERE p."Id" = p2."PettyCashId" AND p2."Type" = 'Director'
          ORDER BY p2."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userIds_1, NULL) IS NOT NULL)
    ))
    OR
    (p."Status" = 'WaitingForCompletion' AND (
        (SELECT p3."UserId"
         FROM "Procurement"."PPettyCashAssignee" AS p3
         WHERE p."Id" = p3."PettyCashId" AND p3."Type" = 'Assignee'
         ORDER BY p3."Sequence" DESC
         LIMIT 1) = ANY (@__userIds_1)
        OR
        ((SELECT p3."UserId"
          FROM "Procurement"."PPettyCashAssignee" AS p3
          WHERE p."Id" = p3."PettyCashId" AND p3."Type" = 'Assignee'
          ORDER BY p3."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userIds_1, NULL) IS NOT NULL)
    ))
  );
```

---

## 8. PPettyCashReimbursement Count Query (2ms)

```sql
SELECT count(*)::int
FROM "Procurement"."PPettyCashReimbursement" AS p
WHERE NOT (p."IsDeleted")
  AND (
    (p."Status" IN ('Draft', 'Rejected', 'Edit') AND p."CreatedBy" = @__userId_0)
    OR
    (p."Status" = 'WaitingApproval' AND EXISTS (
        SELECT 1
        FROM "Procurement"."PPettyCashReimbursementAcceptor" AS p0
        LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p0."DelegateeId" = s."Id"
        WHERE NOT (p0."IsDeleted")
          AND p."Id" = p0."PettyCashReimbursementId"
          AND p0."IsCurrent"
          AND NOT (p0."IsDeleted")
          AND (p0."UserId" = ANY (@__userIds_1)
               OR (s."Id" IS NOT NULL
                   AND (s."SuUserId" = ANY (@__userIds_1_1)
                        OR (s."SuUserId" IS NULL AND array_position(@__userIds_1_1, NULL) IS NOT NULL))))
          AND p0."Status" = 'Pending'
          AND p0."Type" IN ('Approver', 'DepartmentDirectorAgree')
    ))
  );
```

---

## 9. DeliveryAcceptancePeriod Count Query (6ms)

```sql
SELECT count(*)::int
FROM "ContractManagement"."CmDeliveryAcceptancePeriod" AS c
INNER JOIN (
    SELECT c0."Id", c0."ContractDraftVendorId", c0."Status"
    FROM "ContractManagement"."CmDeliveryAcceptance" AS c0
    WHERE NOT (c0."IsDeleted")
) AS c1 ON c."CmDeliveryAcceptanceId" = c1."Id"
INNER JOIN (
    SELECT c2."Id", c2."ContractDraftId"
    FROM "ContractAgreement"."CaContractDraftVendor" AS c2
    WHERE NOT (c2."IsDeleted")
) AS c3 ON c1."ContractDraftVendorId" = c3."Id"
INNER JOIN (
    SELECT c4."Id", c4."ProcurementId"
    FROM "ContractAgreement"."CaContractDraft" AS c4
    WHERE NOT (c4."IsDeleted")
) AS c5 ON c3."ContractDraftId" = c5."Id"
INNER JOIN (
    SELECT p."Id"
    FROM "Procurement"."Procurement" AS p
    WHERE NOT (p."IsDeleted")
) AS p0 ON c5."ProcurementId" = p0."Id"
WHERE NOT (c."IsDeleted")
  AND (
    -- Inspection Committee new delivery
    (NOT EXISTS (
        SELECT 1
        FROM "ContractManagement"."CmDelivery" AS c6
        WHERE NOT (c6."IsDeleted") AND c."Id" = c6."CmDeliveryAcceptancePeriodId"
    ) AND c1."Status" = 'InProgress' AND EXISTS (
        SELECT 1
        FROM "Procurement"."PJp005" AS p1
        WHERE NOT (p1."IsDeleted")
          AND p0."Id" = p1."ProcurementId"
          AND EXISTS (
              SELECT 1
              FROM "Procurement"."PJp005Committee" AS p2
              WHERE p1."Id" = p2."PJp005Id"
                AND p2."SuUserId" = ANY (@__userWithOutDelegateeId_0)
                AND p2."GroupType" = 'InspectionCommittee'
          )
    ))
    OR
    -- Draft/Rejected status with AcceptanceCommittee
    (c."Status" IN ('Draft', 'Rejected') AND EXISTS (
        SELECT 1
        FROM "ContractManagement"."CmDeliveryAcceptancePeriodAcceptor" AS c7
        WHERE NOT (c7."IsDeleted")
          AND c."Id" = c7."DeliveryAcceptancePeriodId"
          AND NOT (c7."IsDeleted")
          AND c7."Type" = 'AcceptanceCommittee'
          AND c7."UserId" = ANY (@__userWithOutDelegateeId_0_1)
    ))
    OR
    -- WaitingCommitteeApproval
    (c."Status" = 'WaitingCommitteeApproval' AND EXISTS (
        SELECT 1
        FROM "ContractManagement"."CmDeliveryAcceptancePeriodAcceptor" AS c8
        WHERE NOT (c8."IsDeleted")
          AND c."Id" = c8."DeliveryAcceptancePeriodId"
          AND NOT (c8."IsDeleted")
          AND c8."Type" = 'AcceptanceCommittee'
          AND c8."UserId" = ANY (@__userWithOutDelegateeId_0_1)
          AND c8."Status" = 'Pending'
    ))
    OR
    -- Director Assign
    (c."Status" = 'WaitingAssign' AND NOT EXISTS (
        SELECT 1
        FROM "ContractManagement"."CmDeliveryAcceptancePeriodAssignee" AS c9
        WHERE NOT (c9."IsDeleted") AND c."Id" = c9."DeliveryAcceptancePeriodId" AND c9."Type" = 'Assignee'
    ) AND (
        (SELECT c10."UserId"
         FROM "ContractManagement"."CmDeliveryAcceptancePeriodAssignee" AS c10
         WHERE NOT (c10."IsDeleted") AND c."Id" = c10."DeliveryAcceptancePeriodId" AND c10."Type" = 'Director'
         ORDER BY c10."Sequence" DESC
         LIMIT 1) = ANY (@__userWithDelegateeId_1)
        OR
        ((SELECT c10."UserId"
          FROM "ContractManagement"."CmDeliveryAcceptancePeriodAssignee" AS c10
          WHERE NOT (c10."IsDeleted") AND c."Id" = c10."DeliveryAcceptancePeriodId" AND c10."Type" = 'Director'
          ORDER BY c10."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userWithDelegateeId_1, NULL) IS NOT NULL)
    ))
    OR
    -- Assignee access
    (c."Status" IN ('WaitingAssign', 'WaitingComment', 'RejectToAssignee') AND EXISTS (
        SELECT 1
        FROM "ContractManagement"."CmDeliveryAcceptancePeriodAssignee" AS c11
        WHERE NOT (c11."IsDeleted") AND c."Id" = c11."DeliveryAcceptancePeriodId" AND c11."Type" = 'Assignee'
    ) AND (
        (SELECT c12."UserId"
         FROM "ContractManagement"."CmDeliveryAcceptancePeriodAssignee" AS c12
         WHERE NOT (c12."IsDeleted") AND c."Id" = c12."DeliveryAcceptancePeriodId" AND c12."Type" = 'Assignee'
         ORDER BY c12."Sequence" DESC
         LIMIT 1) = ANY (@__userWithDelegateeId_1)
        OR
        ((SELECT c12."UserId"
          FROM "ContractManagement"."CmDeliveryAcceptancePeriodAssignee" AS c12
          WHERE NOT (c12."IsDeleted") AND c."Id" = c12."DeliveryAcceptancePeriodId" AND c12."Type" = 'Assignee'
          ORDER BY c12."Sequence" DESC
          LIMIT 1) IS NULL AND array_position(@__userWithDelegateeId_1, NULL) IS NOT NULL)
    ))
    OR
    -- Approver WaitingAcceptance
    (c."Status" = 'WaitingAcceptance' AND EXISTS (
        SELECT 1
        FROM "ContractManagement"."CmDeliveryAcceptancePeriodAcceptor" AS c13
        WHERE NOT (c13."IsDeleted")
          AND c."Id" = c13."DeliveryAcceptancePeriodId"
          AND c13."Type" = 'Approver'
          AND c13."Status" = 'Pending'
          AND NOT (c13."IsDeleted")
          AND c13."Sequence" = (
              SELECT min(c14."Sequence")
              FROM "ContractManagement"."CmDeliveryAcceptancePeriodAcceptor" AS c14
              WHERE NOT (c14."IsDeleted")
                AND c."Id" = c14."DeliveryAcceptancePeriodId"
                AND NOT (c14."IsDeleted")
                AND c14."Status" = 'Pending'
          )
          AND c13."UserId" = ANY (@__userWithDelegateeId_1)
    ))
  );
```

---

## 10. ExpenseDisbursement Count Query (3ms)

```sql
SELECT count(*)::int
FROM "Procurement"."PExpenseDisbursement" AS p
WHERE NOT (p."IsDeleted")
  AND p."Status" = 'WaitingApproval'
  AND EXISTS (
      SELECT 1
      FROM "Procurement"."PExpenseDisbursementAcceptor" AS p0
      LEFT JOIN "SystemUtility"."SuDelegatee" AS s ON p0."DelegateeId" = s."Id"
      WHERE NOT (p0."IsDeleted")
        AND p."Id" = p0."PExpenseDisbursementId"
        AND p0."Sequence" = (
            SELECT min(p1."Sequence")
            FROM "Procurement"."PExpenseDisbursementAcceptor" AS p1
            WHERE NOT (p1."IsDeleted") AND p."Id" = p1."PExpenseDisbursementId"
        )
        AND (p0."UserId" = ANY (@__userIds_0)
             OR (s."Id" IS NOT NULL
                 AND (s."SuUserId" = ANY (@__userIds_0_1)
                      OR (s."SuUserId" IS NULL AND array_position(@__userIds_0_1, NULL) IS NOT NULL))))
        AND p0."Type" IN ('Approver', 'DepartmentDirectorAgree')
  );
```

---

## Summary: Query Execution Times

| Query | Duration | Percentage |
|-------|----------|------------|
| Procurement Count (PreProcurement) | 10,021ms | 16.8% |
| Procurement Count (Procurement) | 10,390ms | 17.4% |
| Procurement Count (ContractAgreement) | 9,608ms | 16.1% |
| Procurement Data (All steps) | 11,736ms | 19.6% |
| **Total Procurement Queries** | **~42,000ms** | **~70%** |
| Other queries combined | ~18,000ms | ~30% |
| **TOTAL** | **59,719ms** | **100%** |

---

## Recommended Indexes for Optimization

```sql
-- Primary bottleneck: Procurement table access patterns
CREATE INDEX CONCURRENTLY idx_procurement_step_isdeleted
ON "Procurement"."Procurement" ("Step", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_procurement_processtype_isdeleted
ON "Procurement"."Procurement" ("ProcessType", "IsDeleted");

-- Acceptor tables with common query patterns
CREATE INDEX CONCURRENTLY idx_ppappoint_acceptors_lookup
ON "Procurement"."PpAppointAcceptors" ("PpAppointId", "Type", "Status", "Sequence");

CREATE INDEX CONCURRENTLY idx_pptordraft_acceptors_lookup
ON "Procurement"."PpTorDraftAcceptors" ("PpTorDraftId", "Type", "Status", "Sequence", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_ppmedianprice_acceptors_lookup
ON "Procurement"."PpMedianPriceAcceptor" ("MedianPriceId", "Type", "Status", "Sequence", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_pppurchaserequisition_acceptors_lookup
ON "Procurement"."PpPurchaseRequisitionAcceptors" ("PpPurchaseRequisitionId", "Type", "Status", "Sequence", "IsDeleted");

-- Assignee tables with ORDER BY patterns
CREATE INDEX CONCURRENTLY idx_pptordraft_assignee_lookup
ON "Procurement"."PpTorDraftAssignee" ("PpTorDraftId", "Type", "Sequence" DESC, "IsDeleted");

CREATE INDEX CONCURRENTLY idx_ppmedianprice_assignee_lookup
ON "Procurement"."PpMedianPriceAssignee" ("MedianPriceId", "Type", "Sequence" DESC, "IsDeleted");

-- Committee tables
CREATE INDEX CONCURRENTLY idx_ppappoint_tordraft_committee
ON "Procurement"."PpAppointTorDraftCommittee" ("PpAppointId", "SuUserId", "IsDeleted");

CREATE INDEX CONCURRENTLY idx_ppappoint_medianprice_committee
ON "Procurement"."PpAppointMedianPriceCommittee" ("PpAppointId", "SuUserId", "IsDeleted");

-- Delegatee lookup
CREATE INDEX CONCURRENTLY idx_sudelegatee_suuserid
ON "SystemUtility"."SuDelegatee" ("SuUserId");
```

---