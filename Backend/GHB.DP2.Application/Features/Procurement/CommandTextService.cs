namespace GHB.DP2.Application.Features.Procurement;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.SystemUtility;

public record CommandTextTemplate(
    string Template80,
    Dictionary<SupplyMethod60, string> Template60s,
    Dictionary<SupplyMethod60, string>? Template60sOverMax = null
);

public enum SupplyMethod60
{
    EMarket,
    EBidding,
    Specific,
    SpecificMoreThen500000,
    Selection,
}

public interface ICommandTextTemplateProvider
{
    CommandTextTemplate GetTemplate(string program);

    string GetExtraText(bool hasBudgetOverMax);

    string GetExtra60Text(bool hasBudgetOverMax, decimal commandBudget, bool useDotsBudget = false);
}

[RegisterService<ICommandTextTemplateProvider>(LifeTime.Scoped)]
public class CommandTextTemplateProvider : ICommandTextTemplateProvider
{
    private readonly Dictionary<string, CommandTextTemplate> templates = new()
    {
        [CommandTextProgram.Appoint] = new(
            Template80: "การแต่งตั้งบุคคลและคณะกรรมการ จัดทำร่างขอบเขตของงาน/ร่างรายละเอียดคุณลักษณะเฉพาะของพัสดุ และกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ และการแต่งตั้งผู้กำหนดราคากลาง/คณะกรรมการกำหนดราคากลาง เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง ข้อ 1",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "การแต่งตั้งบุคคลและคณะกรรมการจัดทำร่างขอบเขตของงาน/ร่างรายละเอียดคุณลักษณะเฉพาะของพัสดุและกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ เป็นอำนาจของ{PositionName} ตามคำสั่งธนาคาร ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐโดยวิธีเฉพาะเจาะจง ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. ๒๕๖๐ ข้อ....(....) {BudgetClause}",
                [SupplyMethod60.SpecificMoreThen500000] = "การแต่งตั้งบุคคลและคณะกรรมการ จัดทำร่างขอบเขตของงาน/ร่างรายละเอียดคุณลักษณะเฉพาะของพัสดุ และกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ และการแต่งตั้งผู้กำหนดราคากลาง/คณะกรรมการกำหนดราคากลาง เป็นอำนาจของ{PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ................ ข้อ 1 และข้อ 2",
                [SupplyMethod60.EMarket] = "การแต่งตั้งบุคคลและคณะกรรมการ จัดทำร่างขอบเขตของงาน/ร่างรายละเอียดคุณลักษณะเฉพาะของพัสดุ และกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ และการแต่งตั้งผู้กำหนดราคากลาง/คณะกรรมการกำหนดราคากลาง เป็นอำนาจของ{PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ................ ข้อ 1 และข้อ 2",
                [SupplyMethod60.EBidding] = "การแต่งตั้งบุคคลและคณะกรรมการ จัดทำร่างขอบเขตของงาน/ร่างรายละเอียดคุณลักษณะเฉพาะของพัสดุ และกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ และการแต่งตั้งผู้กำหนดราคากลาง/คณะกรรมการกำหนดราคากลาง เป็นอำนาจของ{PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ................ ข้อ 1 และข้อ 2",
                [SupplyMethod60.Selection] = "การแต่งตั้งบุคคลและคณะกรรมการ จัดทำร่างขอบเขตของงาน/ร่างรายละเอียดคุณลักษณะเฉพาะของพัสดุ และกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ และการแต่งตั้งผู้กำหนดราคากลาง/คณะกรรมการกำหนดราคากลาง เป็นอำนาจของ{PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ................ ข้อ 1 และข้อ 2",
            }),

