# RDLC Reports Directory

This directory contains RDLC (Report Definition Language Client-side) report templates for PDF generation.

## 📁 Available Reports

### 1. PaymentReceipt.rdlc ✅
**Status:** Implemented

**Purpose:** Generate payment receipt PDF for customers

**DataSet:** PaymentReceipt

**Fields:**
- PaymentId (Int32)
- PaymentDate (DateTime)
- Amount (Decimal)
- PaymentMethod (String)
- Status (String)
- PropertyAddress (String)
- OwnerName (String)
- ProcessedBy (String)
- ReferenceNumber (String)

**Usage:**
```csharp
var pdfBytes = await _reportService.GeneratePaymentReceiptAsync(paymentId);
```

---

### 2. PropertyTaxReport.rdlc 📝
**Status:** Template needed

**Purpose:** Detailed property tax report for a specific year

**Suggested DataSet:** PropertyTaxReport

**Suggested Fields:**
- PropertyId
- PropertyAddress
- OwnerName
- PropertyType
- LandArea
- AssessedValue
- Year
- TotalTaxDue
- TotalPaid
- Balance
- Region

---

### 3. CollectionSummary.rdlc 📝
**Status:** Template needed

**Purpose:** Summary of tax collections for a date range

**Suggested DataSet:** CollectionSummary

**Suggested Fields:**
- StartDate
- EndDate
- TotalPayments
- TotalAmount
- CashPayments
- CashAmount
- OnlinePayments
- OnlineAmount
- CheckPayments
- CheckAmount

---

### 4. TaxAssessmentReport.rdlc 📝
**Status:** Template needed

**Purpose:** Individual tax assessment details

**Suggested DataSet:** TaxAssessment

**Suggested Fields:**
- AssessmentId
- PropertyAddress
- OwnerName
- AssessmentYear
- TaxAmount
- DueDate
- Status
- AmountPaid
- Balance
- AssessedValue
- TaxRate

---

### 5. AnnualTaxStatement.rdlc 📝
**Status:** Template needed

**Purpose:** Annual tax statement for property owners

**Suggested DataSet:** AnnualStatement

**Suggested Fields:**
- PropertyId
- PropertyAddress
- OwnerName
- Year
- AssessedValue
- TotalTaxDue
- TotalPayments
- OutstandingBalance
- PaymentCount
- LastPaymentDate

---

## 🛠️ Creating New RDLC Reports

### Option 1: Using Visual Studio (Recommended)

1. **Install Report Designer:**
   - Visual Studio 2022
   - Install "Microsoft RDLC Report Designer" extension

2. **Create New Report:**
   - Right-click `Reports` folder
   - Add → New Item → Report
   - Name it (e.g., `PropertyTaxReport.rdlc`)

3. **Design Report:**
   - Add DataSet with matching field names
   - Drag fields onto report canvas
   - Add formatting, headers, footers
   - Add parameters if needed

4. **Test Report:**
   - Build and run application
   - Call report endpoint
   - Verify PDF generation

### Option 2: Using Report Builder

1. Download Microsoft Report Builder
2. Create new report
3. Define data source and dataset
4. Design report layout
5. Save as .rdlc file
6. Copy to Reports directory

### Option 3: Copy and Modify Existing

1. Copy `PaymentReceipt.rdlc`
2. Rename to new report name
3. Modify DataSet name and fields
4. Update report layout
5. Test with your data

---

## 📋 RDLC Report Structure

### Basic Structure

```xml
<?xml version="1.0" encoding="utf-8"?>
<Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition">
  <DataSources>
    <!-- Data source definition -->
  </DataSources>
  <DataSets>
    <!-- Dataset with fields -->
  </DataSets>
  <ReportSections>
    <ReportSection>
      <Body>
        <ReportItems>
          <!-- Report content: textboxes, tables, etc. -->
        </ReportItems>
      </Body>
    </ReportSection>
  </ReportSections>
  <ReportParameters>
    <!-- Optional parameters -->
  </ReportParameters>
</Report>
```

---

## 🎨 Styling Guidelines

### Colors (TaxSync Brand)
- Primary Blue: `#2563eb`
- Success Green: `#16a34a`
- Warning Yellow: `#f59e0b`
- Danger Red: `#dc2626`
- Gray Text: `#6b7280`

### Fonts
- Headers: Arial, 16-20pt, Bold
- Body: Arial, 10-12pt, Regular
- Footer: Arial, 9pt, Italic

### Layout
- Page Size: 8.5" x 11" (Letter)
- Margins: 0.5" all sides
- Header: Company name and logo
- Footer: Page numbers and timestamp

---

## 🔧 Common Report Elements

### Title Section
```xml
<Textbox Name="Title">
  <Value>REPORT TITLE</Value>
  <Style>
    <FontFamily>Arial</FontFamily>
    <FontSize>20pt</FontSize>
    <FontWeight>Bold</FontWeight>
    <Color>#2563eb</Color>
    <TextAlign>Center</TextAlign>
  </Style>
</Textbox>
```

