namespace SubiektMobile.Infrastructure.Persistence.Entities;

public sealed class TwCena
{
    public int Id { get; set; }
    public int IdTowar { get; set; }
    public decimal? CenaNetto1 { get; set; }
    public decimal? CenaNetto2 { get; set; }
    public decimal? CenaNetto3 { get; set; }
    public decimal? CenaNetto4 { get; set; }
    public decimal? CenaNetto5 { get; set; }
    public decimal? CenaNetto6 { get; set; }
    public decimal? CenaNetto7 { get; set; }
    public decimal? CenaNetto8 { get; set; }
    public decimal? CenaNetto9 { get; set; }
    public decimal? CenaNetto10 { get; set; }
    public decimal? CenaBrutto1 { get; set; }
    public decimal? CenaBrutto2 { get; set; }
    public decimal? CenaBrutto3 { get; set; }
    public decimal? CenaBrutto4 { get; set; }
    public decimal? CenaBrutto5 { get; set; }
    public decimal? CenaBrutto6 { get; set; }
    public decimal? CenaBrutto7 { get; set; }
    public decimal? CenaBrutto8 { get; set; }
    public decimal? CenaBrutto9 { get; set; }
    public decimal? CenaBrutto10 { get; set; }
    public string? IdWaluta1 { get; set; }
    public string? IdWaluta2 { get; set; }
    public string? IdWaluta3 { get; set; }
    public string? IdWaluta4 { get; set; }
    public string? IdWaluta5 { get; set; }
    public string? IdWaluta6 { get; set; }
    public string? IdWaluta7 { get; set; }
    public string? IdWaluta8 { get; set; }
    public string? IdWaluta9 { get; set; }
    public string? IdWaluta10 { get; set; }
}

public sealed class TwParametr
{
    public int Id { get; set; }
    public string? NazwaCeny1 { get; set; }
    public string? NazwaCeny2 { get; set; }
    public string? NazwaCeny3 { get; set; }
    public string? NazwaCeny4 { get; set; }
    public string? NazwaCeny5 { get; set; }
    public string? NazwaCeny6 { get; set; }
    public string? NazwaCeny7 { get; set; }
    public string? NazwaCeny8 { get; set; }
    public string? NazwaCeny9 { get; set; }
    public string? NazwaCeny10 { get; set; }
}

public sealed class TwStan
{
    public int TowarId { get; set; }
    public int MagazynId { get; set; }
    public decimal? Stan { get; set; }
    public decimal? StanMin { get; set; }
    public decimal? StanRezerwacji { get; set; }
    public decimal? StanMax { get; set; }
}

public sealed class SlMagazyn
{
    public int Id { get; set; }
    public string? Symbol { get; set; }
    public string? Nazwa { get; set; }
    public bool? Glowny { get; set; }
}

public sealed class TwKodKreskowy
{
    public int Id { get; set; }
    public int TowarId { get; set; }
    public string? Kod { get; set; }
}

public sealed class TwZdjecieTw
{
    public int Id { get; set; }
    public int TowarId { get; set; }
    public byte[]? Zdjecie { get; set; }
    public bool? Glowne { get; set; }
    public int? Crc { get; set; }
}

public sealed class SlStawkaVat
{
    public int Id { get; set; }
    public string? Nazwa { get; set; }
    public decimal? Stawka { get; set; }
    public string? Symbol { get; set; }
}

public sealed class KhKontrahent
{
    public int Id { get; set; }
    public string? Symbol { get; set; }
    public int? Rodzaj { get; set; }
    public bool? Zablokowany { get; set; }
}

public sealed class AdrEwid
{
    public int Id { get; set; }
    public int ObjectId { get; set; }
    public int AddressType { get; set; }
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string? Nip { get; set; }
}