        [CommandTextProgram.TorDraft] = new(
            Template80: "ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง ข้อ 2 การอนุมัติร่างขอบเขตของงาน (TOR) ให้เป็นอำนาจของผู้ดำรงตำแหน่ง ดังต่อไปนี้ {PositionName} {BudgetClause} {Extra}",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "การอนุมัติขอบเขตของงานโดยวิธีเฉพาะเจาะจง กรณี{BudgetClause} เป็นอำนาจของ{PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธีเฉพาะเจาะจงประกอบแนวปฏิบัติตามคำสั่งตามคำสั่งส่งธนาคารอาคารสงเคราะห์ ที่ 215/2560 เรื่อง การปฏิบัติงานที่เกี่ยวกับการจัดซื้อจัดจ้างโดยวิธีเฉพาะเจาะจง วงเงินไม่เกิน 500,000 บาท",
                [SupplyMethod60.SpecificMoreThen500000] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ 3",
                [SupplyMethod60.EMarket] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ 3",
                [SupplyMethod60.EBidding] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ 3",
                [SupplyMethod60.Selection] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ 3",
            },
            Template60sOverMax: new()
            {
                [SupplyMethod60.SpecificMoreThen500000] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
                [SupplyMethod60.EMarket] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
                [SupplyMethod60.EBidding] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
                [SupplyMethod60.Selection] = "การอนุมัติร่างขอบเขตของงาน (TOR) หรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จัดซื้อหรือจ้าง รวมทั้งหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
            }),

        [CommandTextProgram.MedianPrice] = new(
            Template80: "ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง ข้อ 2 การอนุมัติกำหนดราคากลาง ให้เป็นอำนาจของผู้ดำรงตำแหน่ง ดังต่อไปนี้ {PositionName} {BudgetClause} {Extra}",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ.2560 มาตรา 56(2)(ข) ประกอบกฎกระทรวง (กำหนดวงเงินการจัดซื้อจัดจ้างพัสดุ โดยวิธี{SupplyMethodSpecialType} วงเงินการจัดซื้อจัดจ้างที่ไม่ทำข้อตกลงเป็นหนังสือ และวงเงินการจัดซื้อจัดจ้าง ในการแต่งตั้งผู้ตรวจรับพัสดุ พ.ศ.2560) ข้อ 2 การอนุมัติกำหนดราคากลาง หรือรายละเอียดเฉพาะของพัสดุที่จะซื้อหรือจ้าง รวมทั้งการกำหนดหลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ ซึ่งได้แก่การจัดซื้อจัดจ้างสินค้างานบริการ หรืองานก่อสร้างที่มีการผลิต จำหน่าย ก่อสร้าง หรือให้บริการทั่วไปและมีวงเงินในการจัดซื้อจัดจ้างครั้งหนึ่งไม่เกินวงเงิน 500,000 บาท ซึ่งอยู่ในอำนาจของ{PositionName} ที่จะอนุมัติได้ใน{BudgetClause} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่องการมอบอำนาจสั่งซื้อหรือสั่งจ้างกรณีจัดซื้อจัดจ้างและการบริหารพัสดุ โดยวิธี{SupplyMethodSpecialType}",
                [SupplyMethod60.SpecificMoreThen500000] = "การอนุมัติกำหนดราคากลาง วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560",
                [SupplyMethod60.EMarket] = "การอนุมัติกำหนดราคากลาง วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560",
                [SupplyMethod60.EBidding] = "การอนุมัติกำหนดราคากลาง วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560",
                [SupplyMethod60.Selection] = "การอนุมัติกำหนดราคากลาง วงเงินไม่เกิน {CommandBudget} บาท เป็นอำนาจของ {PositionName} ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุโดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560",
            },
            Template60sOverMax: new()
            {
                [SupplyMethod60.SpecificMoreThen500000] = "การอนุมัติกำหนดราคากลาง ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ .... และข้อ ..... กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
                [SupplyMethod60.EMarket] = "การอนุมัติกำหนดราคากลาง ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ .... และข้อ ..... กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
                [SupplyMethod60.EBidding] = "การอนุมัติกำหนดราคากลาง ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ .... และข้อ ..... กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
                [SupplyMethod60.Selection] = "การอนุมัติกำหนดราคากลาง ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนหัวหน้าหน่วยงานของรัฐ โดยวิธี{SupplyMethodSpecialType} ตามพระราชบัญญัติการจัดซื้อจัดจ้าง และการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ .... และข้อ ..... กรรมการผู้จัดการ มอบอำนาจให้{PositionName} กระทำการแทนในวงเงินไม่เกิน {CommandBudget} บาท {Extra60}",
            }),

        [CommandTextProgram.JorPor05] = new(
            Template80: "ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง ข้อ 3 การให้ความเห็นชอบรายงานขอซื้อหรือขอจ้าง รวมถึงการแต่งตั้งคณะกรรมการหรือพนักงานผู้หนึ่งผู้ใด เพื่อดำเนินการซื้อหรือจ้างโดยวิธีพิเศษ และการแต่งตั้งคณะกรรมการหรือพนักงานผู้หนึ่งผู้ใดเพื่อดำเนินการตรวจรับพัสดุ ให้เป็นอำนาจของผู้ดำรงตำแหน่ง ดังต่อไปนี้ {PositionName} {BudgetClause} {Extra}",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.SpecificMoreThen500000] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EMarket] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EBidding] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.Selection] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
            }),

        [CommandTextProgram.JorPor06] = new(
            Template80: "ตามข้อบังคับธนาคารอาคารสงเคราะห์ ฉบับที่ 80 ว่าด้วยการจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง พ.ศ.2561 ข้อ 22 (1) การซื้อหรือจ้างโดยวิธีพิเศษ และข้อ 23 (1) ประกอบกับคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง ข้อ 4 และ ข้อ 5 (ก) กรรมการผู้จัดการได้มอบอำนาจให้ {PositionName} สั่งซื้อสั่งจ้างโดยวิธีพิเศษ รวมถึงลงนามประกาศผู้ชนะการเสนอราคา ใน{BudgetClause} {Extra}",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "ตามพระราชบัญญัติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ.2560 มาตรา 56(2)(ข) ประกอบกฎกระทรวง (กำหนดวงเงินการจัดซื้อจัดจ้างพัสดุ โดยวิธี{SupplyMethodSpecialType} วงเงินการจัดซื้อจัดจ้างที่ไม่ทําข้อตกลงเป็นหนังสือ และวงเงินการจัดซื้อจัดจ้าง ในการแต่งตั้งผู้ตรวจรับพัสดุ พ.ศ.2560) ข้อ 1 ซึ่งได้แก่การจัดซื้อจัดจ้างสินค้างานบริการ หรืองานก่อสร้างที่มีการผลิต จําหน่าย ก่อสร้าง หรือให้บริการทั่วไปและมีวงเงินในการจัดซื้อจัดจ้างครั้งหนึ่งไม่เกินวงเงิน 500,000.00 บาท ซึ่งอยู่ในอำนาจของ{PositionName} ที่จะอนุมัติได้ในวงเงินไม่เกิน 100,000.00 บาท ตามคําสั่งธนาคารอาคารสงเคราะห์ ที่ 216/2560 เรื่องการมอบอํานาจสั่งซื้อหรือสั่งจ้างกรณีจัดซื้อจัดจ้างและการบริหารพัสดุ โดยวิธี{SupplyMethodSpecialType}",
                [SupplyMethod60.SpecificMoreThen500000] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EMarket] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EBidding] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.Selection] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
            }),

        [CommandTextProgram.ContractDraft] = new(
            Template80: "กรณีสัญญามีวงเงินค่าซื้อขาย ค่าจ้างหรือค่าเช่า รวมทั้งสัญญา หรือตลอดอายุสัญญา {BudgetClause} {PositionName} เป็นผู้ได้รับมอบอำนาจให้ลงนามในสัญญาแทนในนามธนาคารฯ ตามคำสั่งธนาคารที่ {CommandNumber} เรื่องการมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการกรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง {Extra}",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "กรณีสัญญามีวงเงินค่าซื้อขาย ค่าจ้างหรือค่าเช่า รวมทั้งสัญญา หรือตลอดอายุสัญญา {BudgetClause} {PositionName} ที่ดูแลรับผิดชอบงานสัญญาซึ่งเป็นผู้ได้รับมอบอำนาจให้ลงนามในสัญญาแทน ในนามธนาคารฯตามคำสั่งธนาคารที่ {CommandNumber} เรื่องการมอบอำนาจลงนามในสัญญากรณีจัดซื้อจัดจ้างและ การบริหารพัสดุ",
                [SupplyMethod60.SpecificMoreThen500000] = "กรณีสัญญามีวงเงินค่าซื้อขาย ค่าจ้างหรือค่าเช่า รวมทั้งสัญญา หรือตลอดอายุสัญญา {BudgetClause} {PositionName} ที่ดูแลรับผิดชอบงานสัญญาซึ่งเป็นผู้ได้รับมอบอำนาจให้ลงนามในสัญญาแทน ในนามธนาคารฯตามคำสั่งธนาคารที่ {CommandNumber} เรื่องการมอบอำนาจลงนามในสัญญากรณีจัดซื้อจัดจ้างและ การบริหารพัสดุ",
                [SupplyMethod60.EMarket] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EBidding] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.Selection] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
            }),

        [CommandTextProgram.DeliveryAcceptance] = new(
            Template80: "ตามคำสั่งธนาคารอาคารสงเคราะห์ ที่ {CommandNumber} เรื่อง การมอบอำนาจให้กระทำการแทนกรรมการผู้จัดการ กรณีจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง ข้อ 9 การรับทราบผลการตรวจรับพัสดุเมื่อคณะกรรมการตรวจรับพัสดุ หรือผู้ตรวจรับพัสดุตรวจรับพัสดุถูกต้องครบถ้วนแล้ว ให้เป็นอำนาจของผู้ดำรงตำแหน่ง ดังต่อไปนี้ {PositionName} {BudgetClause} {Extra}",
            Template60s: new()
            {
                [SupplyMethod60.Specific] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.SpecificMoreThen500000] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EMarket] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.EBidding] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
                [SupplyMethod60.Selection] = "......................................................................................................................................................................................................................................................................................................................................................................................................................",
            }),
    };

    private const string ExtraTextForBudgetOverMax = "แต่ในกรณีดังกล่าววงเงินเกิน {CommandBudget} บาท อยู่ในอำนาจกรรมการผู้จัดการ";

    private const string Extra60TextForBudgetOverMax = "แต่การจัดซื้อจัดจ้างครั้งนี้มีวงเงินเกิน {CommandBudget} บาท จึงอยู่ในอำนาจของกรรมการผู้จัดการ ตามระเบียบกระทรวงการคลัง ว่าด้วยการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ พ.ศ. 2560 ข้อ 6";

    public CommandTextTemplate GetTemplate(string program)
    {
        return this.templates.TryGetValue(program, out var template)
            ? template
            : new CommandTextTemplate(string.Empty, new Dictionary<SupplyMethod60, string>());
    }

    public string GetExtraText(bool hasBudgetOverMax) =>
        hasBudgetOverMax ? ExtraTextForBudgetOverMax : string.Empty;

    public string GetExtra60Text(bool hasBudgetOverMax, decimal commandBudget, bool useDotsBudget = false) =>
        hasBudgetOverMax
            ? Extra60TextForBudgetOverMax.Replace("{CommandBudget}", useDotsBudget ? "............." : commandBudget.ToCurrencyStringNoDecimal())
            : string.Empty;
}