### Data Table
```xml
<Tablix Name="DataTable">
  <TablixBody>
    <TablixColumns>
      <TablixColumn><Width>2in</Width></TablixColumn>
      <TablixColumn><Width>3in</Width></TablixColumn>
    </TablixColumns>
    <TablixRows>
      <!-- Header Row -->
      <TablixRow>
        <Height>0.3in</Height>
        <TablixCells>
          <TablixCell>
            <CellContents>
              <Textbox Name="HeaderLabel">
                <Value>Label</Value>
                <Style>
                  <FontWeight>Bold</FontWeight>
                  <BackgroundColor>#2563eb</BackgroundColor>
                  <Color>White</Color>
                </Style>
              </Textbox>
            </CellContents>
          </TablixCell>
        </TablixCells>
      </TablixRow>
      <!-- Data Row -->
      <TablixRow>
        <Height>0.3in</Height>
        <TablixCells>
          <TablixCell>
            <CellContents>
              <Textbox Name="DataValue">
                <Value>=Fields!FieldName.Value</Value>
              </Textbox>
            </CellContents>
          </TablixCell>
        </TablixCells>
      </TablixRow>
    </TablixRows>
  </TablixBody>
</Tablix>
```

### Currency Formatting
```xml
<Value>="₱" &amp; Format(Fields!Amount.Value, "#,##0.00")</Value>
```

### Date Formatting
```xml
<Value>=Format(Fields!Date.Value, "MMMM dd, yyyy")</Value>
```

---

## 🧪 Testing Reports

### 1. Test Data Binding

Ensure your DataTable matches the RDLC DataSet:

```csharp
var dataTable = new DataTable("PaymentReceipt");
dataTable.Columns.Add("PaymentId", typeof(int));
dataTable.Columns.Add("Amount", typeof(decimal));
// ... add all columns

dataTable.Rows.Add(1, 25000.00, ...);
```

### 2. Test Report Generation

```csharp
using var report = new LocalReport();
report.ReportPath = "Reports/PaymentReceipt.rdlc";

var dataSource = new ReportDataSource("PaymentReceipt", dataTable);
report.DataSources.Add(dataSource);

var pdfBytes = report.Render("PDF");
```

### 3. Verify PDF Output

- Open generated PDF
- Check all data displays correctly
- Verify formatting and styling
- Test with different data scenarios

---

## 📚 Resources

### Official Documentation
- [Microsoft RDLC Documentation](https://docs.microsoft.com/en-us/sql/reporting-services/)
- [ReportViewerCore.NETCore GitHub](https://github.com/lkosson/reportviewercore)

### Tutorials
- [Creating RDLC Reports](https://www.c-sharpcorner.com/article/creating-rdlc-report/)
- [RDLC Report Tutorial](https://www.tutorialspoint.com/rdlc/index.htm)

### Tools
- Visual Studio 2022 with RDLC Designer
- Microsoft Report Builder
- SQL Server Data Tools (SSDT)

---

## ⚠️ Important Notes

1. **DataSet Names Must Match:** The DataSet name in RDLC must match the name used in `ReportDataSource`

2. **Field Types Must Match:** DataTable column types must match RDLC field types

3. **Report Path:** Use correct path relative to application root:
   ```csharp
   Path.Combine(_environment.ContentRootPath, "Reports", "ReportName.rdlc")
   ```

4. **Parameters:** If using parameters, set them before rendering:
   ```csharp
   report.SetParameters(new ReportParameter("ParamName", "Value"));
   ```

5. **Deployment:** Include .rdlc files in publish output:
   ```xml
   <ItemGroup>
     <Content Include="Reports\*.rdlc">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </Content>
   </ItemGroup>
   ```

---

## 🚀 Quick Start

### Create a New Report in 5 Steps

1. **Copy Template:**
   ```bash
   cp PaymentReceipt.rdlc NewReport.rdlc
   ```

2. **Update DataSet Name:**
   - Open in text editor
   - Find `<DataSet Name="PaymentReceipt">`
   - Replace with your dataset name

3. **Update Fields:**
   - Modify `<Fields>` section
   - Match your data structure

4. **Update Layout:**
   - Modify report content
   - Update labels and values

5. **Test:**
   ```csharp
   var pdf = await _reportService.GenerateNewReportAsync(id);
   ```

---

## ✅ Checklist for New Reports

- [ ] RDLC file created in Reports directory
- [ ] DataSet name matches code
- [ ] All required fields defined
- [ ] Field types match data types
- [ ] Report layout designed
- [ ] Styling applied
- [ ] Parameters configured (if needed)
- [ ] Service method implemented
- [ ] Controller endpoint created
- [ ] Tested with sample data
- [ ] PDF generates correctly
- [ ] Documentation updated

---

**Need Help?**

Check the `PaymentReceipt.rdlc` file as a reference implementation, or refer to the ReportService.cs for code examples.
