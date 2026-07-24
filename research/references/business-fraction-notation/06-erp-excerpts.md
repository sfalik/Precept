# Mirror: load-bearing excerpts for strand 06 (ERP ratio-pair representations)

> Snapshotted excerpts backing `research/language/...` (strand 06 fragment on ERP/enterprise-platform
> quantity representation). All access dates 2026-07-20. See the parent research file's Sources section
> for full citation context and grading.

## SAP — TCURR field types (Primary, SAP Help Portal, old static NetWeaver BW page)

Source: https://help.sap.com/doc/saphelp_nw75/7.5.5/en-US/4a/27aab481661d10e10000000a42189b/content.htm
("Exchange Rates for Currencies in Flat Files")

> Field Data Type Length Decimal Places Meaning
> KURST CHAR 4 0 Exchange rate type
> FCURR CUKY 5 0 From currency
> TCURR CUKY 5 0 To currency
> GDATU CHAR 8 0 Date when the rate will be valid
> UKURS DEC 9 5 Exchange rate
> FFACT DEC 9 0 Factor for the units of the 'from' currency
> TFACT DEC 9 0 Factor for the units of the 'to' currency

## SAP — Direct/Indirect Quotation for Exchange Rates (Primary, SAP AG official PDF, HELP.CAMENG Release 4.6C, April 2001)

> Purpose: This component enables you to manage exchange rates for each currency pair using direct or
> indirect quotation.
>
> Direct quotation is where the cost of one unit of foreign currency is given in units of local
> currency, whereas indirect quotation is where the cost of one unit of local currency is given in
> units of foreign currency.
>
> Table (worked example):
> Exchange rate type: M | Valid from: 01.01.1999 | Indirect quotation: [blank] | X | Factor (from): 100
> | Currency (from): USD | = | Direct quotation: 92.81993 | X | Factor (to): 1 | Currency (to): EUR
>
> "First line: Direct quotation is read from left to right, as before: 100 USD equal 92.81993 EUR."
>
> Earlier "Previously" table, same document:
> Exchange rate type: M | Currency (from): USD | Currency (to): EUR | Valid from: 01.01.1999 |
> Exchange rate: 92.81993 | Translation ratio: 100:1
>
> "Indirect exchange rates are stored with a minus sign (-) at database level."

## SAP — UMREZ / UMREN ABAP data-element documentation (Secondary — SAP-authored F1-help text, third-party-mirrored at sapdatasheet.org)

Source: https://www.sapdatasheet.org/abap/dtel/umrez.html and
https://www.sapdatasheet.org/abap/dtel/umren.html

> Definition (UMREZ): "Numerator of the quotient that specifies the ratio of the alternative unit of
> measure to the base unit of measure."
>
> Definition (UMREN): "Denominator of the quotient that specifies the ratio of the alternative unit of
> measure to the base unit of measure."
>
> Use: "Quantity (in alternative unit of measure) = quotient * quantity (in base unit of measure)"
>
> Procedure: "Enter the number of units of the alternative unit of measure (denominator) that
> corresponds to the number of units of the base unit of measure (numerator)."
>
> Example: "The alternative unit of measure is kilogram (kg). The base unit of measure is piece (PC).
> 5 kg correspond to 3 pieces. 5 kg = 3 PC => 1 kg = 3/5 PC" [quotient 3/5, numerator 3, denominator 5]
>
> Note: "You may enter only whole numbers in the numerator and denominator fields; that is, if 3.14 m²
> correspond to one piece, you must enter integer multiples (314 m² = 100 PC)."

## SAP — MARM field types (Tertiary, sapdatasheet.org)

> MARM-UMREZ: Data Type: DEC ... Internal ABAP Type: P (Packed number) ... Length: 5 characters ...
> Decimal Places: 0
>
> MARM-UMREN: same shape — DEC / Packed / length 5 / 0 decimal places

## SAP — BSEG-DMBTR field type (Tertiary, sapdatasheet.org) — the counter-check field

> Data Type: CURR ("Currency field, stored as DEC") ... Internal ABAP Type: P (Packed number) ...
> Length: 13 characters ... Decimal Places: 2

Corroborating (Tertiary, KBA preview via stechies.com summary):
> "BSEG-DMBTR and BSEG-WRBTR fields are restricted to 13 digits length (11 digits + 2 decimals). The
> maximum amount is 99,999,999,999.99."

## SAP — PP-PI Material Quantity Calculation (Primary, old static SAP Library mirror)

Source: https://help.sap.com/saphelp_globext607_10/helpdata/en/89/a42514461e11d182b50000e829fbfe/content.htm

> "The formulas for the product and component quantities may not mutually refer to each other."

## SAP — Allocation Segment / CO distribution-assessment cycle tracing-factor rules (Primary, SAP Library)

Source: https://help.sap.com/doc/ebbfd953189a424de10000000a174cb4/2.6/en-US/6cbbd953189a424de10000000a174cb4.html

> "Fixed percentages, you enter the fixed percentages (not more than 100%) that should be allocated to
> each receiver." Example: cost center 100 : 10%; cost center 300 : 30%; cost center 400 : 60%.
>
> "Fixed amounts, you enter the fixed amounts that should be allocated to each receiver."
>
> "Fixed portions, you enter the fixed portions."
>
> "Variable portions, you enter the receiver values for the receiver(s)."