public static class CommandTextProgram
{
    public const string Appoint = "Appoint";
    public const string TorDraft = "TorDraft";
    public const string MedianPrice = "MedianPrice";
    public const string JorPor05 = "JorPor05";
    public const string JorPor06 = "JorPor06";
    public const string ContractDraft = "ContractDraft";
    public const string DeliveryAcceptance = "DeliveryAcceptance";
}

public interface ICommandTextService
{
    string GetCommandText(
        string program,
        IEnumerable<OperationPositionInfo> managers,
        ParameterCode supplyMethodCode,
        decimal budget,
        ParameterCode? supplyMethodSpecialType = null,
        string? supplyMethodSpecialName = null,
        string? commandNumber = null);
}

[RegisterService<ICommandTextService>(LifeTime.Scoped)]
public class CommandTextService : ICommandTextService
{
    private readonly ICommandTextTemplateProvider templateProvider;

    public CommandTextService(ICommandTextTemplateProvider templateProvider)
    {
        this.templateProvider = templateProvider;
    }

    public string GetCommandText(
        string program,
        IEnumerable<OperationPositionInfo> managers,
        ParameterCode supplyMethodCode,
        decimal budget,
        ParameterCode? supplyMethodSpecialType = null,
        string? supplyMethodSpecialName = null,
        string? commandNumber = null)
    {
        var operationPositionInfos = managers.ToList();

        var inRefCodeManager = new[]
        {
            InRefCodeConstant.Bp001,
            InRefCodeConstant.Bp002,
        };

        var hasApproverMD = operationPositionInfos.Any(m => m.Budget > MaximumBudget.MaxBudget && inRefCodeManager.Contains(m.InRefCode));

        var finalManager = hasApproverMD && operationPositionInfos.Count > 1
            ? operationPositionInfos
                .OrderByDescending(m => m.Budget)
                .FirstOrDefault(o => !inRefCodeManager.Contains(o.InRefCode))
              ?? operationPositionInfos.OrderByDescending(m => m.Budget).FirstOrDefault()
            : operationPositionInfos.OrderByDescending(m => m.Budget).FirstOrDefault();

        var template = this.templateProvider.GetTemplate(program);

        var supplyMethod60 = MapSupplyMethod(supplyMethodSpecialType, budget);
        var isMethod80 = supplyMethodCode == SupplyMethodConstant.Eighty;

        string templateToUse = isMethod80
            ? template.Template80
            : hasApproverMD
                ? this.GetTemplate60OverMax(program, supplyMethod60)
                : this.GetTemplate60(program, supplyMethod60);

        if (string.IsNullOrEmpty(templateToUse))
        {
            return string.Empty;
        }

        var firstManager = operationPositionInfos.FirstOrDefault();
        var commandBudget = firstManager?.CommandBudget ?? finalManager?.CommandBudget ?? 0;
        var effectiveCommandNumber = firstManager?.CommandNumber ?? commandNumber;

        // กรณี รายการที่ 1 เป็น BP001/BP002 และ รายการที่ 2 เป็น BP025 ให้แสดง ............. แทนวงเงิน
        var useDotsBudget = inRefCodeManager.Contains(firstManager?.InRefCode)
            && finalManager?.InRefCode == InRefCodeConstant.Bp025;

        var extraText = this.templateProvider.GetExtraText(hasApproverMD);
        var extra60Text = this.templateProvider.GetExtra60Text(hasApproverMD, commandBudget, useDotsBudget);
        var supplyMethodDisplayName = isMethod80 ? supplyMethodSpecialName : MapSupplyMethodDisplayName(supplyMethod60);

        return FormatTemplate(templateToUse, finalManager, supplyMethodDisplayName, extraText, extra60Text, effectiveCommandNumber, commandBudget, useDotsBudget);
    }

