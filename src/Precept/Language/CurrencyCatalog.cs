// ISO 4217 List One — active currency catalog.
// Last synced: 2026-05-09. Sync process: run VS Code task "iso4217: refresh",
// then verify with dotnet test --filter CurrencyCatalogSyncTests.
// Excludes: precious metals (XAU/XAG/XPT/XPD — commodities, not currencies),
//           fund codes and accounting units (BOV/CHE/CHW/CLF/COU/MXV/USN/UYI/UYW/VED/XAD/XCG/ZWG),
//           XTS (testing), XXX (no currency).
// Tier 1 working set: 159 ISO 4217 codes after intentional exclusions.

using System.Collections.Frozen;

namespace Precept.Language;

public sealed record CurrencyEntry(
    string AlphaCode,    // e.g. "USD"
    int    NumericCode,  // e.g. 840
    string Name,         // e.g. "US Dollar"
    int    MinorUnit     // e.g. 2 (decimal places; 0 for JPY; -1 where ISO 4217 lists N/A)
);

public static class CurrencyCatalog
{
    public static readonly FrozenDictionary<string, CurrencyEntry> All =
        new CurrencyEntry[]
        {
            // A
            new("AED", 784, "UAE Dirham",                          2),
            new("AFN", 971, "Afghani",                             2),
            new("ALL",   8, "Lek",                                 2),
            new("AMD",  51, "Armenian Dram",                       2),
            new("AOA", 973, "Kwanza",                              2),
            new("ARS",  32, "Argentine Peso",                      2),
            new("AUD",  36, "Australian Dollar",                   2),
            new("AWG", 533, "Aruban Florin",                       2),
            new("AZN", 944, "Azerbaijan Manat",                    2),
            // B
            new("BAM", 977, "Convertible Mark",                    2),
            new("BBD",  52, "Barbados Dollar",                     2),
            new("BDT",  50, "Taka",                                2),
            new("BHD",  48, "Bahraini Dinar",                      3),
            new("BIF", 108, "Burundi Franc",                       0),
            new("BMD",  60, "Bermudian Dollar",                    2),
            new("BND",  96, "Brunei Dollar",                       2),
            new("BOB",  68, "Boliviano",                           2),
            new("BRL", 986, "Brazilian Real",                      2),
            new("BSD",  44, "Bahamian Dollar",                     2),
            new("BTN",  64, "Ngultrum",                            2),
            new("BWP",  72, "Pula",                                2),
            new("BYN", 933, "Belarusian Ruble",                    2),
            new("BZD",  84, "Belize Dollar",                       2),
            // C
            new("CAD", 124, "Canadian Dollar",                     2),
            new("CDF", 976, "Congolese Franc",                     2),
            new("CHF", 756, "Swiss Franc",                         2),
            new("CLP", 152, "Chilean Peso",                        0),
            new("CNY", 156, "Yuan Renminbi",                       2),
            new("COP", 170, "Colombian Peso",                      2),
            new("CRC", 188, "Costa Rican Col\u00f3n",              2),
            new("CUP", 192, "Cuban Peso",                          2),
            new("CVE", 132, "Cabo Verde Escudo",                   2),
            new("CZK", 203, "Czech Koruna",                        2),
            // D
            new("DJF", 262, "Djibouti Franc",                      0),
            new("DKK", 208, "Danish Krone",                        2),
            new("DOP", 214, "Dominican Peso",                      2),
            new("DZD",  12, "Algerian Dinar",                      2),
            // E
            new("EGP", 818, "Egyptian Pound",                      2),
            new("ERN", 232, "Nakfa",                               2),
            new("ETB", 230, "Ethiopian Birr",                      2),
            new("EUR", 978, "Euro",                                2),
            // F
            new("FJD", 242, "Fiji Dollar",                         2),
            new("FKP", 238, "Falkland Islands Pound",              2),
            // G
            new("GBP", 826, "Pound Sterling",                      2),
            new("GEL", 981, "Lari",                                2),
            new("GHS", 936, "Ghana Cedi",                          2),
            new("GIP", 292, "Gibraltar Pound",                     2),
            new("GMD", 270, "Dalasi",                              2),
            new("GNF", 324, "Guinean Franc",                       0),
            new("GTQ", 320, "Quetzal",                             2),
            new("GYD", 328, "Guyana Dollar",                       2),
            // H
            new("HKD", 344, "Hong Kong Dollar",                    2),
            new("HNL", 340, "Lempira",                             2),
            new("HTG", 332, "Gourde",                              2),
            new("HUF", 348, "Forint",                              2),
            // I
            new("IDR", 360, "Rupiah",                              2),
            new("ILS", 376, "New Israeli Sheqel",                  2),
            new("INR", 356, "Indian Rupee",                        2),
            new("IQD", 368, "Iraqi Dinar",                         3),
            new("IRR", 364, "Iranian Rial",                        2),
            new("ISK", 352, "Iceland Krona",                       0),
            // J
            new("JMD", 388, "Jamaican Dollar",                     2),
            new("JOD", 400, "Jordanian Dinar",                     3),
            new("JPY", 392, "Yen",                                 0),
            // K
            new("KES", 404, "Kenyan Shilling",                     2),
            new("KGS", 417, "Som",                                 2),
            new("KHR", 116, "Riel",                                2),
            new("KMF", 174, "Comorian Franc",                      0),
            new("KPW", 408, "North Korean Won",                    2),
            new("KRW", 410, "Won",                                 0),
            new("KWD", 414, "Kuwaiti Dinar",                       3),
            new("KYD", 136, "Cayman Islands Dollar",               2),
            new("KZT", 398, "Tenge",                               2),
            // L
            new("LAK", 418, "Lao Kip",                             2),
            new("LBP", 422, "Lebanese Pound",                      2),
            new("LKR", 144, "Sri Lanka Rupee",                     2),
            new("LRD", 430, "Liberian Dollar",                     2),
            new("LSL", 426, "Loti",                                2),
            new("LYD", 434, "Libyan Dinar",                        3),
            // M
            new("MAD", 504, "Moroccan Dirham",                     2),
            new("MDL", 498, "Moldovan Leu",                        2),
            new("MGA", 969, "Malagasy Ariary",                     2),
            new("MKD", 807, "Denar",                               2),
            new("MMK", 104, "Kyat",                                2),
            new("MNT", 496, "Tugrik",                              2),
            new("MOP", 446, "Pataca",                              2),
            new("MRU", 929, "Ouguiya",                             2),
            new("MUR", 480, "Mauritius Rupee",                     2),
            new("MVR", 462, "Rufiyaa",                             2),
            new("MWK", 454, "Malawi Kwacha",                       2),
            new("MXN", 484, "Mexican Peso",                        2),
            new("MYR", 458, "Malaysian Ringgit",                   2),
            new("MZN", 943, "Mozambique Metical",                  2),
            // N
            new("NAD", 516, "Namibia Dollar",                      2),
            new("NGN", 566, "Naira",                               2),
            new("NIO", 558, "Cordoba Oro",                         2),
            new("NOK", 578, "Norwegian Krone",                     2),
            new("NPR", 524, "Nepalese Rupee",                      2),
            new("NZD", 554, "New Zealand Dollar",                  2),
            // O
            new("OMR", 512, "Rial Omani",                          3),
            // P
            new("PAB", 590, "Balboa",                              2),
            new("PEN", 604, "Sol",                                 2),
            new("PGK", 598, "Kina",                                2),
            new("PHP", 608, "Philippine Peso",                     2),
            new("PKR", 586, "Pakistan Rupee",                      2),
            new("PLN", 985, "Zloty",                               2),
            new("PYG", 600, "Guarani",                             0),
            // Q
            new("QAR", 634, "Qatari Rial",                         2),
            // R
            new("RON", 946, "Romanian Leu",                        2),
            new("RSD", 941, "Serbian Dinar",                       2),
            new("RUB", 643, "Russian Ruble",                       2),
            new("RWF", 646, "Rwanda Franc",                        0),
            // S
            new("SAR", 682, "Saudi Riyal",                         2),
            new("SBD",  90, "Solomon Islands Dollar",              2),
            new("SCR", 690, "Seychelles Rupee",                    2),
            new("SDG", 938, "Sudanese Pound",                      2),
            new("SEK", 752, "Swedish Krona",                       2),
            new("SGD", 702, "Singapore Dollar",                    2),
            new("SHP", 654, "Saint Helena Pound",                  2),
            new("SLE", 925, "Leone",                               2),
            new("SOS", 706, "Somali Shilling",                     2),
            new("SRD", 968, "Surinam Dollar",                      2),
            new("SSP", 728, "South Sudanese Pound",                2),
            new("STN", 930, "Dobra",                               2),
            new("SVC", 222, "El Salvador Col\u00f3n",              2),
            new("SYP", 760, "Syrian Pound",                        2),
            new("SZL", 748, "Lilangeni",                           2),
            // T
            new("THB", 764, "Baht",                                2),
            new("TJS", 972, "Somoni",                              2),
            new("TMT", 934, "Turkmenistan New Manat",              2),
            new("TND", 788, "Tunisian Dinar",                      3),
            new("TOP", 776, "Pa\u02bbanga",                        2),
            new("TRY", 949, "Turkish Lira",                        2),
            new("TTD", 780, "Trinidad and Tobago Dollar",          2),
            new("TWD", 901, "New Taiwan Dollar",                   2),
            new("TZS", 834, "Tanzanian Shilling",                  2),
            // U
            new("UAH", 980, "Hryvnia",                             2),
            new("UGX", 800, "Uganda Shilling",                     0),
            new("USD", 840, "US Dollar",                           2),
            new("UYU", 858, "Peso Uruguayo",                       2),
            new("UZS", 860, "Uzbekistan Sum",                      2),
            // V
            new("VES", 928, "Bol\u00edvar Soberano",               2),
            new("VND", 704, "Dong",                                0),
            new("VUV", 548, "Vatu",                                0),
            // W
            new("WST", 882, "Tala",                                2),
            // X — remaining X-series codes in the catalog (MinorUnit = -1 where ISO 4217 lists N/A)
            new("XAF", 950, "CFA Franc BEAC",                      0),
            new("XBA", 955, "Bond Markets Unit European Composite Unit (EURCO)",      -1),
            new("XBB", 956, "Bond Markets Unit European Monetary Unit (E.M.U.-6)",    -1),
            new("XBC", 957, "Bond Markets Unit European Unit of Account 9 (EUA-9)",   -1),
            new("XBD", 958, "Bond Markets Unit European Unit of Account 17 (EUA-17)", -1),
            new("XCD", 951, "East Caribbean Dollar",               2),
            new("XDR", 960, "SDR (Special Drawing Right)",         -1),
            new("XOF", 952, "CFA Franc BCEAO",                     0),
            new("XPF", 953, "CFP Franc",                           0),
            new("XSU", 994, "Sucre",                               -1),
            new("XUA", 965, "ADB Unit of Account",                 -1),
            // Y
            new("YER", 886, "Yemeni Rial",                         2),
            // Z
            new("ZAR", 710, "Rand",                                2),
            new("ZMW", 967, "Zambian Kwacha",                      2),
        }
        .ToFrozenDictionary(e => e.AlphaCode, StringComparer.OrdinalIgnoreCase);
}