## SAP — Group Reporting ownership/group-share percentage (Primary, SAP S/4HANA Group Reporting PDF guide)

> "If you enter the group share, for example, as value 80, it corresponds to 80%."
>
> "...enter the group share percentage for the relevant consolidation unit/group... document type 39
> (Group shares), which is based on quantity and the base unit Percentage."
>
> "A unit is consolidated in the subgroup B for 80% group share percentage and in the upper group A for
> 90%. The corresponding group share must be posted on the group B for 80% and in the group A for 10%."

## SAP — Partial Period Remuneration / Factoring (Primary, SAP AG PDF, HELP.PYINT, April 2001)

> "The SAP System uses the following formula to calculate the partial period factor: (Planned working
> time or flat rate period working time – absence) ÷ divisor."
>
> "These wage types contain the partial period factor. The value is set at 1 in each wage type and is
> then multiplied by the constant GENAU 100,000.00 to increase the accuracy of the calculations. The
> result is written to the Rate (RTE) field."

## Dynamics AX2012 / Commerce Runtime — Numerator/Denominator UOM conversion (Primary, Microsoft Learn, archived)

Source: https://learn.microsoft.com/en-us/previous-versions/dynamicsax-2012/unit-conversions-form
(Applies To: AX 2012 R3/R2/FP/AX2012)

> "Numerator and Denominator — With the numerator and the denominator values, you can indicate whether
> the relation between the From unit and the To unit is a 1:1 ratio or if it is a fraction. Say, that
> you want to create a conversion rule for a product where only a half piece fits into a box. In this
> case you can set up a conversion factor with From unit = Box and To unit = Pieces, and then enter 1
> in the Numerator field and 2 in the Denominator field."

Commerce Runtime API type confirmation (Primary):
> `public int Numerator { get; set; }`
> `public int Denominator { get; set; }`
> `public decimal Factor { get; set; }`
— https://learn.microsoft.com/en-us/dynamicsax-2012/appuser-itpro/unitofmeasureconversion-numerator-property-microsoft-dynamics-commerce-runtime-datamodel
(and sibling Denominator/Factor property pages)

## Dynamics 365 F&O — posted-amount counter-check (Primary, current, dated 2026-03-27)

> "You can create extensions of specific extended data types of the type Real, to change the number of
> decimals... Amounts, including unit amounts, can be maintained with a maximum of two decimals by
> default."
— https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/extensibility/decimal-point-precision

## Oracle Fusion — UOM conversion (Primary)

> "the CONVERSION_RATE column... represents the conversion factor by which the UOM is equivalent to the
> Base UOM of its Class"
— https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/25c/oedsc/invuomconversions-22253.html

Rounding behavior on ratio evaluation (Tertiary corroboration, Oracle MOSC community):
> "if a user sets the Profile QP: Inventory Decimal Precision to 6 digits, then a calculation such as
> 16 * (1/12) = 1.33333333333333… is rounded to 1.333333"

## Oracle Fusion HCM — FTE calculation (Primary)

> "The full-time equivalent (FTE) value is the result of dividing assignment working hours by standard
> working hours"
>
> "The FTE is rounded off to 10 decimal points... for example 0.1254677897"
— https://docs.oracle.com/en/cloud/saas/human-resources/fahbo/automatic-calculation-of-fte-values-for-workers.html

## Workday SOAP API — cost-allocation percentage (Primary — Workday's own published schema)

> "Distribution_Percent: decimal(9,6) >0 ... percentage, represented as a decimal value (e.g., .5), for
> the given allocation detail."
— https://community.workday.com/sites/default/files/file-hosting/productionapi/Payroll/v20/Assign_Costing_Allocation.html

## Workday SOAP API — FTE hours pair and posted-amount counter-check (Primary)

> Default_Hours: decimal(5,2) >0, "The standard weekly hours for the filled position."
> Scheduled_Hours: decimal(5,2), "The scheduled weekly hours for worker."
— https://community.workday.com/sites/default/files/file-hosting/productionapi/Staffing/v18/Change_Job.html

> Debit_Amount: decimal(26,6); Credit_Amount: decimal(26,6)
— https://community.workday.com/sites/default/files/file-hosting/productionapi/Financial_Management/v35.0/Get_Journals.html

## Infor — UOM conversion and currency-rate scaling (Primary, docs.infor.com)

> "The multiplication factor used to convert an alternative unit to the base unit. The conversion
> factor is calculated as follows: (alternative unit/base unit)." ... "If the conversion factor is 12
> and the Raise 10 to the Power value is 3, the actual conversion factor is 12000."
— https://docs.infor.com/ln/10.5/en-us/lnolh/help/tc/ibd/tcibd0103m000.html

> "Currency Rate: The factor by which an amount in a different currency is multiplied to calculate the
> amount in the currency base." "Rate Factor" (default 1.00): "to avoid extremely high or low values of
> a currency exchange-rate." "Defined Rate: The result of the specified currency exchange rate and rate
> factor."
— https://docs.infor.com/ln/10.6/en-us/lnolh/help/tc/mcs/tcmcs0108m000.html