    private static SupplyMethod60 MapSupplyMethod(ParameterCode? supplyMethodSpecialType, decimal budget)
    {
        return supplyMethodSpecialType.ToString() switch
        {
            SupplyMethodSpecialTypeConstant.Specific when budget > 500_000 => SupplyMethod60.SpecificMoreThen500000,
            SupplyMethodSpecialTypeConstant.Specific => SupplyMethod60.Specific,
            SupplyMethodSpecialTypeConstant.EMarket => SupplyMethod60.EMarket,
            SupplyMethodSpecialTypeConstant.EBidding => SupplyMethod60.EBidding,
            SupplyMethodSpecialTypeConstant.Selection => SupplyMethod60.Selection,
            _ => SupplyMethod60.Specific,
        };
    }

    private static string MapSupplyMethodDisplayName(SupplyMethod60 method) => method switch
    {
        SupplyMethod60.Specific => "วิธีเฉพาะเจาะจง",
        SupplyMethod60.SpecificMoreThen500000 => "วิธีเฉพาะเจาะจง",
        SupplyMethod60.Selection => "วิธีคัดเลือก",
        SupplyMethod60.EMarket => "วิธีประกาศเชิญชวนทั่วไป",
        SupplyMethod60.EBidding => "วิธีประกาศเชิญชวนทั่วไป",
        _ => string.Empty,
    };

