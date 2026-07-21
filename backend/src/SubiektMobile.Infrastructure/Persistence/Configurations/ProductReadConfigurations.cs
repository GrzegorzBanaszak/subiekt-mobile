using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Infrastructure.Persistence.Entities;

namespace SubiektMobile.Infrastructure.Persistence.Configurations;

public sealed class TwCenaConfiguration : IEntityTypeConfiguration<TwCena>
{
    public void Configure(EntityTypeBuilder<TwCena> entity)
    {
        entity.ToTable("tw_Cena");
        entity.HasKey(price => price.Id).HasName("PK_tw_Cena");
        entity.HasIndex(price => price.IdTowar).IsUnique().HasDatabaseName("IX_tw_Cena");

        entity.Property(price => price.Id).HasColumnName("tc_Id");
        entity.Property(price => price.IdTowar).HasColumnName("tc_IdTowar");
        entity.Property(price => price.CenaNetto1).HasColumnName("tc_CenaNetto1").HasColumnType("money");
        entity.Property(price => price.CenaNetto2).HasColumnName("tc_CenaNetto2").HasColumnType("money");
        entity.Property(price => price.CenaNetto3).HasColumnName("tc_CenaNetto3").HasColumnType("money");
        entity.Property(price => price.CenaNetto4).HasColumnName("tc_CenaNetto4").HasColumnType("money");
        entity.Property(price => price.CenaNetto5).HasColumnName("tc_CenaNetto5").HasColumnType("money");
        entity.Property(price => price.CenaNetto6).HasColumnName("tc_CenaNetto6").HasColumnType("money");
        entity.Property(price => price.CenaNetto7).HasColumnName("tc_CenaNetto7").HasColumnType("money");
        entity.Property(price => price.CenaNetto8).HasColumnName("tc_CenaNetto8").HasColumnType("money");
        entity.Property(price => price.CenaNetto9).HasColumnName("tc_CenaNetto9").HasColumnType("money");
        entity.Property(price => price.CenaNetto10).HasColumnName("tc_CenaNetto10").HasColumnType("money");
        entity.Property(price => price.CenaBrutto1).HasColumnName("tc_CenaBrutto1").HasColumnType("money");
        entity.Property(price => price.CenaBrutto2).HasColumnName("tc_CenaBrutto2").HasColumnType("money");
        entity.Property(price => price.CenaBrutto3).HasColumnName("tc_CenaBrutto3").HasColumnType("money");
        entity.Property(price => price.CenaBrutto4).HasColumnName("tc_CenaBrutto4").HasColumnType("money");
        entity.Property(price => price.CenaBrutto5).HasColumnName("tc_CenaBrutto5").HasColumnType("money");
        entity.Property(price => price.CenaBrutto6).HasColumnName("tc_CenaBrutto6").HasColumnType("money");
        entity.Property(price => price.CenaBrutto7).HasColumnName("tc_CenaBrutto7").HasColumnType("money");
        entity.Property(price => price.CenaBrutto8).HasColumnName("tc_CenaBrutto8").HasColumnType("money");
        entity.Property(price => price.CenaBrutto9).HasColumnName("tc_CenaBrutto9").HasColumnType("money");
        entity.Property(price => price.CenaBrutto10).HasColumnName("tc_CenaBrutto10").HasColumnType("money");
        entity.Property(price => price.IdWaluta1).HasColumnName("tc_IdWaluta1").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta2).HasColumnName("tc_IdWaluta2").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta3).HasColumnName("tc_IdWaluta3").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta4).HasColumnName("tc_IdWaluta4").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta5).HasColumnName("tc_IdWaluta5").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta6).HasColumnName("tc_IdWaluta6").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta7).HasColumnName("tc_IdWaluta7").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta8).HasColumnName("tc_IdWaluta8").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta9).HasColumnName("tc_IdWaluta9").HasMaxLength(3).IsUnicode(false).IsFixedLength();
        entity.Property(price => price.IdWaluta10).HasColumnName("tc_IdWaluta10").HasMaxLength(3).IsUnicode(false).IsFixedLength();
    }
}

public sealed class TwParametrConfiguration : IEntityTypeConfiguration<TwParametr>
{
    public void Configure(EntityTypeBuilder<TwParametr> entity)
    {
        entity.ToTable("tw_Parametr");
        entity.HasKey(parameter => parameter.Id).HasName("PK_tw_Parametr");

        entity.Property(parameter => parameter.Id).HasColumnName("twp_Id");
        entity.Property(parameter => parameter.NazwaCeny1).HasColumnName("twp_NazwaCeny1").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny2).HasColumnName("twp_NazwaCeny2").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny3).HasColumnName("twp_NazwaCeny3").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny4).HasColumnName("twp_NazwaCeny4").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny5).HasColumnName("twp_NazwaCeny5").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny6).HasColumnName("twp_NazwaCeny6").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny7).HasColumnName("twp_NazwaCeny7").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny8).HasColumnName("twp_NazwaCeny8").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny9).HasColumnName("twp_NazwaCeny9").HasMaxLength(50).IsUnicode(false);
        entity.Property(parameter => parameter.NazwaCeny10).HasColumnName("twp_NazwaCeny10").HasMaxLength(50).IsUnicode(false);
    }
}

public sealed class TwStanConfiguration : IEntityTypeConfiguration<TwStan>
{
    public void Configure(EntityTypeBuilder<TwStan> entity)
    {
        entity.ToTable("tw_Stan");
        entity.HasKey(stock => new { stock.TowarId, stock.MagazynId }).HasName("PK_tw_Stan");

        entity.Property(stock => stock.TowarId).HasColumnName("st_TowId");
        entity.Property(stock => stock.MagazynId).HasColumnName("st_MagId");
        entity.Property(stock => stock.Stan).HasColumnName("st_Stan").HasColumnType("money");
        entity.Property(stock => stock.StanMin).HasColumnName("st_StanMin").HasColumnType("money");
        entity.Property(stock => stock.StanRezerwacji).HasColumnName("st_StanRez").HasColumnType("money");
        entity.Property(stock => stock.StanMax).HasColumnName("st_StanMax").HasColumnType("money");
    }
}

public sealed class SlMagazynConfiguration : IEntityTypeConfiguration<SlMagazyn>
{
    public void Configure(EntityTypeBuilder<SlMagazyn> entity)
    {
        entity.ToTable("sl_Magazyn");
        entity.HasKey(warehouse => warehouse.Id).HasName("PK_sl_Magazyn");

        entity.Property(warehouse => warehouse.Id).HasColumnName("mag_Id");
        entity.Property(warehouse => warehouse.Symbol).HasColumnName("mag_Symbol").HasMaxLength(3).IsUnicode(false);
        entity.Property(warehouse => warehouse.Nazwa).HasColumnName("mag_Nazwa").HasMaxLength(50).IsUnicode(false);
        entity.Property(warehouse => warehouse.Glowny).HasColumnName("mag_Glowny");
    }
}

public sealed class TwKodKreskowyConfiguration : IEntityTypeConfiguration<TwKodKreskowy>
{
    public void Configure(EntityTypeBuilder<TwKodKreskowy> entity)
    {
        entity.ToTable("tw_KodKreskowy");
        entity.HasKey(barcode => barcode.Id).HasName("PK_tw_KodKreskowy");

        entity.Property(barcode => barcode.Id).HasColumnName("kk_Id");
        entity.Property(barcode => barcode.TowarId).HasColumnName("kk_IdTowar");
        entity.Property(barcode => barcode.Kod).HasColumnName("kk_Kod").HasMaxLength(20).IsUnicode(false);
    }
}

public sealed class TwZdjecieTwConfiguration : IEntityTypeConfiguration<TwZdjecieTw>
{
    public void Configure(EntityTypeBuilder<TwZdjecieTw> entity)
    {
        entity.ToTable("tw_ZdjecieTw");
        entity.HasKey(image => image.Id).HasName("PK_tw_ZdjecieTw");

        entity.Property(image => image.Id).HasColumnName("zd_Id");
        entity.Property(image => image.TowarId).HasColumnName("zd_IdTowar");
        entity.Property(image => image.Zdjecie).HasColumnName("zd_Zdjecie").HasColumnType("image");
        entity.Property(image => image.Glowne).HasColumnName("zd_Glowne");
        entity.Property(image => image.Crc).HasColumnName("zd_CRC");
    }
}

public sealed class SlStawkaVatConfiguration : IEntityTypeConfiguration<SlStawkaVat>
{
    public void Configure(EntityTypeBuilder<SlStawkaVat> entity)
    {
        entity.ToTable("sl_StawkaVAT");
        entity.HasKey(vat => vat.Id).HasName("PK_sl_StawkaVAT");

        entity.Property(vat => vat.Id).HasColumnName("vat_Id");
        entity.Property(vat => vat.Nazwa).HasColumnName("vat_Nazwa").HasMaxLength(50).IsUnicode(false);
        entity.Property(vat => vat.Stawka).HasColumnName("vat_Stawka").HasColumnType("money");
        entity.Property(vat => vat.Symbol).HasColumnName("vat_Symbol").HasMaxLength(20).IsUnicode(false);
    }
}

public sealed class KhKontrahentConfiguration : IEntityTypeConfiguration<KhKontrahent>
{
    public void Configure(EntityTypeBuilder<KhKontrahent> entity)
    {
        entity.ToTable("kh__Kontrahent");
        entity.HasKey(contractor => contractor.Id).HasName("PK_kh__Kontrahent");
        entity.Property(contractor => contractor.Id).HasColumnName("kh_Id");
        entity.Property(contractor => contractor.Symbol).HasColumnName("kh_Symbol").HasMaxLength(20).IsUnicode(false);
        entity.Property(contractor => contractor.Rodzaj).HasColumnName("kh_Rodzaj");
        entity.Property(contractor => contractor.Zablokowany).HasColumnName("kh_Zablokowany");
    }
}

public sealed class AdrEwidConfiguration : IEntityTypeConfiguration<AdrEwid>
{
    public void Configure(EntityTypeBuilder<AdrEwid> entity)
    {
        entity.ToTable("adr__Ewid");
        entity.HasKey(address => address.Id).HasName("PK_adr__Ewid");
        entity.Property(address => address.Id).HasColumnName("adr_Id");
        entity.Property(address => address.ObjectId).HasColumnName("adr_IdObiektu");
        entity.Property(address => address.AddressType).HasColumnName("adr_TypAdresu");
        entity.Property(address => address.Name).HasColumnName("adr_Nazwa").HasMaxLength(53);
        entity.Property(address => address.FullName).HasColumnName("adr_NazwaPelna").HasMaxLength(255);
        entity.Property(address => address.Nip).HasColumnName("adr_NIP").HasMaxLength(20).IsUnicode(false);
        entity.HasIndex(address => new { address.ObjectId, address.AddressType }).IsUnique().HasDatabaseName("IX_adr__Ewid");
    }
}