    public string GetTemplate60(string program, SupplyMethod60 method)
    {
        var template = this.templateProvider.GetTemplate(program);

        if (template.Template60s.TryGetValue(method, out var value))
        {
            return value;
        }

        return string.Empty;
    }

    private string GetTemplate60OverMax(string program, SupplyMethod60 method)
    {
        var template = this.templateProvider.GetTemplate(program);

        if (template.Template60sOverMax != null && template.Template60sOverMax.TryGetValue(method, out var value))
        {
            return value;
        }

        return this.GetTemplate60(program, method);
    }

    private static string FormatTemplate(string template, OperationPositionInfo? manager, string? supplyMethodSpecialType, string extraText, string extra60Text, string? commandNumber, decimal? commandBudget = null, bool useDotsBudget = false)
    {
        var effectiveBudget = commandBudget is { } cb && cb != 0 ? cb : manager?.CommandBudget ?? 0;
        var budgetDisplay = useDotsBudget ? "............." : effectiveBudget.ToCurrencyStringNoDecimal();
        var isUnlimitedBudget = manager?.Budget > MaximumBudget.MaxBudget;
        var budgetClause = isUnlimitedBudget
            ? string.Empty
            : $"วงเงินไม่เกิน {budgetDisplay} บาท";

        return template
            .Replace("{RefBankOrder}", manager?.RefBankOrder ?? string.Empty)
            .Replace("{ShortPositionName}", manager?.ShortPositionName ?? "()")
            .Replace("{PositionName}", manager?.PositionName?.Replace("ผ่าน", string.Empty).Replace("  ", " ").Trim() ?? string.Empty)
            .Replace("{Extra}", extraText)
            .Replace("{Extra60}", extra60Text)
            .Replace("{BudgetClause}", budgetClause)
            .Replace("{CommandBudget}", budgetDisplay)
            .Replace("{SupplyMethodSpecialType}", supplyMethodSpecialType ?? string.Empty)
            .Replace("{CommandNumber}", string.IsNullOrEmpty(commandNumber) ? "............................." : commandNumber);
    }
}